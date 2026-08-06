using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlimmerOfHope.Core.Localization;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.UI.Localization
{
    public class LanguageSwitcher : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Mode bouton")]
        [SerializeField] private Button _cycleButton;
        [SerializeField] private TMP_Text _buttonLabel;

        [Header("Mode dropdown")]
        [SerializeField] private TMP_Dropdown _dropdown;

        #endregion

        #region Private Fields

        private LocalizationManager _localization;
        private readonly Dictionary<string, string> _displayNames = new()
        {
            { "fr", "Francais" },
            { "en", "English" },
            { "es", "Espanol" }
        };

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _localization))
            {
                Debug.LogWarning("[LanguageSwitcher] LocalizationManager introuvable.");
                return;
            }

            SetupDropdown();
            SetupButton();

            _localization.OnLanguageChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_localization != null)
                _localization.OnLanguageChanged -= Refresh;

            if (_cycleButton != null)
                _cycleButton.onClick.RemoveListener(OnCycleClicked);

            if (_dropdown != null)
                _dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        }

        #endregion

        #region Public Methods

        public void SetLanguage(string languageCode)
        {
            _localization?.SetLanguage(languageCode);
        }

        #endregion

        #region Private Methods

        private void SetupButton()
        {
            if (_cycleButton == null)
                return;

            _cycleButton.onClick.AddListener(OnCycleClicked);
        }

        private void SetupDropdown()
        {
            if (_dropdown == null)
                return;

            var options = new List<string>();

            foreach (var code in _localization.AvailableLanguages)
                options.Add(GetDisplayName(code));

            _dropdown.ClearOptions();
            _dropdown.AddOptions(options);
            _dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        private void OnCycleClicked()
        {
            _localization?.CycleLanguage();
        }

        private void OnDropdownChanged(int index)
        {
            if (_localization == null)
                return;

            if (index < 0 || index >= _localization.AvailableLanguages.Count)
                return;

            _localization.SetLanguage(_localization.AvailableLanguages[index]);
        }

        private void Refresh()
        {
            if (_localization == null)
                return;

            var current = _localization.CurrentLanguage;

            if (_buttonLabel != null)
                _buttonLabel.text = GetDisplayName(current);

            if (_dropdown != null)
            {
                var index = IndexOfLanguage(current);
                if (index >= 0)
                    _dropdown.SetValueWithoutNotify(index);
            }
        }

        private int IndexOfLanguage(string code)
        {
            var languages = _localization.AvailableLanguages;

            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i] == code)
                    return i;
            }

            return -1;
        }

        private string GetDisplayName(string code)
        {
            return _displayNames.TryGetValue(code, out var name) ? name : code;
        }

        #endregion
    }
}
