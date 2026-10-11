using System;
using System.Collections;
using UnityEngine;
using UnityRonin.Runtime.Combat;
using UnityRonin.Runtime.Input;
using UnityRonin.Runtime.Player;

namespace UnityRonin.Runtime.Game
{
    /// <summary>
    /// Showcase mode for the command-line flag "-demo": drives the Samurai through <see cref="SelfPlayInput"/>,
    /// the same real pointer-handler self-play <see cref="RoninAutopilot"/> uses to verify "-autoplay", but
    /// for watching or recording rather than verifying — it never quits and never loses on purpose, and once
    /// every enemy is defeated it reactivates them all and loops forever.
    /// </summary>
    public class RoninDemoMode : MonoBehaviour
    {
        [SerializeField] private VirtualJoystick _joystick;
        [SerializeField] private SwipeInputReader _swipeReader;
        [SerializeField] private DuelPresenter _duelPresenter;
        [SerializeField] private Transform _player;
        [SerializeField] private SamuraiLocomotion _locomotion;
        [SerializeField] private Transform _cameraYawSource;
        [SerializeField] private EnemyEngageZone[] _enemies;

        private Vector3 _spawnPosition;

        private void Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-demo") < 0)
            {
                enabled = false;
                return;
            }
            _spawnPosition = _player.position;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Debug.Log("[UnityRonin] demo mode start");

            while (true)
            {
                var target = NextActiveEnemy();
                if (target == null)
                {
                    yield return new WaitForSeconds(1.5f);
                    // Back to spawn, out of every enemy's exit radius, so the engage gate releases and the
                    // next loop replays the full walk-then-duel sequence instead of re-triggering instantly.
                    _locomotion.Teleport(_spawnPosition);
                    foreach (var enemy in _enemies)
                        if (enemy != null) enemy.gameObject.SetActive(true);
                    Debug.Log("[UnityRonin] demo mode: loop complete, restarting");
                    continue;
                }

                yield return SelfPlayInput.WalkToward(_joystick, _locomotion, _cameraYawSource, _player,
                    target.transform, _duelPresenter, timeoutSeconds: 20f, logTag: "demo");

                float waited = 0f;
                while (!_duelPresenter.IsActive && waited < 3f)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
                if (!_duelPresenter.IsActive)
                {
                    Debug.LogWarning("[UnityRonin] demo mode: duel never started, retrying");
                    continue;
                }

                yield return SelfPlayInput.FightDuel(_duelPresenter, _swipeReader, alwaysLose: false,
                    onRanTooLong: beats => Debug.LogWarning($"[UnityRonin] demo mode: a duel ran {beats} beats without ending"));

                Debug.Log("[UnityRonin] demo mode: duel resolved");
            }
        }

        private EnemyEngageZone NextActiveEnemy()
        {
            foreach (var enemy in _enemies)
                if (enemy != null && enemy.gameObject.activeInHierarchy) return enemy;
            return null;
        }
    }
}
