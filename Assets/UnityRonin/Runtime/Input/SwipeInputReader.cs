using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityRonin.Core.Combat;

namespace UnityRonin.Runtime.Input
{
    /// <summary>
    /// Reads one continuous finger drag anywhere on screen and, on release, classifies the whole path into
    /// <see cref="GestureStroke"/>s via <see cref="GestureClassifier"/> — a straight line is one stroke, a
    /// zigzag is a chained combo. Active only while a duel wants gestures; the virtual joystick owns input
    /// the rest of the time.
    /// </summary>
    public class SwipeInputReader : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public System.Action<IReadOnlyList<GestureStroke>> GestureCompleted;

        private readonly List<GesturePoint> _path = new List<GesturePoint>();
        private bool _tracking;

        public void OnPointerDown(PointerEventData eventData)
        {
            _path.Clear();
            _path.Add(ToGesturePoint(eventData.position));
            _tracking = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_tracking) return;
            _path.Add(ToGesturePoint(eventData.position));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_tracking) return;
            _tracking = false;

            _path.Add(ToGesturePoint(eventData.position));
            var strokes = GestureClassifier.Classify(_path);
            if (strokes.Count > 0)
                GestureCompleted?.Invoke(strokes);
        }

        private static GesturePoint ToGesturePoint(Vector2 screenPosition) =>
            new GesturePoint(screenPosition.x, screenPosition.y);
    }
}
