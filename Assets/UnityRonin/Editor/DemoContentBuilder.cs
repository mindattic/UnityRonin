using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityRonin.Runtime.Camera;
using UnityRonin.Runtime.Combat;
using UnityRonin.Runtime.Game;
using UnityRonin.Runtime.Input;
using UnityRonin.Runtime.Player;

namespace UnityRonin.Editor
{
    /// <summary>
    /// Builds Demo.unity from primitives, deterministically, every time it runs: a ground plane, a
    /// Samurai, a smart-follow camera, the bottom-half transparent joystick, a full-screen swipe zone,
    /// a duel telegraph, an engage-radius warning ring, and two enemy swordsmen. Change this builder, not
    /// the generated scene.
    /// </summary>
    public static class DemoContentBuilder
    {
        private const string ScenePath = "Assets/UnityRonin/Scenes/Demo.unity";

        [MenuItem("UnityRonin/Build Demo Scene")]
        public static void BuildAll()
        {
            RoninBuild.EnsureRenderPipeline();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildGround();
            BuildLight();
            var player = BuildPlayer();
            var camera = BuildCamera(player.transform);
            BindPrivate(player.GetComponent<SamuraiLocomotion>(), "_cameraYawSource", camera.transform);
            var canvas = BuildCanvas(out var joystick, out var swipeZone, out var swipeReader, out var telegraph);
            var enemies = new[] { BuildEnemy("Enemy Swordsman 1", new Vector3(6f, 0.9f, 4f)), BuildEnemy("Enemy Swordsman 2", new Vector3(-7f, 0.9f, 9f)) };

            BuildGameController(player, camera, joystick, swipeReader, telegraph, joystick.gameObject, swipeZone, enemies);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            System.IO.Directory.CreateDirectory("Assets/UnityRonin/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[UnityRonin] Demo scene rebuilt at {ScenePath}");
        }

        private static void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            ground.GetComponent<Renderer>().sharedMaterial = MakeMatteMaterial(new Color(0.33f, 0.38f, 0.3f));
        }

