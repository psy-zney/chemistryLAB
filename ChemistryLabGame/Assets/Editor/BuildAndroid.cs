using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildAndroid
{
    public static void Build()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (Object.FindAnyObjectByType<ChemistryLabGame>() == null) new GameObject("ChemistryLabGame").AddComponent<ChemistryLabGame>();
        EditorSceneManager.SaveScene(scene);
        PlayerSettings.companyName = "Chemistry Lab";
        PlayerSettings.productName = "Chemistry Lab Mobile";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.chemistrylab.mobile");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android)) throw new System.Exception("Could not switch to Android target.");
        var options = new BuildPlayerOptions { scenes = new[] { scenePath }, locationPathName = "Builds/Android/ChemistryLabMobile.apk", targetGroup = BuildTargetGroup.Android, target = BuildTarget.Android, options = BuildOptions.None };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Android build failed: " + report.summary.result);
    }
}
