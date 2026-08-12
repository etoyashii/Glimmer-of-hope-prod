using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Owns the fixed-on-screen "continue / choices" panel, used when the text and bubble follows the speaker and isn't clickable itself.
    /// </summary>
    public class DialogueInteractionPresenter
    {
        #region Private Fields

        private readonly GameObject _prefab;

        private Action _onContinue;
        private Action<int> _onChoiceSelected;

        private GameObject _instance;
        private IDialogueInteractionUI _ui;

        #endregion

        #region Constructor

        public DialogueInteractionPresenter(GameObject prefab)
        {
            _prefab = prefab;
        }

        #endregion

        #region Public Methods

        //Call once,from DialogueManager.Awake
        public void SetCallbacks(Action onContinue, Action<int> onChoiceSelected)
        {
            _onContinue = onContinue;
            _onChoiceSelected = onChoiceSelected;
        }

        public void ShowContinue()
        {
            EnsureInstance();
            _ui?.ShowContinue();
        }

        public void ShowChoices(IReadOnlyList<string> labels)
        {
            EnsureInstance();
            _ui?.ShowChoices(labels);
        }

        public void Hide() => _ui?.Hide();

        public void Cleanup()
        {
            _ui?.Hide();
            if (_instance != null) UnityEngine.Object.Destroy(_instance);
            _instance = null;
            _ui = null;
        }

        #endregion

        #region Private Methods

        //Instantiates the panel once for the whole dialogue and reuses it after that.
        private void EnsureInstance()
        {
            if (_ui != null) return;
            if (_prefab == null)
            {
                Debug.LogWarning("[DialogueInteractionPresenter] No prefab assigned, can't show continue/choices in followSpeaker mode.");
                return;
            }

            _instance = UnityEngine.Object.Instantiate(_prefab);
            _ui = _instance.GetComponent<IDialogueInteractionUI>();

            if (_ui == null)
            {
                Debug.LogError($"[DialogueInteractionPresenter] Prefab '{_prefab.name}' doesn't implement IDialogueInteractionUI.");
                UnityEngine.Object.Destroy(_instance);
                _instance = null;
                return;
            }

            _ui.Initialize(_onContinue, _onChoiceSelected);
            _ui.Hide();
        }

        #endregion
    }
}
