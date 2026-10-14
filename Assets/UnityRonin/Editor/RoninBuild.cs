using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityRonin.Editor
{
    /// <summary>Batch-mode entry points for the test gate (tools/test.ps1, tools/unity.ps1, tools/autoplay.ps1).</summary>
    public static class RoninBuild
    {
        private const string PlayerPath = "Build/Windows/UnityRonin.exe";
        private const string AndroidPath = "Build/Android/UnityRonin.apk";
        private const string SettingsDir = "Assets/UnityRonin/Generated/Settings";
        private const string PipelinePath = SettingsDir + "/UnityRonin_URP.asset";
        private const string RendererPath = SettingsDir + "/UnityRonin_Renderer.asset";

        [MenuItem("UnityRonin/Compile Check")]
        public static void CompileCheck()
        {
            Debug.Log("[UnityRonin] compile check passed");
        }

        /// <summary>
        /// ProjectSettings/GraphicsSettings.asset and QualitySettings.asset were copied wholesale from
        /// UnityEve at scaffold time and still pointed at UnityEve's own URP pipeline asset GUID — one
        /// that doesn't exist in this project. With no render pipeline asset actually resolvable, URP's
        /// shader stripper couldn't tell which variants were live and kept ~1.7M "ForwardLit" variants (a
        /// ~36 GB, 15+ minute player build that also pressured system memory), and lighting/exposure
        /// behaved unpredictably (the ground rendering as a blown-out white specular highlight on a real
        /// device). This creates a fresh, minimal URP asset for UnityRonin and wires it in everywhere.
        /// Idempotent: a second run is a no-op once the asset exists and is assigned.
        /// </summary>
        [MenuItem("UnityRonin/Ensure Render Pipeline")]
        public static void EnsureRenderPipeline()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset existing
                && AssetDatabase.GetAssetPath(existing) == PipelinePath)
            {
                Debug.Log("[UnityRonin] Render pipeline already set up.");
                return;
            }

            System.IO.Directory.CreateDirectory(SettingsDir);

            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, RendererPath);

            var pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            int originalLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipelineAsset;
            }
            QualitySettings.SetQualityLevel(originalLevel, applyExpensiveChanges: false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UnityRonin] Render pipeline asset created and assigned: {PipelinePath}");
        }

        /// <summary>
        /// Builds a Windows standalone player of Demo.unity so tools/autoplay.ps1 can run it headless with
        /// "-batchmode -nographics -demo", or tools/demo.ps1 can run it windowed with "-demo" to watch it:
        /// a desktop build is enough, since RoninAutopilot drives the game through the same pointer-handler
        /// methods a touchscreen would call, never real hardware input, so the lack of a touchscreen on
        /// this build target doesn't matter.
        /// </summary>
        [MenuItem("UnityRonin/Build Windows Player")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/UnityRonin/Scenes/Demo.unity" },
                locationPathName = PlayerPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                // This is the playtest/demo player, never a public release build, so every run (manual play
                // or -demo) gets MindAttic.Capture.
                extraScriptingDefines = new[] { "MINDATTIC_CAPTURE" }
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception($"[UnityRonin] player build failed: {report.summary.result}, {report.summary.totalErrors} error(s)");

            Debug.Log($"[UnityRonin] Windows player built: {PlayerPath}");
        }

        /// <summary>
        /// Builds a real, installable Android APK of Demo.unity with real touch input (no "-demo"
        /// shortcut) — this is for an owner to sideload onto their own phone, since no scripted run can
        /// substitute for a human thumb's judgment of whether the gestures feel right.
        /// </summary>
        [MenuItem("UnityRonin/Build Android APK")]
        public static void BuildAndroid()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/UnityRonin/Scenes/Demo.unity" },
                locationPathName = AndroidPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception($"[UnityRonin] Android build failed: {report.summary.result}, {report.summary.totalErrors} error(s)");

            Debug.Log($"[UnityRonin] Android APK built: {AndroidPath}");
        }
    }
}
