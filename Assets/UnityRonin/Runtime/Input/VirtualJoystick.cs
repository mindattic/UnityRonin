using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityRonin.Core.Movement;

namespace UnityRonin.Runtime.Input
{
    /// <summary>
    /// A transparent, one-thumb analog stick covering the bottom half of the screen. Touching anywhere in
    /// that zone drops the visible ring under the thumb (clamped on-screen) and drags a knob inside it;
    /// releasing hides it and zeroes the reading. This is the only control surface while exploring.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _knob;
        [SerializeField] private CanvasGroup _visual;
        [SerializeField] private float _radiusPixels = 160f;
        [SerializeField] private float _deadZone = 0.12f;

        public StickVector Current { get; private set; } = StickVector.Zero;
        public bool IsHeld { get; private set; }

        private Vector2 _origin;
        private int _activePointerId = -1;

        private void Awake()
        {
            SetVisible(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHeld) return;
            IsHeld = true;
            _activePointerId = eventData.pointerId;
            _origin = eventData.position;
            _background.position = _origin;
            _knob.anchoredPosition = Vector2.zero;
            SetVisible(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsHeld || eventData.pointerId != _activePointerId) return;

            Vector2 delta = eventData.position - _origin;
            var stick = VirtualStick.FromDrag(delta.x, delta.y, _radiusPixels, _deadZone);
            Current = stick;

            Vector2 knobOffset = new Vector2(stick.X, stick.Y) * _radiusPixels;
            _knob.anchoredPosition = knobOffset;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            Release();
        }

        private void Release()
        {
            IsHeld = false;
            _activePointerId = -1;
            Current = StickVector.Zero;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_visual != null) _visual.alpha = visible ? 0.55f : 0f;
        }

        /// <summary>Forces the stick to release and zero, e.g. when a duel takes over input.</summary>
        public void ForceRelease() => Release();
    }
}
