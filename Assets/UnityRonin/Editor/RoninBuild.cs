using System;
using UnityEditor;
using UnityEngine;

namespace UnityRonin.Editor
{
    /// <summary>Batch-mode entry points for the test gate (tools/test.ps1, tools/unity.ps1, tools/autoplay.ps1).</summary>
    public static class RoninBuild
    {
        private const string PlayerPath = "Build/Windows/UnityRonin.exe";

        [MenuItem("UnityRonin/Compile Check")]
        public static void CompileCheck()
        {
            Debug.Log("[UnityRonin] compile check passed");
        }

        /// <summary>
        /// Builds a Windows standalone player of Demo.unity so tools/autoplay.ps1 can run it with
        /// "-batchmode -nographics -autoplay": a desktop build is enough, since RoninAutopilot drives the
        /// game through the same pointer-handler methods a touchscreen would call, never real hardware
        /// input, so the lack of a touchscreen on this build target doesn't matter.
        /// </summary>
        [MenuItem("UnityRonin/Build Windows Player")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/UnityRonin/Scenes/Demo.unity" },
                locationPathName = PlayerPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception($"[UnityRonin] player build failed: {report.summary.result}, {report.summary.totalErrors} error(s)");

            Debug.Log($"[UnityRonin] Windows player built: {PlayerPath}");
        }
    }
}
