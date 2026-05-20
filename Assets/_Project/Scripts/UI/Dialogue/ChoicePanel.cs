using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.UI.Dialogue
{
    public class ChoicePanel : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private ChoiceButton _buttonPrefab;
        [SerializeField] private Transform _container;
        [SerializeField] private bool _enableKeyboardNav = true;

        #endregion

        #region Private Fields

        private List<ChoiceButton> _buttons = new();
        private int _selectedIndex;
        private bool _isActive;

        #endregion

        #region Events

        public event Action<int> OnChoiceSelected;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!_isActive || !_enableKeyboardNav || _buttons.Count == 0)
                return;

            HandleNavigation();
        }

        #endregion

        #region Public Methods

        public void Show(List<DialogueChoice> choices)
        {
            if (choices == null || choices.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            var texts = new List<string>();
            foreach (var c in choices)
                texts.Add(c.choiceText);

            ShowWithTexts(texts);
        }

        public void ShowWithTexts(List<string> texts)
        {
            Clear();

            if (texts == null || texts.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < texts.Count; i++)
            {
                var btn = Instantiate(_buttonPrefab, _container);
                btn.Setup(i, texts[i], HandleSelection);
                _buttons.Add(btn);
            }

            _selectedIndex = 0;
            UpdateSelection();

            gameObject.SetActive(true);
            _isActive = true;
        }

        public void Hide()
        {
            _isActive = false;
            gameObject.SetActive(false);
            Clear();
        }

        #endregion

        #region Private Methods

        private void Clear()
        {
            foreach (var btn in _buttons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            _buttons.Clear();
            _selectedIndex = 0;
        }

        private void HandleNavigation()
        {
            if (Keyboard.current == null) return;

            bool up = Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame;
            bool down = Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame;
            bool confirm = Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;

            if (up)
            {
                _selectedIndex = (_selectedIndex - 1 + _buttons.Count) % _buttons.Count;
                UpdateSelection();
            }
            else if (down)
            {
                _selectedIndex = (_selectedIndex + 1) % _buttons.Count;
                UpdateSelection();
            }
            else if (confirm)
            {
                HandleSelection(_selectedIndex);
            }
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (i == _selectedIndex)
                    _buttons[i].Select();
                else
                    _buttons[i].Deselect();
            }
        }

        private void HandleSelection(int index)
        {
            if (index < 0 || index >= _buttons.Count)
                return;

            _isActive = false;
            OnChoiceSelected?.Invoke(index);
        }

        #endregion
    }
}
