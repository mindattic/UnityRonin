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
    ///
    /// <see cref="_radiusPixels"/> is in canvas units (the HUD's 1080x1920 reference resolution), not raw
    /// screen pixels, so the stick's travel is the same share of the screen on every device; pointer
    /// positions are screen pixels and are scaled by the canvas's scale factor before use. The ring is
    /// clamped so it stays wholly on screen when the thumb lands near an edge; the reading is still taken
    /// from where the thumb landed, so the knob sits under the thumb (U.RON-T21).
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
        private Vector2 _ringOffsetCanvas;
        private int _activePointerId = -1;
        private Canvas _canvas;

        /// <summary>The stick's full travel in screen pixels on this device.</summary>
        public float RadiusScreenPixels => _radiusPixels * ScaleFactor;

        private float ScaleFactor
        {
            get
            {
                if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
                float scale = _canvas != null ? _canvas.rootCanvas.scaleFactor : 1f;
                return scale > 0.0001f ? scale : 1f;
            }
        }

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
            Vector2 ring = ClampOnScreen(_origin);
            _background.position = ring;
            _ringOffsetCanvas = (_origin - ring) / ScaleFactor;
            _knob.anchoredPosition = _ringOffsetCanvas;
            SetVisible(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsHeld || eventData.pointerId != _activePointerId) return;

            Vector2 delta = (eventData.position - _origin) / ScaleFactor;
            var stick = VirtualStick.FromDrag(delta.x, delta.y, _radiusPixels, _deadZone);
            Current = stick;

            Vector2 knobOffset = new Vector2(stick.X, stick.Y) * _radiusPixels;
            _knob.anchoredPosition = _ringOffsetCanvas + knobOffset;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            Release();
        }

        /// <summary>The ring's centre for a thumb at <paramref name="screen"/>, moved in just far enough that
        /// the whole ring is on screen (or centred, on a screen smaller than the ring).</summary>
        private Vector2 ClampOnScreen(Vector2 screen)
        {
            float scale = ScaleFactor;
            float halfW = _background.rect.width * 0.5f * scale;
            float halfH = _background.rect.height * 0.5f * scale;
            return new Vector2(ClampAxis(screen.x, halfW, Screen.width), ClampAxis(screen.y, halfH, Screen.height));
        }

        private static float ClampAxis(float value, float half, float size)
        {
            if (size <= 2f * half) return size * 0.5f;
            return Mathf.Clamp(value, half, size - half);
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
