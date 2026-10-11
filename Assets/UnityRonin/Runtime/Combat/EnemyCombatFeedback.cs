using System.Collections;
using UnityEngine;

namespace UnityRonin.Runtime.Combat
{
    /// <summary>
    /// Minimal, primitives-only visual feedback for the enemy's side of a riposte exchange: a quick
    /// backward hop when it retreats from the player's attack, a brief color flash when its counter lands
    /// instead. No animation rig — matching the project's current pre-art phase (docs/BIBLE.md §3/§6).
    /// </summary>
    public class EnemyCombatFeedback : MonoBehaviour
    {
        [SerializeField] private float _hopDistance = 0.6f;
        [SerializeField] private float _hopSeconds = 0.25f;
        [SerializeField] private float _flashSeconds = 0.15f;
        [SerializeField] private Color _flashColor = Color.white;

        private Renderer _renderer;
        private Color _baseColor;
        private Coroutine _running;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            // .material (not .sharedMaterial) makes a per-instance copy on first access: both enemies are
            // built from the same saved matte-material asset (same base color), so mutating .color on the
            // shared asset would flash every enemy using it, not just this one.
            if (_renderer != null) _baseColor = _renderer.material.color;
        }

        public void HopBack(Vector3 awayFromPosition)
        {
            if (_running != null) StopCoroutine(_running);
            Vector3 away = transform.position - awayFromPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            _running = StartCoroutine(Hop(away.normalized * _hopDistance));
        }

        public void FlashCounter()
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Flash());
        }

        private IEnumerator Hop(Vector3 offset)
        {
            Vector3 start = transform.position;
            Vector3 end = start + offset;
            float elapsed = 0f;
            while (elapsed < _hopSeconds)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / _hopSeconds));
                yield return null;
            }
            transform.position = end;
            _running = null;
        }

        private IEnumerator Flash()
        {
            if (_renderer != null) _renderer.material.color = _flashColor;
            yield return new WaitForSeconds(_flashSeconds);
            if (_renderer != null) _renderer.material.color = _baseColor;
            _running = null;
        }
    }
}
