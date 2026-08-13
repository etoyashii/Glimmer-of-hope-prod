using GlimmerOfHope.Gameplay.Character.SpecialActions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GlimmerOfHope.Gameplay
{
    [Serializable]
    /// <summary>
    /// The combo list that can be set up by Designers. It allows specifying the combo input
    /// based on the button ID (left to right: 0 to 2) and then launching the desired method(s).
    /// </summary>
    public class Combo
    {
        [Tooltip("Skill this combo unlocks and previews. Set explicitly instead of relying on list order matching the SkillType enum.")]
        public SkillManager.SkillType _skillType;

        public List<int> _combo;
        public UnityEvent _useSkill;
    }

    /// <summary>
    /// Checks player input for skill combos. There is a maximum delay between inputs
    /// before the combo is reset.
    /// On mobile: ActivateNote() is called directly from UI buttons.
    /// On Keyboard/Mouse: 1 = note 0, 2 = note 1, 3 = note 2.
    /// On Gamepad: Button East = note 0, Button North = note 1, Button West = note 2.
    /// The note buttons UI stays visible on every scheme, since it also
    /// serves as the visual reveal for newly unlocked combos, but is only
    /// interactable on Mobile so a mouse click cannot accidentally trigger
    /// a note on Keyboard/Mouse or Gamepad.
    /// </summary>
    public class SkillNoteManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Combo Stats")]
        [Range(0.5f, 10.0f)]
        [SerializeField] private float _delayBetweenNotes = 1.0f;
        [SerializeField] private List<Combo> _comboList;
        [Range(4, 7)]
        [SerializeField] private int _maxNoteNumber = 4;

        [Header("References")]
        [SerializeField] private SkillManager _skillLearningManager;

        [Header("Mobile UI Buttons")]
        [Tooltip("The three skill input buttons shown on mobile (index 0, 1, 2). Stays visible on all schemes, only interactable on Mobile.")]
        [SerializeField] private List<Button> _playerSkillInputList;

        [Header("Input Actions")]
        [Tooltip("Note 0 - A [Keyboard] / Button East [Gamepad]")]
        [SerializeField] private InputActionReference _note0Action;

        [Tooltip("Note 1 - E [Keyboard] / Button North [Gamepad]")]
        [SerializeField] private InputActionReference _note1Action;

        [Tooltip("Note 2 - R [Keyboard] / Button West [Gamepad]")]
        [SerializeField] private InputActionReference _note2Action;

        #endregion

        #region Private Fields

        private List<int> _inputNoteList;
        private int _currentInputNumber = 0;
        private int _validCheck = 0;
        private Coroutine _currentChrono;
        private Coroutine _currentRevealRoutine;

        // Default idle color of each note button, captured once so a reveal
        // interrupted mid flash never leaves a button stuck red.
        private Color[] _defaultButtonColors;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _skillLearningManager._skillTypeUnlocked += ShowCombo;
        }

        private void Start()
        {
            _inputNoteList = new();
            for (int i = 0; i < _maxNoteNumber; i++)
                _inputNoteList.Add(-1); // default value

            CacheDefaultButtonColors();
        }

        private void OnEnable()
        {
            EnableAction(_note0Action, OnNote0);
            EnableAction(_note1Action, OnNote1);
            EnableAction(_note2Action, OnNote2);

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnSchemeChanged.AddListener(OnSchemeChanged);
                ApplyBindingMask(InputManager.Instance.CurrentScheme);
                SetNoteButtonsInteractable(InputManager.Instance.CurrentScheme == InputManager.ControlScheme.Mobile);
            }
        }

        private void OnDisable()
        {
            _skillLearningManager._skillTypeUnlocked -= ShowCombo;

            DisableAction(_note0Action, OnNote0);
            DisableAction(_note1Action, OnNote1);
            DisableAction(_note2Action, OnNote2);

            if (InputManager.Instance != null)
                InputManager.Instance.OnSchemeChanged.RemoveListener(OnSchemeChanged);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called from UI buttons on mobile, or automatically from InputActions on other schemes.
        /// noteIndex: 0 = first button/A/East, 1 = second/E/North, 2 = third/R/West.
        /// </summary>
        public void ActivateNote(int noteIndex)
        {
            if (_currentChrono != null)
                StopCoroutine(_currentChrono);

            SaveNote(noteIndex);
        }

        /// <summary>
        /// Plays the reveal animation for the combo tied to the given skill,
        /// only if that skill is unlocked. Safe to call at any time, not
        /// just right after an unlock, for example from a spellbook or a
        /// help button. Cancels and resets any reveal already playing so
        /// two calls never overlap on the same buttons.
        /// </summary>
        public void ShowCombo(int skillTypeIndex)
        {
            if (!_skillLearningManager.IsSkillUnlocked(skillTypeIndex)) return;

            int comboIndex = _comboList.FindIndex(c => (int)c._skillType == skillTypeIndex);
            if (comboIndex == -1) return;

            if (_currentRevealRoutine != null)
                StopCoroutine(_currentRevealRoutine);

            ResetButtonColors();
            _currentRevealRoutine = StartCoroutine(RevealCombo(0.4f, 0.2f, comboIndex));
        }

        #endregion

        #region Private Methods - Input

        private void OnNote0(InputAction.CallbackContext ctx) => ActivateNote(0);
        private void OnNote1(InputAction.CallbackContext ctx) => ActivateNote(1);
        private void OnNote2(InputAction.CallbackContext ctx) => ActivateNote(2);

        private void OnSchemeChanged(InputManager.ControlScheme scheme)
        {
            // Reset any ongoing combo when switching schemes
            if (_currentChrono != null) StopCoroutine(_currentChrono);
            ResetNotes();

            ApplyBindingMask(scheme);
            SetNoteButtonsInteractable(scheme == InputManager.ControlScheme.Mobile);
        }

        /// <summary>
        /// On mobile the actions are disabled - UI buttons call ActivateNote() directly.
        /// On other schemes only the relevant bindings are active.
        /// </summary>
        private void ApplyBindingMask(InputManager.ControlScheme scheme)
        {
            InputBinding? mask = scheme switch
            {
                InputManager.ControlScheme.Mobile => null,
                InputManager.ControlScheme.KeyboardMouse => InputBinding.MaskByGroup("Keyboard/Mouse"),
                InputManager.ControlScheme.Gamepad => InputBinding.MaskByGroup("Gamepad"),
                _ => null
            };

            bool isMobile = scheme == InputManager.ControlScheme.Mobile;

            ApplyMaskToAction(_note0Action, mask, isMobile);
            ApplyMaskToAction(_note1Action, mask, isMobile);
            ApplyMaskToAction(_note2Action, mask, isMobile);
        }

        private void ApplyMaskToAction(InputActionReference actionRef, InputBinding? mask, bool disable)
        {
            if (actionRef == null) return;

            actionRef.action.bindingMask = mask;

            if (disable)
                actionRef.action.Disable();
            else
                actionRef.action.Enable();
        }

        private void EnableAction(InputActionReference actionRef, Action<InputAction.CallbackContext> callback)
        {
            if (actionRef == null) return;
            actionRef.action.Enable();
            actionRef.action.performed += callback;
        }

        private void DisableAction(InputActionReference actionRef, Action<InputAction.CallbackContext> callback)
        {
            if (actionRef == null) return;
            actionRef.action.Disable();
            actionRef.action.performed -= callback;
        }

        /// <summary>
        /// Note buttons stay visible on every scheme so they can still show
        /// the combo reveal animation on Keyboard/Mouse and Gamepad, but
        /// only respond to clicks on Mobile.
        /// </summary>
        private void SetNoteButtonsInteractable(bool interactable)
        {
            foreach (Button button in _playerSkillInputList)
                if (button != null) button.interactable = interactable;
        }

        #endregion

        #region Private Methods - Combo Logic

        private void CheckNoteCombo()
        {
            for (int i = 0; i < _comboList.Count; i++)
            {
                _validCheck = 0;

                for (int j = 0; j < _comboList[i]._combo.Count; j++)
                {
                    if (_comboList[i]._combo[j] == _inputNoteList[j])
                        _validCheck++;
                }

                if (_validCheck == _currentInputNumber && _currentInputNumber == _comboList[i]._combo.Count)
                {
                    if (_skillLearningManager.IsSkillUnlocked((int)_comboList[i]._skillType))
                        _comboList[i]._useSkill?.Invoke();

                    break;
                }
            }
        }

        private void SaveNote(int noteIndex)
        {
            if (_currentInputNumber >= _maxNoteNumber)
            {
                ResetNotes();
                return;
            }

            _inputNoteList[_currentInputNumber] = noteIndex;
            _currentInputNumber++;
            _currentChrono = StartCoroutine(NoteTimer(_delayBetweenNotes));
        }

        private void ResetNotes()
        {
            for (int i = 0; i < _inputNoteList.Count; i++)
                _inputNoteList[i] = -1;

            _currentInputNumber = 0;
        }

        #endregion

        #region Private Methods - Reveal Animation

        private void CacheDefaultButtonColors()
        {
            _defaultButtonColors = new Color[_playerSkillInputList.Count];

            for (int i = 0; i < _playerSkillInputList.Count; i++)
            {
                Image image = _playerSkillInputList[i].GetComponent<Image>();
                if (image != null)
                    _defaultButtonColors[i] = image.color;
            }
        }

        private void ResetButtonColors()
        {
            for (int i = 0; i < _playerSkillInputList.Count; i++)
            {
                Image image = _playerSkillInputList[i].GetComponent<Image>();
                if (image != null)
                    image.color = _defaultButtonColors[i];
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator NoteTimer(float delay)
        {
            yield return new WaitForSeconds(delay);

            CheckNoteCombo();
            ResetNotes();
        }

        private IEnumerator RevealCombo(float t1, float t2, int comboIndex)
        {
            yield return new WaitForSeconds(t1);

            List<int> combo = _comboList[comboIndex]._combo;

            for (int i = 0; i < combo.Count; i++)
            {
                int buttonIndex = combo[i];
                Image image = _playerSkillInputList[buttonIndex].GetComponent<Image>();
                if (image == null) continue;

                yield return new WaitForSeconds(t1);
                image.color = Color.red;

                yield return new WaitForSeconds(t2);
                image.color = _defaultButtonColors[buttonIndex];
            }

            _currentRevealRoutine = null;
        }

        #endregion
    }
}