using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityRonin.Core.Combat;
using UnityRonin.Runtime.Combat;
using UnityRonin.Runtime.Input;
using UnityRonin.Runtime.Player;

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
        [SerializeField] private SamuraiLocomotion _locomotion;
        [SerializeField] private Transform _cameraYawSource;
        [SerializeField] private EnemyEngageZone[] _enemies;

        private const float DragRadiusPixels = 140f;
        private bool _failed;

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

            if (_enemies.Length > 0 && _enemies[0] != null)
            {
                yield return VerifyDefeatRetreats(_enemies[0]);
                if (_failed) yield break;
            }

            foreach (var enemy in _enemies)
            {
                if (enemy == null) continue;

                yield return EngageAndWin(enemy);
                if (_failed) yield break;

                Debug.Log($"[UnityRonin] autoplay: defeated {enemy.name}");
            }

            Debug.Log("[UnityRonin] autoplay PASS: all enemies defeated");
            Quit(0);
        }

        /// <summary>Deliberately loses one duel to prove a loss knocks the player back out of range
        /// instead of the duel silently re-triggering the instant control returns.</summary>
        private IEnumerator VerifyDefeatRetreats(EnemyEngageZone enemy)
        {
            yield return WalkToward(enemy.transform, timeoutSeconds: 15f);
            yield return WaitForDuelToStart(enemy.name);
            if (_failed) yield break;

            yield return FightDuel(enemy.name, alwaysLose: true);
            yield return null; // let OnDuelEnded's retreat apply before checking

            float distance = Vector3.Distance(_player.position, enemy.transform.position);
            if (_duelPresenter.IsActive || distance < enemy.ExitRadius)
            {
                Debug.LogError($"[UnityRonin] autoplay FAIL: losing did not retreat the player out of range (distance={distance}, exitRadius={enemy.ExitRadius})");
                Fail();
                yield break;
            }
            if (!enemy.gameObject.activeInHierarchy)
            {
                Debug.LogError("[UnityRonin] autoplay FAIL: the enemy was defeated despite the player losing");
                Fail();
                yield break;
            }

            Debug.Log($"[UnityRonin] autoplay: confirmed a loss retreats the player (distance={distance})");
        }

        private IEnumerator EngageAndWin(EnemyEngageZone enemy)
        {
            yield return WalkToward(enemy.transform, timeoutSeconds: 15f);
            yield return WaitForDuelToStart(enemy.name);
            if (_failed) yield break;

            yield return FightDuel(enemy.name, alwaysLose: false);

            if (enemy.gameObject.activeInHierarchy)
            {
                Debug.LogError($"[UnityRonin] autoplay FAIL: {enemy.name} still standing after its duel");
                Fail();
            }
        }

        private IEnumerator WaitForDuelToStart(string enemyName)
        {
            float waited = 0f;
            while (!_duelPresenter.IsActive && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (!_duelPresenter.IsActive)
            {
                Debug.LogError($"[UnityRonin] autoplay FAIL: duel never started against {enemyName}");
                Fail();
            }
        }

        private void Fail()
        {
            _failed = true;
            Quit(1);
        }

        private IEnumerator WalkToward(Transform target, float timeoutSeconds)
        {
            float elapsed = 0f;
            float nextLog = 0f;
            Vector2 origin = new Vector2(Screen.width * 0.5f, Screen.height * 0.25f);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 0, position = origin };
            _joystick.OnPointerDown(pointer);

            while (elapsed < timeoutSeconds && !_duelPresenter.IsActive)
            {
                Vector3 toTarget = target.position - _player.position;
                toTarget.y = 0f;

                if (elapsed >= nextLog)
                {
                    Debug.Log($"[UnityRonin] autoplay walk t={elapsed:F1} playerPos={_player.position} dist={toTarget.magnitude:F2} stick={_joystick.Current.X:F2},{_joystick.Current.Y:F2} horiz={_locomotion.DebugLastHorizontal} vel={_locomotion.DebugControllerVelocity} grounded={_locomotion.DebugIsGrounded} dt={Time.deltaTime:F4}");
                    nextLog += 2f;
                }

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

        private IEnumerator FightDuel(string enemyName, bool alwaysLose)
        {
            int beats = 0;
            while (_duelPresenter.IsActive && beats < 100)
            {
                var beat = _duelPresenter.PeekCurrentBeat;
                yield return new WaitForSeconds(0.1f); // let the telegraph show before answering it
                if (!_duelPresenter.IsActive) break;
                if (beat.HasValue)
                {
                    var direction = alwaysLose ? Opposite(beat.Value.Direction) : beat.Value.Direction;
                    yield return Swipe(direction);
                }
                beats++;
                yield return null;
            }

            if (beats >= 100)
                Debug.LogWarning($"[UnityRonin] autoplay: duel against {enemyName} ran {beats} beats without ending");
        }

        private static SwipeDirection Opposite(SwipeDirection direction) => direction switch
        {
            SwipeDirection.Up => SwipeDirection.Down,
            SwipeDirection.Down => SwipeDirection.Up,
            SwipeDirection.Left => SwipeDirection.Right,
            SwipeDirection.Right => SwipeDirection.Left,
            _ => SwipeDirection.Up
        };

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
