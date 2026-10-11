using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityRonin.Core.Combat;

namespace UnityRonin.Runtime.Player
{
    /// <summary>Shows/hides the drawn katana, raises the stance pose when a duel starts or ends, and swings
    /// the blade through the shape of whatever the player just drew (independent of whether Core scores it
    /// a hit, parry or whiff — the swing shows the drawn gesture, Core alone decides what it was worth).</summary>
    public class SamuraiWeapon : MonoBehaviour
    {
        [SerializeField] private GameObject _sheathedKatana;
        [SerializeField] private GameObject _drawnKatana;
        [SerializeField] private Animator _animator;
        [SerializeField] private float _swingSeconds = 0.22f;
        [SerializeField] private float _thrustLungeDistance = 0.3f;
        private static readonly int DrawnHash = Animator.StringToHash("Drawn");

        private Coroutine _swinging;
        private Quaternion _restRotation;
        private Vector3 _restPosition;
        private bool _restPoseCaptured;

        public bool IsDrawn { get; private set; }

        public void Draw()
        {
            if (IsDrawn) return;
            IsDrawn = true;
            if (_sheathedKatana != null) _sheathedKatana.SetActive(false);
            if (_drawnKatana != null) _drawnKatana.SetActive(true);
            if (_animator != null) _animator.SetBool(DrawnHash, true);
        }

        public void Sheathe()
        {
            if (!IsDrawn) return;
            IsDrawn = false;
            if (_sheathedKatana != null) _sheathedKatana.SetActive(true);
            if (_drawnKatana != null) _drawnKatana.SetActive(false);
            if (_animator != null) _animator.SetBool(DrawnHash, false);
            if (_swinging != null) { StopCoroutine(_swinging); _swinging = null; }
        }

        /// <summary>Swings the blade through each stroke of a drawn gesture in sequence — a chained combo
        /// plays as a chained combo of swings.</summary>
        public void Swing(IReadOnlyList<GestureStroke> strokes)
        {
            if (_drawnKatana == null || strokes.Count == 0) return;
            CaptureRestPose();
            if (_swinging != null) StopCoroutine(_swinging);
            _swinging = StartCoroutine(SwingSequence(strokes));
        }

        private void CaptureRestPose()
        {
            if (_restPoseCaptured) return;
            var t = _drawnKatana.transform;
            _restRotation = t.localRotation;
            _restPosition = t.localPosition;
            _restPoseCaptured = true;
        }

        private IEnumerator SwingSequence(IReadOnlyList<GestureStroke> strokes)
        {
            var t = _drawnKatana.transform;
            foreach (var stroke in strokes)
            {
                var strikeRotation = _restRotation * DeltaRotationFor(stroke.Symbol);
                var strikePosition = _restPosition + _restRotation * LungeFor(stroke.Symbol);

                yield return Animate(t, t.localRotation, t.localPosition, strikeRotation, strikePosition, _swingSeconds * 0.4f);
                yield return Animate(t, strikeRotation, strikePosition, _restRotation, _restPosition, _swingSeconds * 0.6f);
            }
            _swinging = null;
        }

        private static IEnumerator Animate(Transform t, Quaternion fromRotation, Vector3 fromPosition,
            Quaternion toRotation, Vector3 toPosition, float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var u = Mathf.Clamp01(elapsed / seconds);
                t.localRotation = Quaternion.Slerp(fromRotation, toRotation, u);
                t.localPosition = Vector3.Lerp(fromPosition, toPosition, u);
                yield return null;
            }
            t.localRotation = toRotation;
            t.localPosition = toPosition;
        }

        // Rotations layered on top of the drawn rest pose — a sideways arc for the flat cuts, an overhead
        // chop for the vertical, and the two diagonals, each sweeping the tip through the same shape the
        // player drew. Thrust has no cutting arc; it lunges instead (see LungeFor).
        private static Quaternion DeltaRotationFor(GestureSymbol symbol) => symbol switch
        {
            GestureSymbol.Horizontal => Quaternion.Euler(0f, 0f, 70f),
            GestureSymbol.Vertical => Quaternion.Euler(70f, 0f, 0f),
            GestureSymbol.Rising => Quaternion.Euler(50f, 0f, 50f),
            GestureSymbol.Falling => Quaternion.Euler(-50f, 0f, 50f),
            _ => Quaternion.identity
        };

        // A Thrust's lunge is along the blade's own grip-to-tip axis (local +Y, see BuildKatana), expressed
        // here in the katana's own local space before CaptureRestPose's rotation carries it into the
        // player's local space.
        private Vector3 LungeFor(GestureSymbol symbol) =>
            symbol == GestureSymbol.Thrust ? new Vector3(0f, _thrustLungeDistance, 0f) : Vector3.zero;
    }
}
