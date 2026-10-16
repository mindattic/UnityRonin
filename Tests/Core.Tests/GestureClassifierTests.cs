using UnityRonin.Core.Combat;
using NUnit.Framework;

namespace UnityRonin.Core.Tests
{
    public class GestureClassifierTests
    {
        private static GesturePoint P(float x, float y) => new GesturePoint(x, y);

        [Test]
        public void A_pure_tap_with_no_movement_is_a_thrust()
        {
            var strokes = GestureClassifier.Classify(new[] { P(10f, 10f), P(10f, 10f) });
            Assert.That(strokes, Has.Exactly(1).Items);
            Assert.That(strokes[0].Symbol, Is.EqualTo(GestureSymbol.Thrust));
        }

        [Test]
        public void A_drag_shorter_than_the_minimum_distance_is_also_a_thrust_not_an_aborted_swipe()
        {
            var strokes = GestureClassifier.Classify(new[] { P(0f, 0f), P(5f, 3f) });
            Assert.That(strokes, Has.Exactly(1).Items);
            Assert.That(strokes[0].Symbol, Is.EqualTo(GestureSymbol.Thrust));
        }

        [Test]
        public void A_long_straight_horizontal_drag_classifies_as_horizontal()
        {
            var strokes = GestureClassifier.Classify(new[] { P(0f, 0f), P(100f, 0f) });
            Assert.That(strokes, Has.Exactly(1).Items);
            Assert.That(strokes[0].Symbol, Is.EqualTo(GestureSymbol.Horizontal));
        }

        [Test]
        public void A_long_straight_vertical_drag_classifies_as_vertical()
        {
            var strokes = GestureClassifier.Classify(new[] { P(0f, 0f), P(0f, 100f) });
            Assert.That(strokes, Has.Exactly(1).Items);
            Assert.That(strokes[0].Symbol, Is.EqualTo(GestureSymbol.Vertical));
        }

        [Test]
        public void A_forty_five_degree_drag_one_way_classifies_as_rising()
        {
            var strokes = GestureClassifier.Classify(new[] { P(0f, 0f), P(100f, 100f) });
            Assert.That(strokes, Has.Exactly(1).Items);
            Assert.That(strokes[0].Symbol, Is.EqualTo(GestureSymbol.Rising));
        }

        [Test]
        public void A_forty_five_degree_drag_the_other_way_classifies_as_falling()
        {
            var strokes = GestureClassifier.Classify(new[] { P(0f, 0f), P(100f, -100f) });
            Assert.That(strokes, Has.Exactly(1).Items);
            Assert.That(strokes[0].Symbol, Is.EqualTo(GestureSymbol.Falling));
        }

        [Test]
        public void A_line_does_not_depend_on_which_end_it_was_drawn_from()
        {
            var forward = GestureClassifier.Classify(new[] { P(0f, 0f), P(100f, 0f) });
            var backward = GestureClassifier.Classify(new[] { P(100f, 0f), P(0f, 0f) });
            Assert.That(backward[0].Symbol, Is.EqualTo(forward[0].Symbol));
        }

        [Test]
        public void A_sharp_corner_splits_into_two_chained_strokes()
        {
            // A short horizontal leg (just past the minimum stroke length), then a sharp turn into a long
            // near-vertical leg — the corner lands right at the point where the angle from the gesture's
            // start swings past the turn threshold.
            var strokes = GestureClassifier.Classify(new[] { P(0f, 0f), P(30f, 0f), P(31f, 100f) });

            Assert.That(strokes.Count, Is.EqualTo(2));
            Assert.That(strokes[0].Symbol, Is.EqualTo(GestureSymbol.Horizontal));
            Assert.That(strokes[1].Symbol, Is.EqualTo(GestureSymbol.Vertical));
        }

        [Test]
        public void A_gentle_curve_with_no_sharp_corner_stays_one_stroke()
        {
            // A slight wobble along an otherwise straight horizontal line — not a deliberate direction change.
            var strokes = GestureClassifier.Classify(new[]
            {
                P(0f, 0f), P(25f, 2f), P(50f, 0f), P(75f, 2f), P(100f, 0f)
            });

            Assert.That(strokes, Has.Exactly(1).Items);
            Assert.That(strokes[0].Symbol, Is.EqualTo(GestureSymbol.Horizontal));
        }
    }
}
