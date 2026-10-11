using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace UnityRonin.Editor
{
    /// <summary>
    /// Builds the TMP font asset used by <see cref="UnityRonin.Runtime.Combat.DuelTelegraph"/> from the
    /// free, OFL-licensed Yuji Syuku brush-calligraphy font (see CREDITS.md). Glyphs populate the atlas
    /// dynamically at runtime, so this only needs re-running if the source font file itself changes.
    ///
    /// TMP's own "Import TMP Essential Resources" menu action is what normally supplies the default SDF
    /// shader a new font asset needs, but AssetDatabase.ImportPackage doesn't run in true -batchmode (no
    /// window to pump the import through), so Assets/UnityRonin/Fonts/Shaders carries just the one shader
    /// (TMP_SDF-Mobile.shader, "TextMeshPro/Mobile/Distance Field") and its cginc dependency, copied once
    /// from TMP's bundled Essential Resources package — not the whole default resources set.
    /// </summary>
    public static class CalligraphyFontBuilder
    {
        private const string FontPath = "Assets/UnityRonin/Fonts/YujiSyuku-Regular.ttf";
        private const string OutputPath = "Assets/UnityRonin/Fonts/YujiSyuku SDF.asset";
        private const string ShaderName = "TextMeshPro/Mobile/Distance Field";

        [MenuItem("UnityRonin/Build Calligraphy Font Asset")]
        public static void Build()
        {
            if (Shader.Find(ShaderName) == null)
                throw new System.Exception(
                    $"[UnityRonin] Shader \"{ShaderName}\" not found — is Assets/UnityRonin/Fonts/Shaders/TMP_SDF-Mobile.shader present?");

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) throw new System.Exception($"[UnityRonin] Font not found at {FontPath}");

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font, 90, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fontAsset.name = "YujiSyuku SDF";

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
            if (existing != null) AssetDatabase.DeleteAsset(OutputPath);
            AssetDatabase.CreateAsset(fontAsset, OutputPath);
            // CreateFontAsset builds the atlas texture and material as plain in-memory objects; without
            // persisting them as sub-assets, the font asset reloads from disk with a dangling, unassigned
            // atlas reference (UnassignedReferenceException the first time anything renders with this font).
            foreach (var atlas in fontAsset.atlasTextures)
                if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            if (fontAsset.material != null) AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(OutputPath);
            Debug.Log($"[UnityRonin] Calligraphy font asset built at {OutputPath}");
        }
    }
}
