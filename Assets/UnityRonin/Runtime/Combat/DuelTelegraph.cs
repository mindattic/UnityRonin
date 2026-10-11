using UnityEngine;
using UnityEngine.UI;
using UnityRonin.Core.Combat;

namespace UnityRonin.Runtime.Combat
{
    /// <summary>
    /// A screen-space arrow icon that shows which of the four cardinal directions the enemy is about to
    /// use, and whether it is an attack (parry it) or a guard opening (strike it) by its color. Lives on
    /// the HUD canvas, not in world space, so it reads the same regardless of camera angle.
    /// </summary>
    public class DuelTelegraph : MonoBehaviour
    {
        [SerializeField] private RectTransform _arrow;
        [SerializeField] private Image _arrowImage;
        [SerializeField] private Color _attackColor = new Color(0.85f, 0.15f, 0.1f);
        [SerializeField] private Color _guardColor = new Color(0.15f, 0.55f, 0.85f);

        public void Show(DuelBeat beat)
        {
            gameObject.SetActive(true);
            if (_arrow != null) _arrow.localRotation = RotationFor(beat.Direction);
            if (_arrowImage != null)
                _arrowImage.color = beat.EnemyAction == DuelAction.Attack ? _attackColor : _guardColor;
        }

        public void Hide() => gameObject.SetActive(false);

        private static Quaternion RotationFor(SwipeDirection direction) => direction switch
        {
            SwipeDirection.Up => Quaternion.Euler(0f, 0f, 0f),
            SwipeDirection.Down => Quaternion.Euler(0f, 0f, 180f),
            SwipeDirection.Left => Quaternion.Euler(0f, 0f, 90f),
            SwipeDirection.Right => Quaternion.Euler(0f, 0f, -90f),
            _ => Quaternion.identity
        };
    }
}
