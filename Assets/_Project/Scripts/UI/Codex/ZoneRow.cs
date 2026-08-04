using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GlimmerOfHope.UI.BookMenu.Panels
{
    public class ZoneRow : MonoBehaviour
    {
        #region Private Fields

        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _percentText;
        [Tooltip("Image component set to Filled (Horizontal)")]
        [SerializeField] private Image _fillImage;

        #endregion

        #region Public Methods

        public void Setup(string zoneName, int percent)
        {
            if (_nameText != null) _nameText.text = zoneName;
            if (_percentText != null) _percentText.text = percent + "%";
            if (_fillImage != null) _fillImage.fillAmount = percent / 100f;
        }

        #endregion
    }
}