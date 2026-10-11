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
    /// a duel telegraph, and two enemy swordsmen. Change this builder, not the generated scene.
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
        private static Material MakeMatteMaterial(Color color)
        {
            const string dir = "Assets/UnityRonin/Generated/Materials";
            System.IO.Directory.CreateDirectory(dir);
            string path = $"{dir}/Matte_{ColorUtility.ToHtmlStringRGB(color)}.mat";

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Smoothness", 0.05f);
            material.SetFloat("_Metallic", 0f);
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
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Samurai";
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.GetComponent<Renderer>().sharedMaterial = MakeMatteMaterial(new Color(0.75f, 0.73f, 0.68f));
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());

            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = new Vector3(0f, 1f, 0f);

            // Cosmetic props only: a BoxCollider parented inside the player's own CharacterController
            // radius self-collides with it and can jam movement entirely, so both colliders are removed.
            var sheathed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sheathed.name = "KatanaSheathed";
            sheathed.transform.SetParent(player.transform);
            sheathed.transform.localPosition = new Vector3(0.4f, 0.2f, -0.2f);
            sheathed.transform.localScale = new Vector3(0.1f, 0.6f, 0.1f);
            Object.DestroyImmediate(sheathed.GetComponent<BoxCollider>());

            var drawn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drawn.name = "KatanaDrawn";
            drawn.transform.SetParent(player.transform);
            drawn.transform.localPosition = new Vector3(0.5f, 0.6f, 0.5f);
            drawn.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            drawn.transform.localScale = new Vector3(0.08f, 1f, 0.08f);
            Object.DestroyImmediate(drawn.GetComponent<BoxCollider>());
            drawn.SetActive(false);

            player.AddComponent<SamuraiLocomotion>();
            var weapon = player.AddComponent<SamuraiWeapon>();
            BindPrivate(weapon, "_sheathedKatana", sheathed);
            BindPrivate(weapon, "_drawnKatana", drawn);

            return player;
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

            // Duel telegraph arrow, top-center.
            var telegraphGo = new GameObject("DuelTelegraph", typeof(RectTransform));
            telegraphGo.transform.SetParent(canvasGo.transform, false);
            var telegraphRect = telegraphGo.GetComponent<RectTransform>();
            telegraphRect.anchorMin = telegraphRect.anchorMax = new Vector2(0.5f, 0.85f);
            telegraphRect.sizeDelta = new Vector2(140f, 140f);
            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            arrowGo.transform.SetParent(telegraphGo.transform, false);
            var arrowRect = arrowGo.GetComponent<RectTransform>();
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(100f, 100f);
            var arrowImage = arrowGo.AddComponent<Image>();
            arrowImage.color = Color.white;

            telegraph = telegraphGo.AddComponent<DuelTelegraph>();
            BindPrivate(telegraph, "_arrow", arrowRect);
            BindPrivate(telegraph, "_arrowImage", arrowImage);
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
            return enemy;
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
