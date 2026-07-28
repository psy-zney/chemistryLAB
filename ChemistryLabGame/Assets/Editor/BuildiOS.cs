using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Exports the Unity player as an Xcode project. Run on macOS with Unity's iOS
/// Build Support installed; Xcode is then responsible for signing and creating the IPA.
/// </summary>
public static class BuildiOS
{
    public static void Build()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        const string outputPath = "Builds/iOS/ChemistryLabMobile";

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (UnityEngine.Object.FindAnyObjectByType<ChemistryLabGame>() == null)
            new GameObject("ChemistryLabGame").AddComponent<ChemistryLabGame>();
        EditorSceneManager.SaveScene(scene);

        PlayerSettings.companyName = "Chemistry Lab";
        PlayerSettings.productName = "Chemistry Lab Mobile";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.chemistrylab.mobile");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.iOS.targetOSVersionString = "13.0";

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "Builds/iOS");
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
            throw new Exception("Could not switch to iOS. Install Unity iOS Build Support and run this export on macOS.");

        var options = new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = outputPath,
            targetGroup = BuildTargetGroup.iOS,
            target = BuildTarget.iOS,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("iOS Xcode export failed: " + report.summary.result);
    }
}
