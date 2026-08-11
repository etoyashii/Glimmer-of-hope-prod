using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Fixed-on-screen prefab that handles both the "continue" button and choices. One instance
    /// is kept alive for the whole dialogue by DialogueManager. Used when the bubble
    /// follows the speaker and is never clickable itself.
    /// </summary>
    public class DialogueInteractionUI : MonoBehaviour, IDialogueInteractionUI
    {
        #region Serialized Fields

        [Header("Plain Continuation")]
        [Tooltip("Shown when this line has no real choice, just needs to advance.")]
        [FormerlySerializedAs("continueButton")]
        [SerializeField] private Button _continueButton;

        [Header("Choices")]
        [Tooltip("Container the choice buttons get instantiated into. Needs a Vertical/Horizontal Layout Group.")]
        [FormerlySerializedAs("choicesContainer")]
        [SerializeField] private Transform _choicesContainer;
        [Tooltip("Button prefab used for each choice. Needs a Button + a TMP_Text.")]
        [FormerlySerializedAs("choiceButtonPrefab")]
        [SerializeField] private GameObject _choiceButtonPrefab;

        #endregion

        #region Private Fields

        private Action _onContinue;
        private Action<int> _onChoiceSelected;
        private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

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

        public void ShowContinue()
        {
            ClearChoiceButtons();
            gameObject.SetActive(true);

            if (_continueButton != null) _continueButton.gameObject.SetActive(true);
            if (_choicesContainer != null) _choicesContainer.gameObject.SetActive(false);
        }

        public void ShowChoices(IReadOnlyList<string> labels)
        {
            ClearChoiceButtons();

            if (labels == null || labels.Count == 0)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);
            if (_continueButton != null) _continueButton.gameObject.SetActive(false);
            if (_choicesContainer != null) _choicesContainer.gameObject.SetActive(true);

            SpawnChoiceButtons(labels);
        }

        public void Hide()
        {
            ClearChoiceButtons();
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        private void SpawnChoiceButtons(IReadOnlyList<string> labels)
        {
            if (_choiceButtonPrefab == null || _choicesContainer == null)
            {
                Debug.LogWarning("[DialogueInteractionUI] Missing choiceButtonPrefab or choicesContainer.");
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

                _spawnedButtons.Add(buttonObject);
            }
        }

        private void ClearChoiceButtons()
        {
            foreach (var button in _spawnedButtons)
                if (button != null) Destroy(button);
            _spawnedButtons.Clear();
        }

        #endregion
    }
}
