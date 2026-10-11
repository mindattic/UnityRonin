using UnityRonin.Core.Combat;
using Xunit;

namespace UnityRonin.Core.Tests
{
    public class SwipeClassifierTests
    {
        [Theory]
        [InlineData(200f, 10f, SwipeDirection.Right)]
        [InlineData(-200f, -10f, SwipeDirection.Left)]
        [InlineData(5f, 200f, SwipeDirection.Up)]
        [InlineData(-5f, -200f, SwipeDirection.Down)]
        public void Classifies_the_dominant_axis_of_a_clear_swipe(float dx, float dy, SwipeDirection expected)
        {
            Assert.Equal(expected, SwipeClassifier.Classify(dx, dy));
        }

        [Fact]
        public void A_drag_shorter_than_the_minimum_distance_is_not_a_swipe()
        {
            Assert.Equal(SwipeDirection.None, SwipeClassifier.Classify(10f, 5f, minDistancePixels: 60f));
        }

        [Fact]
        public void A_diagonal_drag_resolves_to_whichever_axis_travelled_further()
        {
            Assert.Equal(SwipeDirection.Right, SwipeClassifier.Classify(100f, 90f));
            Assert.Equal(SwipeDirection.Up, SwipeClassifier.Classify(90f, 100f));
        }
    }
}
