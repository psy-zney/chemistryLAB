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
        private const string SceneDirectory = "Assets/ChemistryLab/Scenes";
        private const string ScenePath = SceneDirectory + "/DesktopChemistryLab.unity";
        private const string ResourceDirectory = "Assets/ChemistryLab/Resources";
        private const string StandardMaterialPath = ResourceDirectory + "/DesktopLabStandard.mat";
        private const string BuildDirectory = "Builds/ChemistryLab3D";
        private const string ExecutablePath = BuildDirectory + "/ChemistryLab3D.exe";
        private const string ReportDirectory = "BuildReports";
        private const string BuildReportFile = "desktop-build-report.json";
        private const string ValidationReportFile = "desktop-validation-report.json";

        [MenuItem("Chemistry Lab/Desktop/Create Native Scene")]
        public static void CreateScene()
        {
            ValidateData();
            CreateSceneAssets();
        }

        private static void CreateSceneAssets()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "ChemistryLab", "Scenes"));
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
            var validation = ValidateData();
            WriteStructuredReport(
                ValidationReportFile,
                "validation",
                "succeeded",
                validation,
                0,
                0,
                0L,
                null);
            Debug.Log(
                "DESKTOP_LAB_DATA_PASS elements=" + HighSchoolPeriodicTable.All.Count
                + " chemicals=" + DesktopChemistryDatabase.AllChemicals.Count
                + " reactions=" + DesktopChemistryDatabase.AllReactions.Count
                + " generatedCompounds=" + CompoundGenerationMatrix.AcceptedCompoundCount
                + " uniqueFormulas=" + CompoundGenerationMatrix.UniqueFormulaCount
                + " reviewedGeneratedCompounds=" + CompoundGenerationMatrix.ReviewedCompoundCount);
        }

        public static void BuildWindows()
        {
            var validation = ValidateData();
            CreateSceneAssets();
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
            WriteStructuredReport(
                BuildReportFile,
                "windows-player",
                report.summary.result == BuildResult.Succeeded ? "succeeded" : "failed",
                validation,
                (int)report.summary.totalWarnings,
                (int)report.summary.totalErrors,
                (long)report.summary.totalSize,
                ExecutablePath);
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

        private static ValidationSummary ValidateData()
        {
            DesktopChemistryDatabase.ValidateOrThrow();
            HighSchoolPeriodicTable.ValidateOrThrow();
            CompoundGenerationMatrix.ValidateOrThrow();
            DynamicReactionEngine.ValidateOrThrow();
            AirborneHazardCatalog.ValidateOrThrow();
            ReactionConditionEngine.ValidateOrThrow();
            RedoxReactionEngine.ValidateOrThrow();
            SynthesizedInventory.ValidateOrThrow();
            LabSafetySystem.ValidateOrThrow();
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
            if (hoodRule.Status != ReactionStatus.Reaction
                || !hoodRule.SafetyViolation
                || hoodRule.Hazard == null
                || hoodRule.Hazard.Severity != HazardSeverity.Critical)
            {
                throw new InvalidOperationException("Fume-hood safety validation failed.");
            }

            var generated = ReactionSimulator.Evaluate(
                new[]
                {
                    new VesselAddition("nitric-acid", 10d),
                    new VesselAddition("calcium-hydroxide", 10d)
                },
                LabStation.Workbench,
                24f);
            if (generated.Status != ReactionStatus.Reaction
                || !generated.GeneratedByRule
                || !string.Equals(generated.RuleFamily, "acid-base", StringComparison.Ordinal)
                || generated.ProductConfidence != CompoundConfidence.RuleDerived
                || string.IsNullOrWhiteSpace(generated.GeneratedPropertyBasis)
                || generated.Equation.IndexOf("Ca(NO₃)₂", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Dynamic reaction fallback validation failed.");
            }

            var generatedToxicGas = ReactionSimulator.Evaluate(
                new[]
                {
                    new VesselAddition("phosphoric-acid", 10d),
                    new VesselAddition("sodium-sulfide", 10d)
                },
                LabStation.Workbench,
                24f);
            if (generatedToxicGas.Status != ReactionStatus.Reaction
                || !generatedToxicGas.GeneratedByRule
                || !string.Equals(
                    generatedToxicGas.RuleFamily,
                    "acid-sulfide",
                    StringComparison.Ordinal)
                || generatedToxicGas.ProductConfidence != CompoundConfidence.Reviewed
                || generatedToxicGas.Hazard == null
                || generatedToxicGas.Hazard.Severity != HazardSeverity.Critical
                || !generatedToxicGas.SafetyViolation)
            {
                throw new InvalidOperationException(
                    "Generated compound safety integration validation failed.");
            }

            var hotSulfuricAdditions = new[]
            {
                new VesselAddition("copper", 6.3546d),
                new VesselAddition("sulfuric-acid", 98.079d)
            };
            var coldSulfuric = ReactionSimulator.Evaluate(
                hotSulfuricAdditions,
                LabStation.FumeHood,
                new ReactionEnvironment(24f, .100d));
            var hotSulfuric = ReactionSimulator.Evaluate(
                hotSulfuricAdditions,
                LabStation.FumeHood,
                new ReactionEnvironment(90f, .100d));
            if (coldSulfuric.Status != ReactionStatus.Blocked
                || hotSulfuric.Status != ReactionStatus.Reaction
                || !hotSulfuric.IsRedox
                || hotSulfuric.ElectronTransferCount != 2
                || hotSulfuric.Hazard == null
                || !hotSulfuric.CanCollectProduct)
            {
                throw new InvalidOperationException(
                    "Temperature/concentration/redox condition validation failed.");
            }

            var hoodReactionCount = 0;
            var dynamicResolvedPairs = ValidateDynamicMatrix();
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
                    var unsafeOutcome = ReactionSimulator.Evaluate(additions, LabStation.Workbench, 24f);
                    if (unsafeOutcome.Status != ReactionStatus.Reaction
                        || !unsafeOutcome.SafetyViolation
                        || unsafeOutcome.Reaction == null
                        || !string.Equals(unsafeOutcome.Reaction.Id, reaction.Id, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "Fume-hood rule did not flag an unsafe reaction: " + reaction.Id);
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
                + " dynamicFamilies=" + DynamicReactionEngine.RuleFamilyCount
                + " dynamicPairs=" + dynamicResolvedPairs
                + " generatedCompounds=" + CompoundGenerationMatrix.AcceptedCompoundCount
                + " uniqueFormulas=" + CompoundGenerationMatrix.UniqueFormulaCount
                + " effects=" + effects.Count
                + " audioSignals=5");

            return new ValidationSummary
            {
                elements = HighSchoolPeriodicTable.All.Count,
                chemicals = DesktopChemistryDatabase.AllChemicals.Count,
                reactions = DesktopChemistryDatabase.AllReactions.Count,
                dynamicSpecies = DynamicReactionEngine.SupportedSpeciesCount,
                dynamicRuleFamilies = DynamicReactionEngine.RuleFamilyCount,
                conditionProfiles = ReactionConditionEngine.ProfileCount,
                redoxRules = RedoxReactionEngine.RuleCount,
                dynamicResolvedPairs = dynamicResolvedPairs,
                generatedCompounds = CompoundGenerationMatrix.AcceptedCompoundCount,
                uniqueGeneratedFormulas = CompoundGenerationMatrix.UniqueFormulaCount,
                reviewedGeneratedCompounds = CompoundGenerationMatrix.ReviewedCompoundCount,
                compoundMatrixElements = CompoundGenerationMatrix.Elements.Count,
                compoundMatrixIons = CompoundGenerationMatrix.Ions.Count,
                fumeHoodRules = hoodReactionCount,
                effectClasses = effects.Count,
                proceduralAudioSignalClasses = 5,
                reactantOrdersPerReaction = 2
            };
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

        private static int ValidateDynamicMatrix()
        {
            var chemicals = DesktopChemistryDatabase.AllChemicals;
            var resolved = 0;
            for (var left = 0; left < chemicals.Count - 1; left++)
            {
                for (var right = left + 1; right < chemicals.Count; right++)
                {
                    var mixture = new System.Collections.Generic.Dictionary<string, double>(
                        StringComparer.Ordinal)
                    {
                        { chemicals[left].Id, 10d },
                        { chemicals[right].Id, 10d }
                    };
                    ReactionDefinition generated;
                    string family;
                    if (!DynamicReactionEngine.TryResolve(mixture, out generated, out family))
                    {
                        continue;
                    }

                    if (generated == null
                        || string.IsNullOrWhiteSpace(family)
                        || generated.CoefficientA <= 0d
                        || generated.CoefficientB <= 0d
                        || generated.ProductMolarMass <= 0d
                        || string.IsNullOrWhiteSpace(generated.Equation))
                    {
                        throw new InvalidOperationException(
                            "Invalid dynamic reaction for "
                            + chemicals[left].Id + " + " + chemicals[right].Id);
                    }

                    resolved++;
                }
            }

            if (resolved < 100)
            {
                throw new InvalidOperationException(
                    "Dynamic reaction coverage is unexpectedly low: " + resolved + " pairs.");
            }

            return resolved;
        }

        private static void EnsureRuntimeMaterial()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "ChemistryLab", "Resources"));
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

        private static void WriteStructuredReport(
            string fileName,
            string phase,
            string result,
            ValidationSummary validation,
            int warnings,
            int errors,
            long sizeBytes,
            string outputPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var absoluteReportDirectory = Path.Combine(projectRoot, ReportDirectory);
            var absoluteReportPath = Path.Combine(absoluteReportDirectory, fileName);
            Directory.CreateDirectory(absoluteReportDirectory);

            var document = new StructuredBuildReport
            {
                schemaVersion = "1.0",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                phase = phase,
                result = result,
                platform = "Windows Standalone x64",
                scene = ScenePath,
                output = outputPath,
                warnings = warnings,
                errors = errors,
                sizeBytes = sizeBytes,
                validation = validation
            };
            File.WriteAllText(
                absoluteReportPath,
                JsonUtility.ToJson(document, true) + Environment.NewLine);
            Debug.Log("DESKTOP_LAB_JSON_REPORT path=" + absoluteReportPath);
        }

        [Serializable]
        private sealed class StructuredBuildReport
        {
            public string schemaVersion;
            public string generatedAtUtc;
            public string unityVersion;
            public string phase;
            public string result;
            public string platform;
            public string scene;
            public string output;
            public int warnings;
            public int errors;
            public long sizeBytes;
            public ValidationSummary validation;
        }

        [Serializable]
        private sealed class ValidationSummary
        {
            public int elements;
            public int chemicals;
            public int reactions;
            public int dynamicSpecies;
            public int dynamicRuleFamilies;
            public int conditionProfiles;
            public int redoxRules;
            public int dynamicResolvedPairs;
            public int generatedCompounds;
            public int uniqueGeneratedFormulas;
            public int reviewedGeneratedCompounds;
            public int compoundMatrixElements;
            public int compoundMatrixIons;
            public int fumeHoodRules;
            public int effectClasses;
            public int proceduralAudioSignalClasses;
            public int reactantOrdersPerReaction;
        }
    }
}
