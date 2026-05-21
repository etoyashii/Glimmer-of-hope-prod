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
        #region Enums

        public enum Steps
        {
            First,
            Second,
            Third,
            Fourth
        }

        //I pref to attach _currentStep around his enum instead of putting on the PrivateFields region, can be moved as you wish
        private Steps _currentStep = Steps.First;

        #endregion

        #region SerializeFields

        [Range(0.5f, 10.0f)]
        [SerializeField] private float _delayBetweenNotes = 1.0f;
        [SerializeField] private List<Combo> _comboList;
        [SerializeField] private int _maxNoteNumber = 3;

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

            _currentInputNumber++;
            switch (_currentStep)
            {
                case Steps.First:
                    SaveNote((int)_currentStep, noteIndex);
                    IncrementStep();
                    break;
                case Steps.Second:
                    SaveNote((int)_currentStep, noteIndex);
                    IncrementStep();
                    break;
                case Steps.Third:
                    SaveNote((int)_currentStep, noteIndex);
                    IncrementStep();
                    break;
                case Steps.Fourth:
                    SaveNote((int)_currentStep, noteIndex);
                    IncrementStep();
                    break;
            }
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
            
            Debug.Log("Combo checked");
        }

        private void SaveNote(int currentStep, int noteIndex)
        {
            _inputNoteList[currentStep] = noteIndex;

            _currentChrono = StartCoroutine(NoteTimer(_delayBetweenNotes));
        }

        private void IncrementStep()
        {
            _currentStep++;
            Debug.Log("Current step : " + _currentStep);
        }

        private void ResetStep()
        {
            _currentStep = Steps.First;

            for (int i = 0; i < _inputNoteList.Count; i++)
            {
                _inputNoteList[i] = -1;
            }

            StopCoroutine(_currentChrono);
            _currentInputNumber = 0;
            Debug.Log("Step resetted");
        }

        #endregion

        #region Coroutines

        private IEnumerator NoteTimer(float delay)
        {
            yield return new WaitForSeconds(delay);

            CheckNoteCombo();
            ResetStep();
        }

        #endregion
    }
}
