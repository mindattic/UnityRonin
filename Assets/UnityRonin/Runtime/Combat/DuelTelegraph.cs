using System.Collections;
using TMPro;
using UnityEngine;
using UnityRonin.Core.Combat;

namespace UnityRonin.Runtime.Combat
{
    /// <summary>
    /// A screen-space calligraphy brush stroke, drawn top-center, in the shape of the
    /// <see cref="GestureSymbol"/> the player must answer: real brush-stroke glyphs (one, vertical-line,
    /// slash, backslash and dot radicals) set in the free, OFL-licensed Yuji Syuku font — see CREDITS.md —
    /// rather than a drawn rectangle, so the shape itself carries its own orientation and no extra rotation
    /// is needed. It scales in over <see cref="_drawSeconds"/> like a brush touching down, then holds until
    /// the beat resolves. Colored by whether it's an attack (parry it) or a guard opening (strike it). Lives
    /// on the HUD canvas, not in world space, so it reads the same regardless of camera angle.
    /// </summary>
    public class DuelTelegraph : MonoBehaviour
    {
        [SerializeField] private RectTransform _stroke;
        [SerializeField] private TMP_Text _strokeText;
        [SerializeField] private Color _attackColor = new Color(0.85f, 0.15f, 0.1f);
        [SerializeField] private Color _guardColor = new Color(0.15f, 0.55f, 0.85f);
        [SerializeField] private Color _riposteColor = new Color(0.85f, 0.7f, 0.15f);
        [SerializeField] private float _drawSeconds = 0.18f;

        // The dot radical ("丶") is drawn far smaller than a full line stroke even at the same point size —
        // true to real calligraphy, but at a glance it would read as barely-there next to the other four
        // glyphs. This brings Thrust back up to a comparable visual weight for gameplay legibility.
        [SerializeField] private float _thrustSizeMultiplier = 3f;

        private const string RiposteGlyph = "刀"; // the katana itself — no technique is telegraphed; the
                                                    // player is choosing their own free attack.

        private Coroutine _drawing;
        private float _baseFontSize;
        private bool _baseFontSizeCaptured;

        public void Show(DuelBeat beat) =>
            BeginDraw(GlyphFor(beat.Symbol), beat.EnemyAction == DuelAction.Attack ? _attackColor : _guardColor,
                beat.Symbol == GestureSymbol.Thrust ? _thrustSizeMultiplier : 1f);

        /// <summary>A parry just earned a free riposte: no telegraph to show, since the player picks their
        /// own attack technique — just a neutral cue that it's their turn.</summary>
        public void ShowRiposte() => BeginDraw(RiposteGlyph, _riposteColor);

        private void BeginDraw(string glyph, Color color, float fontSizeMultiplier = 1f)
        {
            gameObject.SetActive(true);
            if (_strokeText != null)
            {
                if (!_baseFontSizeCaptured)
                {
                    _baseFontSize = _strokeText.fontSize;
                    _baseFontSizeCaptured = true;
                }
                _strokeText.text = glyph;
                _strokeText.color = color;
                _strokeText.fontSize = _baseFontSize * fontSizeMultiplier;
            }

            if (_drawing != null) StopCoroutine(_drawing);
            _drawing = StartCoroutine(DrawOn());
        }

        public void Hide()
        {
            if (_drawing != null) StopCoroutine(_drawing);
            gameObject.SetActive(false);
        }

        // Scales up from the center, like a brush stamping the glyph down, rather than sweeping out from
        // one edge — the glyph's own shape (not a rotated generic bar) is what carries the stroke direction
        // now, so a uniform "touch down" reads better than an asymmetric sweep.
        private IEnumerator DrawOn()
        {
            if (_stroke == null) yield break;

            _stroke.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < _drawSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _drawSeconds);
                _stroke.localScale = new Vector3(t, t, 1f);
                yield return null;
            }
            _stroke.localScale = Vector3.one;
        }

        private static string GlyphFor(GestureSymbol symbol) => symbol switch
        {
            GestureSymbol.Horizontal => "一", // 一 — the horizontal-stroke ideograph
            GestureSymbol.Vertical => "丨",   // 丨 — the vertical-line radical
            GestureSymbol.Rising => "／",     // ／ — fullwidth slash
            GestureSymbol.Falling => "＼",    // ＼ — fullwidth backslash
            GestureSymbol.Thrust => "丶",     // 丶 — the dot radical
            _ => string.Empty
        };
    }
}
