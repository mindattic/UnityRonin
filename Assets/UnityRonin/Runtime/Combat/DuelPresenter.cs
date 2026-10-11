using System;
using System.Collections;
using System.Collections.Generic;
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
    ///
    /// Alternates between Core's two phases: a normal enemy-telegraphed beat, and — after a parry — a
    /// slowed-time riposte window where the player freely picks their own attack. <see cref="Time.timeScale"/>
    /// is the only piece of the slow-motion feel Core doesn't know about; everything else (the window's
    /// length, whether the enemy counters or retreats, whether the chain continues) is <see cref="DuelDirector"/>'s.
    /// </summary>
    public class DuelPresenter : MonoBehaviour
    {
        [SerializeField] private SwipeInputReader _swipeReader;
        [SerializeField] private DuelTelegraph _telegraph;
        [SerializeField] private SamuraiWeapon _weapon;
        [SerializeField] private float _riposteTimeScale = 0.3f;

        public event Action<DuelOutcome> BeatResolved;
        public event Action<bool> DuelEnded; // true = player won

        private DuelDirector _director;
        private Coroutine _running;
        private IReadOnlyList<GestureStroke> _pendingStrokes = Array.Empty<GestureStroke>();

        public bool IsActive { get; private set; }

        /// <summary>The beat currently being telegraphed, for autoplay/verification; null when inactive or
        /// mid-riposte (there is no telegraph to peek at — the player is choosing freely).</summary>
        public DuelBeat? PeekCurrentBeat =>
            IsActive && _director.Phase == DuelPhase.EnemyBeat ? _director.CurrentBeat : (DuelBeat?)null;

        /// <summary>Which side's turn it is, for autoplay/verification; null when inactive.</summary>
        public DuelPhase? PeekCurrentPhase => IsActive ? _director.Phase : (DuelPhase?)null;

        public void Begin(uint seed, int playerHealth = 3, int enemyHealth = 3)
        {
            _director = new DuelDirector(new Rng(seed), playerHealth, enemyHealth);
            IsActive = true;
            _weapon?.Draw();
            _swipeReader.GestureCompleted += OnGesture;
            _running = StartCoroutine(RunBeats());
        }

        private void OnGesture(IReadOnlyList<GestureStroke> strokes)
        {
            _pendingStrokes = strokes;
            _weapon?.Swing(strokes); // shows the drawn gesture immediately; Core alone decides what it was worth
        }

        private IEnumerator RunBeats()
        {
            while (!_director.IsOver)
            {
                yield return _director.Phase == DuelPhase.PlayerRiposte ? RunRiposteWindow() : RunEnemyBeat();
            }

            Time.timeScale = 1f; // safety net: a duel can only end mid-riposte if the last hit was lethal
            End();
        }

        private IEnumerator RunEnemyBeat()
        {
            var beat = _director.CurrentBeat;
            _telegraph.Show(beat);
            _pendingStrokes = Array.Empty<GestureStroke>();

            yield return new WaitForSeconds(beat.TelegraphSeconds);

            float elapsed = 0f;
            while (elapsed < beat.ResponseWindowSeconds && _pendingStrokes.Count == 0)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            var outcome = _director.Resolve(_pendingStrokes);
            _telegraph.Hide();
            BeatResolved?.Invoke(outcome);
        }

        private IEnumerator RunRiposteWindow()
        {
            Time.timeScale = _riposteTimeScale;
            _telegraph.ShowRiposte();
            _pendingStrokes = Array.Empty<GestureStroke>();

            // RiposteWindowSeconds is in slowed game-time (Time.deltaTime already reflects timeScale), so the
            // player's real, wall-clock window is longer than the number itself suggests — that's the point.
            float elapsed = 0f;
            while (elapsed < _director.RiposteWindowSeconds && _pendingStrokes.Count == 0)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            var outcome = _director.ResolveRiposte(_pendingStrokes);
            _telegraph.Hide();
            BeatResolved?.Invoke(outcome);

            // Only restore full speed once the chain has actually ended; a landed hit loops straight back into
            // another riposte window at the same slow-mo, no jarring speed-up mid-combo.
            if (_director.Phase != DuelPhase.PlayerRiposte) Time.timeScale = 1f;
        }

        private void End()
        {
            IsActive = false;
            _swipeReader.GestureCompleted -= OnGesture;
            _weapon?.Sheathe();
            DuelEnded?.Invoke(_director.PlayerWon);
        }

        public void Abort()
        {
            Time.timeScale = 1f;
            if (_running != null) StopCoroutine(_running);
            if (IsActive) End();
        }
    }
}
