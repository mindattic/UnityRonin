using System;
using System.Collections;
using UnityEngine;
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

            yield return SelfPlayInput.FightDuel(_duelPresenter, _swipeReader, alwaysLose: true,
                onRanTooLong: beats => Debug.LogWarning($"[UnityRonin] autoplay: duel against {enemy.name} ran {beats} beats without ending"));
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

            yield return SelfPlayInput.FightDuel(_duelPresenter, _swipeReader, alwaysLose: false,
                onRanTooLong: beats => Debug.LogWarning($"[UnityRonin] autoplay: duel against {enemy.name} ran {beats} beats without ending"));

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

        private IEnumerator WalkToward(Transform target, float timeoutSeconds) =>
            SelfPlayInput.WalkToward(_joystick, _locomotion, _cameraYawSource, _player, target, _duelPresenter,
                timeoutSeconds, logTag: "autoplay");

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
