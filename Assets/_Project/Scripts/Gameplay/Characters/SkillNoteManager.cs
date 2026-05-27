using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    [Serializable]
    public class Combo
    {
        public List<int> _combo;
        public GameObject _cubePrefab;
        public Transform _spawnPoint;
    }

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
                    if (_comboList[i]._combo[j] == _inputNoteList[j])
                    {
                        _validCheck++;
                    }
                }

                if (_validCheck == _currentInputNumber && _currentInputNumber == _comboList[i]._combo.Count)
                {
                    Instantiate(_comboList[i]._cubePrefab, _comboList[i]._spawnPoint.position, _comboList[i]._spawnPoint.rotation);
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
            {
                _inputNoteList[i] = -1;
            }

            _currentInputNumber = 0;

            StopCoroutine(_currentChrono);
        }

        #endregion

        #region Coroutines

        private IEnumerator NoteTimer(float delay)
        {
            yield return new WaitForSeconds(delay);

            CheckNoteCombo();
            ResetNotes();
        }

        #endregion
    }
}
