using System;
using System.IO;
using ChemistryLab.Desktop;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChemistryLab.Desktop.Editor
{
    public static class DesktopLabBuild
    {
        private const string SceneDirectory = "Assets/_DesktopLab/Scenes";
        private const string ScenePath = SceneDirectory + "/DesktopChemistryLab.unity";
        private const string ResourceDirectory = "Assets/_DesktopLab/Resources";
        private const string StandardMaterialPath = ResourceDirectory + "/DesktopLabStandard.mat";
        private const string BuildDirectory = "Builds/ChemistryLab3D";
        private const string ExecutablePath = BuildDirectory + "/ChemistryLab3D.exe";

        [MenuItem("Chemistry Lab/Desktop/Create Native Scene")]
        public static void CreateScene()
        {
            ValidateData();
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_DesktopLab", "Scenes"));
            EnsureRuntimeMaterial();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("Desktop Chemistry Lab");
            bootstrap.AddComponent<DesktopLabGame>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("DESKTOP_LAB_SCENE_PASS path=" + ScenePath);
        }

        public static void ValidateOnly()
        {
            ValidateData();
            Debug.Log(
                "DESKTOP_LAB_DATA_PASS elements=" + HighSchoolPeriodicTable.All.Count
                + " chemicals=" + DesktopChemistryDatabase.AllChemicals.Count
                + " reactions=" + DesktopChemistryDatabase.AllReactions.Count);
        }

        public static void BuildWindows()
        {
            CreateScene();
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var absoluteBuildDirectory = Path.Combine(projectRoot, BuildDirectory);
            var absoluteExecutablePath = Path.Combine(projectRoot, ExecutablePath);
            Directory.CreateDirectory(absoluteBuildDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = absoluteExecutablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Native Windows build failed: " + report.summary.result
                    + " · errors=" + report.summary.totalErrors
                    + " · warnings=" + report.summary.totalWarnings);
            }

            Debug.Log(
                "DESKTOP_LAB_BUILD_PASS path=" + absoluteExecutablePath
                + " size=" + report.summary.totalSize
                + " warnings=" + report.summary.totalWarnings);
        }

        private static void ValidateData()
        {
            DesktopChemistryDatabase.ValidateOrThrow();
            HighSchoolPeriodicTable.ValidateOrThrow();
            DesktopLabAudio.ValidateSignalGenerationOrThrow();

            var mission = ReactionSimulator.Evaluate(
                new[]
                {
                    new VesselAddition("copper-sulfate", 10d),
                    new VesselAddition("sodium-hydroxide", 10d)
                },
                LabStation.Workbench,
                24f);
            if (mission.Status != ReactionStatus.Reaction
                || mission.Reaction == null
                || !string.Equals(mission.Reaction.Id, "copper-hydroxide", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Mission reaction validation failed.");
            }

            var hoodRule = ReactionSimulator.Evaluate(
                new[]
                {
                    new VesselAddition("sodium-sulfide", 5d),
                    new VesselAddition("hydrochloric-acid", 5d)
                },
                LabStation.Workbench,
                24f);
            if (hoodRule.Status != ReactionStatus.Blocked)
            {
                throw new InvalidOperationException("Fume-hood safety validation failed.");
            }

            var hoodReactionCount = 0;
            var effects = new System.Collections.Generic.HashSet<ReactionEffect>();
            foreach (var reaction in DesktopChemistryDatabase.AllReactions)
            {
                var chemicalA = DesktopChemistryDatabase.GetChemical(reaction.ReactantA);
                var chemicalB = DesktopChemistryDatabase.GetChemical(reaction.ReactantB);
                if (chemicalA == null || chemicalB == null)
                {
                    throw new InvalidOperationException(
                        "Reaction references missing chemistry data: " + reaction.Id);
                }

                var additions = new[]
                {
                    new VesselAddition(
                        reaction.ReactantA,
                        chemicalA.MolarMass * reaction.CoefficientA * 0.01d),
                    new VesselAddition(
                        reaction.ReactantB,
                        chemicalB.MolarMass * reaction.CoefficientB * 0.01d)
                };
                var station = reaction.RequiresFumeHood ? LabStation.FumeHood : LabStation.Workbench;
                var forward = ReactionSimulator.Evaluate(additions, station, 24f);
                var reverse = ReactionSimulator.Evaluate(
                    new[] { additions[1], additions[0] },
                    station,
                    24f);
                ValidateReactionOutcome(reaction, forward, "forward");
                ValidateReactionOutcome(reaction, reverse, "reverse");
                effects.Add(reaction.Effect);

                if (reaction.RequiresFumeHood)
                {
                    hoodReactionCount++;
                    var blocked = ReactionSimulator.Evaluate(additions, LabStation.Workbench, 24f);
                    if (blocked.Status != ReactionStatus.Blocked
                        || blocked.Reaction == null
                        || !string.Equals(blocked.Reaction.Id, reaction.Id, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "Fume-hood rule did not block reaction: " + reaction.Id);
                    }
                }
            }

            if (!effects.Contains(ReactionEffect.Heat)
                || !effects.Contains(ReactionEffect.Precipitate)
                || !effects.Contains(ReactionEffect.Gas)
                || !effects.Contains(ReactionEffect.Colour))
            {
                throw new InvalidOperationException("Reaction-effect coverage is incomplete.");
            }

            Debug.Log(
                "DESKTOP_LAB_REACTION_MATRIX_PASS total="
                + DesktopChemistryDatabase.AllReactions.Count
                + " hoodRules=" + hoodReactionCount
                + " effects=" + effects.Count
                + " audioSignals=4");
        }

        private static void ValidateReactionOutcome(
            ReactionDefinition expected,
            ReactionOutcome outcome,
            string order)
        {
            if (outcome.Status != ReactionStatus.Reaction
                || outcome.Reaction == null
                || !string.Equals(outcome.Reaction.Id, expected.Id, StringComparison.Ordinal)
                || double.IsNaN(outcome.TheoreticalProductGrams)
                || double.IsInfinity(outcome.TheoreticalProductGrams)
                || outcome.TheoreticalProductGrams <= 0d
                || double.IsNaN(outcome.EstimatedProductGrams)
                || double.IsInfinity(outcome.EstimatedProductGrams)
                || outcome.EstimatedProductGrams <= 0d
                || float.IsNaN(outcome.TemperatureC)
                || float.IsInfinity(outcome.TemperatureC))
            {
                throw new InvalidOperationException(
                    "Reaction matrix validation failed: " + expected.Id + " · " + order);
            }
        }

        private static void EnsureRuntimeMaterial()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_DesktopLab", "Resources"));
            var material = AssetDatabase.LoadAssetAtPath<Material>(StandardMaterialPath);
            if (material != null)
            {
                return;
            }

            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("Không tìm thấy shader dựng hình phù hợp trong Unity Editor.");
            }

            material = new Material(shader)
            {
                name = "Desktop Lab Standard"
            };
            AssetDatabase.CreateAsset(material, StandardMaterialPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
