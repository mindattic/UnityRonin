using UnityEngine;
using UnityEngine.EventSystems;
using UnityRonin.Core.Combat;

namespace UnityRonin.Runtime.Input
{
    /// <summary>
    /// Reads one finger drag anywhere on screen and classifies it as a cardinal swipe on release.
    /// Active only while a duel wants swipes; the virtual joystick owns input the rest of the time.
    /// </summary>
    public class SwipeInputReader : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private float _minSwipeDistancePixels = 60f;

        public System.Action<SwipeDirection> SwipeCompleted;

        private Vector2 _start;
        private bool _tracking;

        public void OnPointerDown(PointerEventData eventData)
        {
            _start = eventData.position;
            _tracking = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Intentionally no-op: classification happens once on release so a single clean swipe counts,
            // not every intermediate frame of the drag.
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_tracking) return;
            _tracking = false;

            Vector2 delta = eventData.position - _start;
            var direction = SwipeClassifier.Classify(delta.x, delta.y, _minSwipeDistancePixels);
            if (direction != SwipeDirection.None)
                SwipeCompleted?.Invoke(direction);
        }
    }
}
