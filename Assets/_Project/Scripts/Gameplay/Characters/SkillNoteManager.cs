using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay
{
    [Serializable]
    /// <summary>
    /// The combo list that can be setted up by Designers. It allow to specify the combo input based on the button ID (left to right : 0 to 2) and then launch the desired method(s)
    /// </summary>
    public class Combo
    {
        public List<int> _combo;
        public UnityEvent _useSkill;
    }

    /// <summary>
    /// The manager that check the player input. There's a maximum delay between input before the combo is resetted
    /// </summary>
    public class SkillNoteManager : MonoBehaviour
    {
        #region SerializeFields

        [Header("Combo stats")]
        [Range(0.5f, 10.0f)]
        [SerializeField] private float _delayBetweenNotes = 1.0f;
        [SerializeField] private List<Combo> _comboList;
        [Range(4, 7)]
        [SerializeField] private int _maxNoteNumber = 4;

        #endregion

        #region PrivateFields

        private List<int> _inputNoteList;
        private int _currentInputNumber = 0;
        private int _validCheck = 0;

        private Coroutine _currentChrono;
        #endregion

        #region UnityLifecycle

        private void Start()
        {
            _inputNoteList = new();

            for (int i = 0; i < _maxNoteNumber; i++)
            {
                _inputNoteList.Add(-1); //default value
            }
        }

        #endregion

        #region PublicMethods

        //Called on the OnClick Drum Buttons
        public void ActivateNote(int noteIndex)
        {
            if (_currentChrono != null)
                StopCoroutine(_currentChrono);

            SaveNote(noteIndex);
        }

        #endregion

        #region PrivateMethods

        private void CheckNoteCombo()
        {
            for (int i = 0; i < _comboList.Count; i++)
            {
                _validCheck = 0;

                for (int j = 0; j < _comboList[i]._combo.Count; j++)
                {
                    //Compare the expected input to the player input
                    if (_comboList[i]._combo[j] == _inputNoteList[j])
                    {
                        _validCheck++;
                    }
                }

                if (_validCheck == _currentInputNumber && _currentInputNumber == _comboList[i]._combo.Count)
                {
                    _comboList[i]._useSkill?.Invoke();
                    break;
                }
            }
        }

        private void SaveNote(int noteIndex)
        {
            //Security
            if (_currentInputNumber >= _maxNoteNumber)
            {
                ResetNotes();
                return;
            }

            _inputNoteList[_currentInputNumber] = noteIndex;

            _currentInputNumber++;
            _currentChrono = StartCoroutine(NoteTimer(_delayBetweenNotes));
        }

        //Take all note and setup them to the default value
        private void ResetNotes()
        {
            for (int i = 0; i < _inputNoteList.Count; i++)
            {
                _inputNoteList[i] = -1;
            }

            _currentInputNumber = 0;

            StopCoroutine(_currentChrono);
        }

        #endregion

        #region Coroutines

        //The timer of the resetter
        private IEnumerator NoteTimer(float delay)
        {
            yield return new WaitForSeconds(delay);

            CheckNoteCombo();
            ResetNotes();
        }

        #endregion
    }
}
