using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityRonin.Core.Combat;
using UnityRonin.Runtime.Combat;
using UnityRonin.Runtime.Input;

namespace UnityRonin.Runtime.Game
{
    /// <summary>
    /// Self-play verification for the command-line flag "-autoplay": drives the Samurai toward each
    /// enemy by calling <see cref="VirtualJoystick"/>'s real IPointerDownHandler/IDragHandler methods
    /// (the same ones a touchscreen would call), then answers every duel beat by calling
    /// <see cref="SwipeInputReader"/>'s real pointer handlers with the correct drag. This exercises the
    /// actual compiled Update/coroutine/physics loop end to end, not just a compile check, so it can run
    /// with no touch hardware and no human. Exits 0 only if every enemy is defeated.
    /// </summary>
    public class RoninAutopilot : MonoBehaviour
    {
        [SerializeField] private VirtualJoystick _joystick;
        [SerializeField] private SwipeInputReader _swipeReader;
        [SerializeField] private DuelPresenter _duelPresenter;
        [SerializeField] private Transform _player;
        [SerializeField] private Transform _cameraYawSource;
        [SerializeField] private EnemyEngageZone[] _enemies;

        private const float DragRadiusPixels = 140f;

        private void Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-autoplay") < 0)
            {
                enabled = false;
                return;
            }
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Debug.Log("[UnityRonin] autoplay start");

            foreach (var enemy in _enemies)
            {
                if (enemy == null) continue;

                yield return WalkToward(enemy.transform, timeoutSeconds: 15f);

                float waited = 0f;
                while (!_duelPresenter.IsActive && waited < 3f)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                if (!_duelPresenter.IsActive)
                {
                    Debug.LogError($"[UnityRonin] autoplay FAIL: duel never started against {enemy.name}");
                    Quit(1);
                    yield break;
                }

                yield return FightDuel(enemy.name);

                if (enemy.gameObject.activeInHierarchy)
                {
                    Debug.LogError($"[UnityRonin] autoplay FAIL: {enemy.name} still standing after its duel");
                    Quit(1);
                    yield break;
                }

                Debug.Log($"[UnityRonin] autoplay: defeated {enemy.name}");
            }

            Debug.Log("[UnityRonin] autoplay PASS: all enemies defeated");
            Quit(0);
        }

        private IEnumerator WalkToward(Transform target, float timeoutSeconds)
        {
            float elapsed = 0f;
            Vector2 origin = new Vector2(Screen.width * 0.5f, Screen.height * 0.25f);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 0, position = origin };
            _joystick.OnPointerDown(pointer);

            while (elapsed < timeoutSeconds && !_duelPresenter.IsActive)
            {
                Vector3 toTarget = target.position - _player.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > 0.01f)
                {
                    Vector3 worldDir = toTarget.normalized;
                    float yaw = _cameraYawSource.eulerAngles.y * Mathf.Deg2Rad;
                    // Inverse of CameraRelativeMotion.ToWorldDirection: rotate the desired world heading
                    // back into stick space by the camera's yaw.
                    float stickX = worldDir.x * Mathf.Cos(yaw) - worldDir.z * Mathf.Sin(yaw);
                    float stickY = worldDir.x * Mathf.Sin(yaw) + worldDir.z * Mathf.Cos(yaw);
                    pointer.position = origin + new Vector2(stickX, stickY) * DragRadiusPixels;
                    _joystick.OnDrag(pointer);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            _joystick.OnPointerUp(pointer);
        }

        private IEnumerator FightDuel(string enemyName)
        {
            int beats = 0;
            while (_duelPresenter.IsActive && beats < 100)
            {
                var beat = _duelPresenter.PeekCurrentBeat;
                yield return new WaitForSeconds(0.1f); // let the telegraph show before answering it
                if (!_duelPresenter.IsActive) break;
                if (beat.HasValue) yield return Swipe(beat.Value.Direction);
                beats++;
                yield return null;
            }

            if (beats >= 100)
                Debug.LogWarning($"[UnityRonin] autoplay: duel against {enemyName} ran {beats} beats without ending");
        }

        private IEnumerator Swipe(SwipeDirection direction)
        {
            Vector2 start = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 delta = direction switch
            {
                SwipeDirection.Up => new Vector2(0f, 200f),
                SwipeDirection.Down => new Vector2(0f, -200f),
                SwipeDirection.Left => new Vector2(-200f, 0f),
                SwipeDirection.Right => new Vector2(200f, 0f),
                _ => Vector2.zero
            };

            var pointer = new PointerEventData(EventSystem.current) { pointerId = 1, position = start };
            _swipeReader.OnPointerDown(pointer);
            pointer.position = start + delta;
            _swipeReader.OnPointerUp(pointer);
            yield return null;
        }

        private static void Quit(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
