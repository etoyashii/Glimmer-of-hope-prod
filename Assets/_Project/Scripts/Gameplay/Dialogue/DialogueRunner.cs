using System;
using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Gameplay.Dialogue.Actions;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    public class DialogueRunner : IService
    {
        #region Constants

        public const float DEFAULT_AUTO_ADVANCE_DELAY = 2f;

        #endregion

        #region Private Fields

        private ConversationSO _currentConversation;
        private DialogueLineSO _currentLine;
        private bool _isPlaying;
        private bool _isWaitingForInput;
        private bool _isShowingChoices;
        private bool _isTypewriting;
        private FlagManager _flagManager;

        private StringEventChannel _onDialogueStarted;
        private VoidEventChannel _onDialogueEnded;
        private IntEventChannel _onChoiceMade;

        private DialogueActionDispatcher _actionDispatcher;

        #endregion

        #region Properties

        public bool IsPlaying => _isPlaying;
        public bool IsWaitingForInput => _isWaitingForInput;
        public bool IsShowingChoices => _isShowingChoices;
        public bool IsTypewriting => _isTypewriting;
        public DialogueLineSO CurrentLine => _currentLine;
        public ConversationSO CurrentConversation => _currentConversation;

        #endregion

        #region Events

        /// <summary>
        /// C# Actions for direct UI subscription (tight coupling, same layer).
        /// EventChannels SO are used for cross-layer broadcasting (set via SetEventChannels).
        /// </summary>
        public event Action<DialogueLineSO> OnLineStart;
        public event Action<DialogueLineSO> OnLineEnd;
        public event Action<List<DialogueChoice>> OnChoicesDisplayed;
        public event Action OnSkipRequested;
        public event Action OnDialogueEnd;

        #endregion

        #region IService

        public void Initialize()
        {
            if (!ServiceLocator.TryGet(out _flagManager))
            {
                Debug.LogWarning("[DialogueRunner] FlagManager not found. Conditional branching will be disabled.");
            }

            _actionDispatcher = new DialogueActionDispatcher();
        }

        public void Shutdown()
        {
            EndConversation();
        }

        #endregion

        #region Public Methods

        public void SetEventChannels(StringEventChannel started, VoidEventChannel ended, IntEventChannel choice)
        {
            _onDialogueStarted = started;
            _onDialogueEnded = ended;
            _onChoiceMade = choice;
        }

        public void SetActionEventChannels(
            StringEventChannel onDialogueEvent,
            StringEventChannel onCharacterShow,
            StringEventChannel onCharacterHide)
        {
            _actionDispatcher?.RegisterDefaults(
                _flagManager,
                onDialogueEvent,
                onCharacterShow,
                onCharacterHide);
        }

        public bool StartConversation(ConversationSO conversation)
        {
            if (conversation == null || _isPlaying)
                return false;

            if (!CanStartConversation(conversation))
            {
                Debug.Log($"[DialogueRunner] Cannot start: {conversation.ConversationId} - missing flags");
                return false;
            }

            _currentConversation = conversation;
            _isPlaying = true;

            _onDialogueStarted?.Raise(conversation.ConversationId);

            PlayLine(conversation.StartLine);
            return true;
        }

        public void RequestSkip()
        {
            if (!_isPlaying) return;

            if (_isTypewriting)
            {
                OnSkipRequested?.Invoke();
            }
            else if (_isWaitingForInput && !_isShowingChoices)
            {
                RequestAdvance();
            }
        }

        public void RequestAdvance()
        {
            if (!_isPlaying || _isShowingChoices || _isTypewriting)
                return;

            AdvanceToNextLine();
        }

        public void SelectChoice(int index)
        {
            if (!_isShowingChoices || _currentLine?.Choices == null)
                return;

            var validChoices = GetValidChoices(_currentLine.Choices);
            if (index < 0 || index >= validChoices.Count)
                return;

            var choice = validChoices[index];

            if (!string.IsNullOrEmpty(choice.setFlag))
                _flagManager?.SetFlag(choice.setFlag);

            _onChoiceMade?.Raise(index);
            _isShowingChoices = false;

            if (choice.targetLine != null)
                PlayLine(choice.targetLine);
            else
                EndConversation();
        }

        public void MarkTypewriteComplete()
        {
            _isTypewriting = false;
            _isWaitingForInput = _currentLine?.WaitForInput ?? true;
        }

        public void MarkLineComplete()
        {
            if (_currentLine != null)
            {
                ExecuteActions(_currentLine.OnEndActions);
                OnLineEnd?.Invoke(_currentLine);
            }
        }

        public void EndConversation()
        {
            if (!_isPlaying) return;

            if (_currentConversation?.SetFlagsOnComplete != null)
            {
                foreach (var flag in _currentConversation.SetFlagsOnComplete)
                    _flagManager?.SetFlag(flag);
            }

            // Batch save flags at end of conversation (dirty flag pattern)
            _flagManager?.FlushIfDirty();

            _currentConversation = null;
            _currentLine = null;
            _isPlaying = false;
            _isWaitingForInput = false;
            _isShowingChoices = false;
            _isTypewriting = false;

            _onDialogueEnded?.Raise();
            OnDialogueEnd?.Invoke();
        }

        #endregion

        #region Private Methods

        private void PlayLine(DialogueLineSO line)
        {
            if (line == null)
            {
                EndConversation();
                return;
            }

            _currentLine = line;
            _isTypewriting = true;
            _isWaitingForInput = false;
            _isShowingChoices = false;

            ExecuteActions(line.OnStartActions);
            OnLineStart?.Invoke(line);

            if (line.HasChoices)
            {
                var choices = GetValidChoices(line.Choices);
                if (choices.Count > 0)
                    _isShowingChoices = true;
            }
        }

        private void AdvanceToNextLine()
        {
            MarkLineComplete();

            if (_currentLine.HasChoices)
            {
                var choices = GetValidChoices(_currentLine.Choices);
                if (choices.Count > 0)
                {
                    _isShowingChoices = true;
                    OnChoicesDisplayed?.Invoke(choices);
                    return;
                }
            }

            var next = DetermineNextLine();
            if (next != null)
                PlayLine(next);
            else
                EndConversation();
        }

        private DialogueLineSO DetermineNextLine()
        {
            if (_currentLine.HasConditionals && _flagManager != null)
            {
                foreach (var cond in _currentLine.ConditionalNexts)
                {
                    if (ConditionParser.Evaluate(cond.condition, _flagManager))
                        return cond.gotoLine;
                }
            }

            return _currentLine.NextLine;
        }

        private bool CanStartConversation(ConversationSO conversation)
        {
            if (!conversation.HasRequiredFlags || _flagManager == null)
                return true;

            foreach (var flag in conversation.RequiredFlags)
            {
                if (!_flagManager.HasFlag(flag))
                    return false;
            }

            return true;
        }

        private List<DialogueChoice> GetValidChoices(DialogueChoice[] choices)
        {
            var result = new List<DialogueChoice>();
            if (choices == null) return result;

            foreach (var c in choices)
            {
                if (c == null || c.IsEmpty)
                    continue;

                if (string.IsNullOrEmpty(c.conditionFlag) ||
                    (_flagManager != null && ConditionParser.Evaluate(c.conditionFlag, _flagManager)))
                {
                    result.Add(c);
                }
            }

            return result;
        }

        private void ExecuteActions(DialogueAction[] actions)
        {
            if (actions == null) return;

            _actionDispatcher?.ExecuteAll(actions);
        }

        #endregion
    }
}
