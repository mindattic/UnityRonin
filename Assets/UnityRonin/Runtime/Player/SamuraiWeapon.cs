using UnityEngine;

namespace UnityRonin.Runtime.Player
{
    /// <summary>Shows/hides the drawn katana and raises the stance pose when a duel starts or ends.</summary>
    public class SamuraiWeapon : MonoBehaviour
    {
        [SerializeField] private GameObject _sheathedKatana;
        [SerializeField] private GameObject _drawnKatana;
        [SerializeField] private Animator _animator;
        private static readonly int DrawnHash = Animator.StringToHash("Drawn");

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
        }
    }
}
