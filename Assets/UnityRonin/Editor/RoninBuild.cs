using UnityEditor;
using UnityEngine;

namespace UnityRonin.Editor
{
    /// <summary>Batch-mode entry points for the test gate (tools/test.ps1, tools/unity.ps1).</summary>
    public static class RoninBuild
    {
        [MenuItem("UnityRonin/Compile Check")]
        public static void CompileCheck()
        {
            Debug.Log("[UnityRonin] compile check passed");
        }
    }
}
