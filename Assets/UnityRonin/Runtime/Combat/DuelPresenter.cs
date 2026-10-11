using System;
using System.Collections;
using UnityEngine;
using UnityRonin.Core.Combat;
using UnityRonin.Core.Util;
using UnityRonin.Runtime.Input;
using UnityRonin.Runtime.Player;

namespace UnityRonin.Runtime.Combat
{
    /// <summary>
    /// Runs one katana duel: owns the Core <see cref="DuelDirector"/>, times each beat's telegraph and
    /// response window, reads the player's swipe, and reports the final result. The Samurai's movement
    /// is frozen to auto-positioning for the duration; the virtual joystick is released.
    /// </summary>
    public class DuelPresenter : MonoBehaviour
    {
        [SerializeField] private SwipeInputReader _swipeReader;
        [SerializeField] private DuelTelegraph _telegraph;
        [SerializeField] private SamuraiWeapon _weapon;

        public event Action<DuelOutcome> BeatResolved;
        public event Action<bool> DuelEnded; // true = player won

        private DuelDirector _director;
        private Coroutine _running;
        private SwipeDirection _pendingSwipe = SwipeDirection.None;

        public bool IsActive { get; private set; }

        public void Begin(uint seed, int playerHealth = 3, int enemyHealth = 3)
        {
            _director = new DuelDirector(new Rng(seed), playerHealth, enemyHealth);
            IsActive = true;
            _weapon?.Draw();
            _swipeReader.SwipeCompleted += OnSwipe;
            _running = StartCoroutine(RunBeats());
        }

        private void OnSwipe(SwipeDirection direction) => _pendingSwipe = direction;

        private IEnumerator RunBeats()
        {
            while (!_director.IsOver)
            {
                var beat = _director.CurrentBeat;
                _telegraph.Show(beat);
                _pendingSwipe = SwipeDirection.None;

                yield return new WaitForSeconds(beat.TelegraphSeconds);

                float elapsed = 0f;
                while (elapsed < beat.ResponseWindowSeconds && _pendingSwipe == SwipeDirection.None)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                var outcome = _director.Resolve(_pendingSwipe);
                _telegraph.Hide();
                BeatResolved?.Invoke(outcome);
            }

            End();
        }

        private void End()
        {
            IsActive = false;
            _swipeReader.SwipeCompleted -= OnSwipe;
            _weapon?.Sheathe();
            DuelEnded?.Invoke(_director.PlayerWon);
        }

        public void Abort()
        {
            if (_running != null) StopCoroutine(_running);
            if (IsActive) End();
        }
    }
}
