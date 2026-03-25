using UnityEngine;
using TMPro;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.Core.Localization
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _tableName = "ui_common";
        [SerializeField] private string _key;

        private TextMeshProUGUI _text;
        private LocalizationManager _localization;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            _localization = ServiceLocator.Get<LocalizationManager>();

            if (_localization != null)
            {
                _localization.OnLanguageChanged += UpdateText;
                UpdateText();
            }
        }

        private void OnDestroy()
        {
            if (_localization != null)
                _localization.OnLanguageChanged -= UpdateText;
        }

        private void UpdateText()
        {
            if (_text != null && _localization != null)
                _text.text = _localization.GetLocalizedString(_tableName, _key);
        }

        public void SetKey(string key)
        {
            _key = key;
            UpdateText();
        }
    }
}
