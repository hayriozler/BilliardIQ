using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidExporter
{
    private const string _exportPath = @"D:\SourceCodes\BilliardIQ\UnityExport";
    private const string _scene = "Assets/Scenes/BillardGame.unity";

    public static void Export()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { _scene },
            locationPathName = _exportPath,
            target = BuildTarget.Android,
            options = BuildOptions.AcceptExternalModificationsToPlayer
        });

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[AndroidExporter] Export succeeded.");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("[AndroidExporter] Export FAILED.");
            EditorApplication.Exit(1);
        }
    }
}
