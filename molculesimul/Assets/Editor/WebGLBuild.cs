using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    private const string ScenePath = "Assets/Scenes/MoleculeSimulator.unity";
    private const string OutputPath = "../docs/webgl";

    [MenuItem("Tools/Molecule Simulator/Build WebGL")]
    public static void Build()
    {
        if (!File.Exists(ScenePath))
            MoleculeSceneBuilder.BuildCompleteScene();

        Directory.CreateDirectory(OutputPath);

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        PlayerSettings.productName = "Molecule Simulator";
        PlayerSettings.companyName = "Science Class";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.memorySize = 128;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("WebGL build failed: " + report.summary.result);

        Debug.Log($"WebGL build complete: {OutputPath}");
    }
}

