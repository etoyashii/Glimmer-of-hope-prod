using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Default bubble implementation ,Drop this on a bubble prefab ,you can build other bubble designs as
    /// long as they implement IDialogueBubble.
    /// </summary>
    public class DialogueBubbleUI : MonoBehaviour, IDialogueBubble
    {
        #region Serialized Fields
        [Header("References")]
        [FormerlySerializedAs("textLabel")]
        [SerializeField] private TMP_Text _textLabel;
        [FormerlySerializedAs("canvasGroup")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Choices (optional)")]
        [Tooltip("Container the choice buttons get instantiated into.")]
        [FormerlySerializedAs("choicesContainer")]
        [SerializeField] private Transform _choicesContainer;
        [Tooltip("Button prefab used for each choice. Needs a Button + a TMP_Text.")]
        [FormerlySerializedAs("choiceButtonPrefab")]
        [SerializeField] private GameObject _choiceButtonPrefab;

        [Header("Plain Continuation")]
        [Tooltip("Button/area used to advance when there's no real choice (optional — if empty, the whole bubble is clickable via the CanvasGroup).")]
        [FormerlySerializedAs("continueButton")]
        [SerializeField] private Button _continueButton;
        #endregion

        #region Public Properties



        [Header("Speaker")]
        [SerializeField] private TMP_Text _speakerName; 

        
        public TMP_Text _SpeakerName
        {
            get => _speakerName;
            set => _speakerName = value;
        }
        public bool IsRevealingText { get; private set; }
        #endregion

        #region Private Fields
        private Action _onContinue;
        private Action<int> _onChoiceSelected;
        private readonly List<GameObject> _spawnedChoiceButtons = new List<GameObject>();

        private Coroutine _typewriterCoroutine;
        private string _fullText;
        #endregion

        #region Public Methods
        public void Initialize(Action onContinue, Action<int> onChoiceSelected)
        {
            _onContinue = onContinue;
            _onChoiceSelected = onChoiceSelected;

            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveAllListeners();
                _continueButton.onClick.AddListener(() => _onContinue?.Invoke());
            }
        }

        public void SetText(string text, bool typewriter, float charsPerSecond)
        {
            StopTypewriter();
            if (_textLabel == null) return;

            if (!typewriter || charsPerSecond <= 0f)
            {
                _textLabel.text = text;
                return;
            }

            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(text, charsPerSecond));
        }
        public void SetSpeakerName(string name)
        {
            if (_speakerName != null) _speakerName.text = name;
        }


        public void CompleteTextReveal()
        {
            StopTypewriter();
            if (_textLabel != null) _textLabel.text = _fullText;
        }

        public void SetChoices(IReadOnlyList<string> labels)
        {
            ClearChoiceButtons();

            bool hasRealChoices = labels != null && labels.Count > 0;
            if (_choicesContainer != null) _choicesContainer.gameObject.SetActive(hasRealChoices);
            if (_continueButton != null) _continueButton.gameObject.SetActive(!hasRealChoices);

            if (hasRealChoices) SpawnChoiceButtons(labels);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_canvasGroup == null) return;

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            StopTypewriter();
            ClearChoiceButtons();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Wire this to an OnClick on the bubble background for "click anywhere to continue".
        /// Only fires when there's no real choice shown and no dedicated continue button.
        /// </summary>
        public void OnBubbleClicked()
        {
            bool choicesVisible = _choicesContainer != null && _choicesContainer.gameObject.activeSelf;
            if (!choicesVisible && _continueButton == null)
                _onContinue?.Invoke();
        }
        #endregion

        #region Private Methods
        private void SpawnChoiceButtons(IReadOnlyList<string> labels)
        {
            if (_choiceButtonPrefab == null || _choicesContainer == null)
            {
                Debug.LogWarning("[DialogueBubbleUI] Missing choiceButtonPrefab or choicesContainer, can't show choices.");
                return;
            }

            for (int i = 0; i < labels.Count; i++)
            {
                int choiceIndex = i; // captured correctly for the closure
                var buttonObject = Instantiate(_choiceButtonPrefab, _choicesContainer);

                var buttonText = buttonObject.GetComponentInChildren<TMP_Text>();
                if (buttonText != null) buttonText.text = labels[i];

                var button = buttonObject.GetComponent<Button>();
                if (button != null)
                    button.onClick.AddListener(() => _onChoiceSelected?.Invoke(choiceIndex));

                _spawnedChoiceButtons.Add(buttonObject);
            }
        }

        private void ClearChoiceButtons()
        {
            foreach (var button in _spawnedChoiceButtons)
                if (button != null) Destroy(button);
            _spawnedChoiceButtons.Clear();
        }

        private void StopTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            IsRevealingText = false;
        }

        private IEnumerator TypewriterRoutine(string text, float charsPerSecond)
        {
            IsRevealingText = true;
            _fullText = text;
            _textLabel.text = "";

            float delay = 1f / Mathf.Max(charsPerSecond, 0.01f);
            var builder = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                builder.Append(text[i]);
                _textLabel.text = builder.ToString();
                yield return new WaitForSeconds(delay);
            }

            IsRevealingText = false;
            _typewriterCoroutine = null;
        }
        #endregion
    }
}
