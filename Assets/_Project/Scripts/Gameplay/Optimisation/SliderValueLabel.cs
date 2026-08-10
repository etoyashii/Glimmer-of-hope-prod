using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Composant léger destiné à RESTER dans la scène après suppression du générateur
    /// d'UI. Met à jour le texte affiché à côté d'un slider ("Point Count: 4500"...).
    /// Ciblé par un listener persistant sur le Slider.onValueChanged.
    /// </summary>
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