        /// <summary>
        /// Default-Material's smoothness produces a huge specular highlight across a flat plane this size
        /// under a directional light — on a real device this blows the ground out to solid white (found by
        /// an actual on-device screenshot; -nographics batch mode never renders a pixel, so it never
        /// caught this). A near-zero-smoothness, non-metallic material avoids it regardless of exposure.
        ///
        /// Saved as a real .mat asset, not a bare `new Material(...)`: an in-memory-only material built at
        /// build time left the URP shader stripper unable to tell which keyword variants were actually
        /// live, and it kept ~1.7M "ForwardLit" variants (a ~36 GB, 15+ minute player build) rather than
        /// the handful this scene actually uses.
        /// </summary>
        private static Material MakeMatteMaterial(Color color, float smoothness = 0.05f)
        {
            const string dir = "Assets/UnityRonin/Generated/Materials";
            System.IO.Directory.CreateDirectory(dir);
            string path = $"{dir}/Matte_{ColorUtility.ToHtmlStringRGB(color)}_{smoothness:F2}.mat";

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Same asset-backed-material reasoning as <see cref="MakeMatteMaterial"/> (the URP shader stripper
        /// needs a saved asset, not an in-memory material, to know which variants are actually live), but
        /// for the engage-radius ground ring: Unlit so it isn't affected by the scene's directional light,
        /// alpha-blended so it reads as a translucent warning disc rather than a solid shape.
        /// </summary>
        private static Material MakeTransparentMaterial(Color color)
        {
            const string dir = "Assets/UnityRonin/Generated/Materials";
            System.IO.Directory.CreateDirectory(dir);
            string path = $"{dir}/Transparent_{ColorUtility.ToHtmlStringRGBA(color)}.mat";

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetFloat("_Surface", 1f); // Transparent
            material.SetFloat("_Blend", 0f); // Alpha
            material.SetFloat("_Cull", 2f); // Back
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildLight()
        {
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static GameObject BuildPlayer()
        {
            var player = new GameObject("Samurai");
            player.transform.position = new Vector3(0f, 1f, 0f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = new Vector3(0f, 1f, 0f);

            // The visual mesh lives on its own child, thinner than the default capsule's girth, so the
            // collision envelope above (sized for comfortable movement) can stay as wide as it needs to be
            // without the katana — held close against the torso — reading as embedded inside the body.
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform, false);
            body.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            body.GetComponent<Renderer>().sharedMaterial = MakeMatteMaterial(new Color(0.75f, 0.73f, 0.68f));
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

            // Cosmetic props only, never colliders: a collider parented inside the player's own
            // CharacterController radius self-collides with it and can jam movement entirely.
            var sheathed = BuildKatana("KatanaSheathed", player.transform,
                localPosition: new Vector3(0.4f, 0.3f, -0.25f), localRotation: Quaternion.Euler(0f, 0f, 10f));

            var drawn = BuildKatana("KatanaDrawn", player.transform,
                localPosition: new Vector3(0.35f, 0.85f, 0.25f), localRotation: Quaternion.Euler(-70f, 15f, -15f));
            drawn.SetActive(false);

            player.AddComponent<SamuraiLocomotion>();
            var weapon = player.AddComponent<SamuraiWeapon>();
            BindPrivate(weapon, "_sheathedKatana", sheathed);
            BindPrivate(weapon, "_drawnKatana", drawn);

            return player;
        }

        /// <summary>
        /// A katana from primitives — steel blade, gold tsuba, dark wrapped grip — ported from UnityEve's
        /// own primitive katana (its local +Y also runs grip-to-tip, so the dimensions transfer directly):
        /// a plain flat cube reads as a bat, not a blade, once the camera sits this close.
        /// </summary>
        private static GameObject BuildKatana(string name, Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            var steel = MakeMatteMaterial(new Color(0.86f, 0.9f, 0.95f), smoothness: 0.85f);
            var wrap = MakeMatteMaterial(new Color(0.09f, 0.06f, 0.08f));
            var gold = MakeMatteMaterial(new Color(0.78f, 0.6f, 0.22f), smoothness: 0.6f);

            var katana = new GameObject(name);
            katana.transform.SetParent(parent, false);
            katana.transform.localPosition = localPosition;
            katana.transform.localRotation = localRotation;

            void Part(PrimitiveType type, string partName, Vector3 pos, Vector3 scale, Material mat)
            {
                var go = GameObject.CreatePrimitive(type);
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.name = partName;
                go.transform.SetParent(katana.transform, false);
                go.transform.localPosition = pos;
                go.transform.localScale = scale;
                go.GetComponent<Renderer>().sharedMaterial = mat;
            }
            // Local +Y runs from the grip (origin) to the tip.
            Part(PrimitiveType.Cube, "Grip", Vector3.zero, new Vector3(0.034f, 0.27f, 0.034f), wrap);
            Part(PrimitiveType.Cylinder, "Tsuba", new Vector3(0f, 0.145f, 0f), new Vector3(0.095f, 0.007f, 0.095f), gold);
            Part(PrimitiveType.Cube, "Blade", new Vector3(0f, 0.55f, 0f), new Vector3(0.012f, 0.79f, 0.034f), steel);

            return katana;
        }

        private static GameObject BuildCamera(Transform player)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            var follow = go.AddComponent<SmartFollowCamera>();
            BindPrivate(follow, "_player", player);
            return go;
        }

        private static GameObject BuildCanvas(out VirtualJoystick joystick, out GameObject swipeZone,
            out SwipeInputReader swipeReader, out DuelTelegraph telegraph)
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasGo.AddComponent<GraphicRaycaster>();

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Bottom-half transparent joystick zone.
            var joystickZone = new GameObject("JoystickZone", typeof(RectTransform));
            joystickZone.transform.SetParent(canvasGo.transform, false);
            var zoneRect = joystickZone.GetComponent<RectTransform>();
            zoneRect.anchorMin = new Vector2(0f, 0f);
            zoneRect.anchorMax = new Vector2(1f, 0.5f);
            zoneRect.offsetMin = Vector2.zero;
            zoneRect.offsetMax = Vector2.zero;
            var zoneImage = joystickZone.AddComponent<Image>();
            zoneImage.color = new Color(0f, 0f, 0f, 0.01f); // effectively invisible but still raycastable

            // The fade group lives on a separate cosmetic child, never on JoystickZone itself: a
            // CanvasGroup with blocksRaycasts=false disables raycasting for its WHOLE subtree, including
            // the object it sits on, which would have silently swallowed every touch on the zone's own
            // Image and defeated the stick entirely.
            var visualGo = new GameObject("Visual", typeof(RectTransform));
            visualGo.transform.SetParent(joystickZone.transform, false);
            var visualRect = visualGo.GetComponent<RectTransform>();
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.offsetMin = Vector2.zero;
            visualRect.offsetMax = Vector2.zero;
            var visualGroup = visualGo.AddComponent<CanvasGroup>();
            visualGroup.alpha = 0f;
            visualGroup.interactable = false;
            visualGroup.blocksRaycasts = false;

            var background = CreateCircle("StickBackground", visualGo.transform, 220f, new Color(1f, 1f, 1f, 0.25f));
            var knob = CreateCircle("StickKnob", background.transform, 90f, new Color(1f, 1f, 1f, 0.55f));

            joystick = joystickZone.AddComponent<VirtualJoystick>();
            BindPrivate(joystick, "_background", background.GetComponent<RectTransform>());
            BindPrivate(joystick, "_knob", knob.GetComponent<RectTransform>());
            BindPrivate(joystick, "_visual", visualGroup);

            // Full-screen swipe zone used only while a duel is active.
            swipeZone = new GameObject("SwipeZone", typeof(RectTransform));
            swipeZone.transform.SetParent(canvasGo.transform, false);
            var swipeRect = swipeZone.GetComponent<RectTransform>();
            swipeRect.anchorMin = Vector2.zero;
            swipeRect.anchorMax = Vector2.one;
            swipeRect.offsetMin = Vector2.zero;
            swipeRect.offsetMax = Vector2.zero;
            var swipeImage = swipeZone.AddComponent<Image>();
            swipeImage.color = new Color(0f, 0f, 0f, 0.01f);
            swipeReader = swipeZone.AddComponent<SwipeInputReader>();
            swipeZone.SetActive(false);

            // Duel telegraph: a real calligraphy-brush glyph (Yuji Syuku, OFL-licensed — see CREDITS.md) in
            // the shape of the gesture symbol the player must answer, brushed on top-center.
            var telegraphGo = new GameObject("DuelTelegraph", typeof(RectTransform));
            telegraphGo.transform.SetParent(canvasGo.transform, false);
            var telegraphRect = telegraphGo.GetComponent<RectTransform>();
            telegraphRect.anchorMin = telegraphRect.anchorMax = new Vector2(0.5f, 0.85f);
            telegraphRect.sizeDelta = new Vector2(140f, 140f);
            var strokeGo = new GameObject("Stroke", typeof(RectTransform));
            strokeGo.transform.SetParent(telegraphGo.transform, false);
            var strokeRect = strokeGo.GetComponent<RectTransform>();
            strokeRect.anchorMin = Vector2.zero;
            strokeRect.anchorMax = Vector2.one;
            strokeRect.offsetMin = Vector2.zero;
            strokeRect.offsetMax = Vector2.zero;
            var strokeText = strokeGo.AddComponent<TextMeshProUGUI>();
            strokeText.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UnityRonin/Fonts/YujiSyuku SDF.asset");
            strokeText.fontSize = 110f;
            strokeText.alignment = TextAlignmentOptions.Center;
            strokeText.color = Color.white;

            telegraph = telegraphGo.AddComponent<DuelTelegraph>();
            BindPrivate(telegraph, "_stroke", strokeRect);
            BindPrivate(telegraph, "_strokeText", strokeText);
            telegraphGo.SetActive(false);

            return canvasGo;
        }

        private static GameObject CreateCircle(string name, Transform parent, float sizePixels, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(sizePixels, sizePixels);
            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static GameObject BuildEnemy(string name, Vector3 position)
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = name;
            enemy.transform.position = position;
            enemy.GetComponent<Renderer>().sharedMaterial = MakeMatteMaterial(new Color(0.6f, 0.15f, 0.15f));
            enemy.AddComponent<EnemyEngageZone>();
            enemy.AddComponent<EnemyCombatFeedback>();
            return enemy;
        }

        /// <summary>
        /// A flat, transparent disc that tracks the player on the ground, sized to the engage-enter radius
        /// so a player can see where the duel will actually trigger instead of relying on the invisible-
        /// at-runtime <see cref="EnemyEngageZone"/> gizmo.
        /// </summary>
        private static GameObject BuildEngageRing()
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "EngageRadiusRing";
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.position = new Vector3(0f, 0.02f, 0f); // just above the ground, avoids z-fighting
            ring.transform.localScale = new Vector3(1f, 0.01f, 1f); // diameter set at runtime from EnterRadius
            var renderer = ring.GetComponent<Renderer>();
            renderer.sharedMaterial = MakeTransparentMaterial(new Color(1f, 0.3f, 0.1f, 0.35f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ring;
        }

        private static void BuildGameController(GameObject player, GameObject camera, VirtualJoystick joystick,
            SwipeInputReader swipeReader, DuelTelegraph telegraph, GameObject joystickZone, GameObject swipeZone,
            GameObject[] enemies)
        {
            var controllerGo = new GameObject("GameController");
            var controller = controllerGo.AddComponent<RoninGameController>();
            var duelPresenterGo = new GameObject("DuelPresenter");
            var presenter = duelPresenterGo.AddComponent<DuelPresenter>();
            BindPrivate(presenter, "_swipeReader", swipeReader);
            BindPrivate(presenter, "_telegraph", telegraph);
            BindPrivate(presenter, "_weapon", player.GetComponent<SamuraiWeapon>());

            var zones = new EnemyEngageZone[enemies.Length];
            for (int i = 0; i < enemies.Length; i++) zones[i] = enemies[i].GetComponent<EnemyEngageZone>();

            var ring = BuildEngageRing();
            var engageRingIndicator = ring.AddComponent<EngageRadiusIndicator>();
            BindPrivate(engageRingIndicator, "_player", player.transform);
            BindPrivate(engageRingIndicator, "_duelPresenter", presenter);
            BindPrivate(engageRingIndicator, "_enemies", zones);

            BindPrivate(controller, "_locomotion", player.GetComponent<SamuraiLocomotion>());
            BindPrivate(controller, "_weapon", player.GetComponent<SamuraiWeapon>());
            BindPrivate(controller, "_camera", camera.GetComponent<SmartFollowCamera>());
            BindPrivate(controller, "_joystick", joystick);
            BindPrivate(controller, "_duelPresenter", presenter);
            BindPrivate(controller, "_joystickZone", joystickZone);
            BindPrivate(controller, "_swipeZone", swipeZone);
            BindPrivate(controller, "_enemies", zones);

            var autopilotGo = new GameObject("Autopilot");
            var autopilot = autopilotGo.AddComponent<RoninAutopilot>();
            BindPrivate(autopilot, "_joystick", joystick);
            BindPrivate(autopilot, "_swipeReader", swipeReader);
            BindPrivate(autopilot, "_duelPresenter", presenter);
            BindPrivate(autopilot, "_player", player.transform);
            BindPrivate(autopilot, "_locomotion", player.GetComponent<SamuraiLocomotion>());
            BindPrivate(autopilot, "_cameraYawSource", camera.transform);
            BindPrivate(autopilot, "_enemies", zones);
        }

        private static void BindPrivate(object target, string field, object value)
        {
            if (target == null) return;
            var so = new SerializedObject((Object)target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[UnityRonin] Missing field {field} on {target.GetType().Name}"); return; }

            switch (value)
            {
                case null: prop.objectReferenceValue = null; break;
                case Object unityObj: prop.objectReferenceValue = unityObj; break;
                case System.Array array:
                    prop.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = (Object)array.GetValue(i);
                    break;
                default: Debug.LogError($"[UnityRonin] Unsupported bind value type {value.GetType()}"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
