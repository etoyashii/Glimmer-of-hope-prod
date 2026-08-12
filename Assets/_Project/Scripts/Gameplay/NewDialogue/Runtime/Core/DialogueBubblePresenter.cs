using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Owns everything about the dialogue bubble: which prefab to spawn, where to place it
    /// (above the speaker or fixed on screen), and what to show in it.
    /// </summary>
    public class DialogueBubblePresenter
    {
        #region Private Fields

        private GameObject _instance;
        private GameObject _prefabInUse;
        private IDialogueBubble _bubble;

        private Action _onContinue;
        private Action<int> _onChoiceSelected;

        #endregion

        #region Public Properties

        public bool IsRevealingText => _bubble != null && _bubble.IsRevealingText;

        #endregion

        #region Public Methods

        public void SetCallbacks(Action onContinue, Action<int> onChoiceSelected)
        {
            _onContinue = onContinue;
            _onChoiceSelected = onChoiceSelected;
        }

        public void CompleteTextReveal() => _bubble?.CompleteTextReveal();

        //Instantiates the right prefab if needed, or reuses the current one if it's already the same
        public void EnsureInstance(DialogueNode node)
        {
            if (node.bubblePrefab == null)
            {
                Debug.LogWarning($"[DialogueBubblePresenter] Node '{node.nodeId}' has no bubblePrefab assigned.");
                return;
            }

            bool alreadyUsingRightPrefab = _instance != null && _prefabInUse == node.bubblePrefab;
            if (alreadyUsingRightPrefab) return;

            if (_instance != null)
                UnityEngine.Object.Destroy(_instance);

            _instance = UnityEngine.Object.Instantiate(node.bubblePrefab);
            _prefabInUse = node.bubblePrefab;
            _bubble = _instance.GetComponent<IDialogueBubble>();

            if (_bubble == null)
            {
                Debug.LogError($"[DialogueBubblePresenter] Prefab '{node.bubblePrefab.name}' has no component implementing IDialogueBubble.");
                UnityEngine.Object.Destroy(_instance);
                _instance = null;
                return;
            }

            _bubble.Initialize(_onContinue, _onChoiceSelected);
        }

        //Parents the bubble above the speaker (world space) or detaches it for fixed UI.
        public void Position(DialogueNode node)
        {
            if (_instance == null) return;
            var bubbleTransform = _instance.transform;

            if (node.followSpeaker)
            {
                var speakerTransform = DialogueSpeaker.GetTransform(node.speakerId);
                if (speakerTransform != null)
                {
                    bubbleTransform.SetParent(speakerTransform, worldPositionStays: false);
                    bubbleTransform.localPosition = node.bubbleOffset;
                    return;
                }

                Debug.LogWarning($"[DialogueBubblePresenter] No active DialogueSpeaker with ID '{node.speakerId}', falling back to fixed screen position.");
            }

            bubbleTransform.SetParent(null, worldPositionStays: false);
            bubbleTransform.localPosition = Vector3.zero;
        }

        public void SetContent(DialogueNode node, IReadOnlyList<string> choiceLabels)
        {
            if (_bubble == null) return;
            _bubble.SetText(node.text, node.useTypewriter, node.typewriterCharsPerSecond);
            _bubble.SetChoices(choiceLabels);
    
            _bubble.SetSpeakerName(node.speakerId);
               
        }

        public void Show() => _bubble?.Show();

        public void Cleanup()
        {
            _bubble?.Hide();
            if (_instance != null) UnityEngine.Object.Destroy(_instance);
            _instance = null;
            _prefabInUse = null;
            _bubble = null;
        }

        #endregion
    }
}
