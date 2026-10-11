# Credits and asset sources

The build is primitives only (capsules, cubes, planes) so the movement and duel rules can be proven
before any art is committed — the one exception is the duel telegraph's font, needed for the gesture
glyphs to read as real calligraphy rather than a drawn rectangle. Nothing here is from Mixamo or a
paid store pack; when real models, rigs, animations, or sound replace the primitives, they will be
listed below with their source and license, following
[MindAttic.HouseRules.md](../MindAttic.HouseRules.md).

| Asset | Source | License | Where |
| --- | --- | --- | --- |
| Yuji Syuku (brush-calligraphy typeface) | [Google Fonts](https://fonts.google.com/specimen/Yuji+Syuku), from [The Yuji Project](https://github.com/Kinutafontfactory/Yuji) | SIL Open Font License 1.1 | `Assets/UnityRonin/Fonts/YujiSyuku-Regular.ttf`, built into `Assets/UnityRonin/Fonts/YujiSyuku SDF.asset` by `CalligraphyFontBuilder`; drives `DuelTelegraph`'s technique glyphs |
| TMP_SDF-Mobile.shader (TextMeshPro's own "TextMeshPro/Mobile/Distance Field" shader) | Unity's `com.unity.ugui` package (bundled "TMP Essential Resources", not otherwise importable in batch mode — see `Assets/UnityRonin/Fonts/Shaders`) | Unity Companion License (part of the Unity Editor/package install, not third-party) | `Assets/UnityRonin/Fonts/Shaders/TMP_SDF-Mobile.shader`, `TMPro_Properties.cginc` |
