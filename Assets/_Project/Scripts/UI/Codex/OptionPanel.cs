using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using GlimmerOfHope.UI.BookMenu;

namespace GlimmerOfHope.UI.BookMenu.Panels
{
    public class OptionsPanel : MonoBehaviour, IBookPage
    {
        #region Private Fields

        [Header("Audio (Left Page)")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _voicesSlider;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _voicesToggle;

        [Header("Language / Video (Right Page)")]
        [SerializeField] private CyclerControl _languageCycler;
        [SerializeField] private CyclerControl _videoCycler;
        [SerializeField] private string[] _languages = { "Français", "English", "Español", "Deutsch" };
        [SerializeField] private string[] _videoQualities = { "Faible", "Moyen", "Élevé", "Deuteranomalie" };

        [Header("Return")]
        [SerializeField] private Button _returnButton;
        [SerializeField] private BookMenuController _bookMenuController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _musicSlider.onValueChanged.AddListener(v => SetVolume("MusicVolume", v));
            _sfxSlider.onValueChanged.AddListener(v => SetVolume("SFXVolume", v));
            _voicesSlider.onValueChanged.AddListener(v => SetVolume("VoicesVolume", v));

            _musicToggle.onValueChanged.AddListener(v => _musicSlider.interactable = v);
            _sfxToggle.onValueChanged.AddListener(v => _sfxSlider.interactable = v);
            _voicesToggle.onValueChanged.AddListener(v => _voicesSlider.interactable = v);

            _languageCycler.Setup(_languages, 0, (value, index) =>
            {
            });

            _videoCycler.Setup(_videoQualities, _videoQualities.Length - 1, (value, index) =>
            {
            });

            if (_returnButton != null && _bookMenuController != null)
                _returnButton.onClick.AddListener(_bookMenuController.CloseBook);
        }

        #endregion

        #region Public Methods

        public void OnPageShown()
        {
        }

        #endregion

        #region Private Methods

        private void SetVolume(string exposedParam, float linear01)
        {
            float db = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
            if (_audioMixer != null) _audioMixer.SetFloat(exposedParam, db);
        }

        #endregion
    }
}