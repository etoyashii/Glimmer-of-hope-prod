using UnityEngine;

namespace GlimmerOfHope.Editor
{
    public class SliderAttribute : PropertyAttribute
    {
        public readonly float Min;
        public readonly float Max;

        public SliderAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public SliderAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
}
