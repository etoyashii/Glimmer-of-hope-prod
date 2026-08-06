using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.Gameplay
{

    public class SliderValueLabel : MonoBehaviour
    {
        public Text label;
        public string prefix = "Valeur";
        public string format = "F2";

        public void UpdateLabel(float value)
        {
            if (label != null) { label.text = $"{prefix}: {value.ToString(format)}"; }
        }
    }
}