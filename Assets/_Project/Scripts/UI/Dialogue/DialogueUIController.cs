using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Localization;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.UI.Dialogue
{
    public class DialogueUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panels")]
        [SerializeField] private GameObject _dialoguePanel;

        [Header("Speaker")]
        [SerializeField] private TMP_Text _speakerName;
        [SerializeField] private Image _speakerPortrait;

        [Header("Dialogue")]
        [SerializeField] private TypewriterEffect _typewriter;

        [Header("Indicators")]
        [SerializeField] private GameObject _continueIndicator;

        [Header("Choices")]
        [SerializeField] private ChoicePanel _choicePanel;

        [Header("Event Channels")]
        [SerializeField] private StringEventChannel _onDialogueStarted;
        [SerializeField] private VoidEventChannel _onDialogueEnded;
        [SerializeField] private IntEventChannel _onChoiceMade;

        #endregion

        #region Private Fields

        private DialogueRunner _runner;
        private LocalizationManager _localization;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (ServiceLocator.TryGet<DialogueRunner>(out var runner))
            {
                _runner = runner;
                _runner.SetEventChannels(_onDialogueStarted, _onDialogueEnded, _onChoiceMade);
                Subscribe();
            }

            ServiceLocator.TryGet(out _localization);

            Hide();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (_runner == null || !_runner.IsPlaying)
                return;

            HandleInput();
        }

        #endregion

        #region Public Methods

        public void Show()
        {
            _dialoguePanel?.SetActive(true);
        }

        public void Hide()
        {
            _dialoguePanel?.SetActive(false);
            _choicePanel?.Hide();
            HideContinueIndicator();
        }

        #endregion

        #region Private Methods

        private void Subscribe()
        {
            if (_runner == null) return;

            _runner.OnLineStart += HandleLineStart;
            _runner.OnSkipRequested += HandleSkipRequest;
            _runner.OnChoicesDisplayed += HandleChoicesDisplayed;
            _runner.OnDialogueEnd += HandleDialogueEnd;

            _typewriter.OnComplete += HandleTypewriteComplete;
            _choicePanel.OnChoiceSelected += HandleChoiceSelected;
        }

        private void Unsubscribe()
        {
            if (_runner != null)
            {
                _runner.OnLineStart -= HandleLineStart;
                _runner.OnSkipRequested -= HandleSkipRequest;
                _runner.OnChoicesDisplayed -= HandleChoicesDisplayed;
                _runner.OnDialogueEnd -= HandleDialogueEnd;
            }

            if (_typewriter != null)
                _typewriter.OnComplete -= HandleTypewriteComplete;

            if (_choicePanel != null)
                _choicePanel.OnChoiceSelected -= HandleChoiceSelected;
        }

        private void HandleInput()
        {
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

            if (spacePressed || mouseClicked)
            {
                _runner.RequestSkip();
            }
        }

        private void HandleLineStart(DialogueLineSO line)
        {
            Show();
            HideContinueIndicator();
            _choicePanel?.Hide();

            SetSpeaker(line.Speaker, line.Emotion);

            var text = GetLocalizedText(line);
            _typewriter?.Play(text, line.TypewriterSpeed);
        }

        private string GetLocalizedText(DialogueLineSO line)
        {
            if (_localization == null)
            {
                Debug.LogWarning($"[Dialogue] LocalizationManager is null! LineId={line.LineId}");
                return $"[{line.LineId}]";
            }

            var tableName = $"dialogue_{line.ConversationId}";
            var text = _localization.GetLocalizedString(tableName, line.LineId);

            Debug.Log($"[Dialogue] Table={tableName}, Key={line.LineId}, Result={text}");
            return text;
        }

        private string GetLocalizedChoiceText(DialogueLineSO line, int choiceIndex, string fallback)
        {
            if (_localization == null)
                return fallback;

            var tableName = $"dialogue_{line.ConversationId}";
            var key = $"{line.LineId}_choice{choiceIndex + 1}";
            var localized = _localization.GetLocalizedString(tableName, key);

            if (localized.StartsWith("[") && localized.EndsWith("]"))
                return fallback;

            return localized;
        }

        private void HandleSkipRequest()
        {
            if (_typewriter != null && _typewriter.IsPlaying)
            {
                _typewriter.Skip();
            }
        }

        private void HandleTypewriteComplete()
        {
            _runner?.MarkTypewriteComplete();

            if (_runner.CurrentLine.HasChoices)
            {
                var choices = GetValidChoices(_runner.CurrentLine.Choices);
                if (choices.Count > 0)
                {
                    ShowLocalizedChoices(choices);
                    return;
                }
            }

            if (_runner.CurrentLine.WaitForInput)
                ShowContinueIndicator();
        }

        private void HandleChoicesDisplayed(List<DialogueChoice> choices)
        {
            ShowLocalizedChoices(choices);
        }

        private void ShowLocalizedChoices(List<DialogueChoice> choices)
        {
            if (_choicePanel == null || choices == null)
                return;

            var localizedTexts = new List<string>();
            var line = _runner.CurrentLine;

            for (int i = 0; i < choices.Count; i++)
            {
                var text = GetLocalizedChoiceText(line, i, choices[i].choiceText);
                localizedTexts.Add(text);
            }

            _choicePanel.ShowWithTexts(localizedTexts);
        }

        private void HandleChoiceSelected(int index)
        {
            _runner?.SelectChoice(index);
        }

        private void HandleDialogueEnd()
        {
            Hide();
        }

        private void SetSpeaker(CharacterSO character, EmotionType emotion)
        {
            if (character == null)
            {
                if (_speakerName != null) _speakerName.text = "";
                if (_speakerPortrait != null) _speakerPortrait.gameObject.SetActive(false);
                return;
            }

            if (_speakerName != null)
            {
                _speakerName.text = character.DisplayName;
                _speakerName.color = character.NameColor;
            }

            if (_speakerPortrait != null)
            {
                var portrait = character.GetPortrait(emotion);
                if (portrait != null)
                {
                    _speakerPortrait.sprite = portrait;
                    _speakerPortrait.gameObject.SetActive(true);
                }
                else
                {
                    _speakerPortrait.gameObject.SetActive(false);
                }
            }
        }

        private void ShowContinueIndicator()
        {
            _continueIndicator?.SetActive(true);
        }

        private void HideContinueIndicator()
        {
            _continueIndicator?.SetActive(false);
        }

        private List<DialogueChoice> GetValidChoices(DialogueChoice[] choices)
        {
            var result = new List<DialogueChoice>();
            if (choices == null) return result;

            if (!ServiceLocator.TryGet<FlagManager>(out var fm))
            {
                result.AddRange(choices);
                return result;
            }

            foreach (var c in choices)
            {
                if (string.IsNullOrEmpty(c.conditionFlag) || ConditionParser.Evaluate(c.conditionFlag, fm))
                    result.Add(c);
            }

            return result;
        }

        #endregion
    }
}
