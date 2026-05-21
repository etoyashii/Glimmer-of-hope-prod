using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class SkillNoteManager : MonoBehaviour
    {
        #region Enums

        public enum Steps
        {
            First,
            Second,
            Third
        }

        //I pref to attach _currentStep around his enum instead of putting on the PrivateFields region, can be moved as you wish
        private Steps _currentStep = Steps.First;

        #endregion

        #region SerializeFields

        [SerializeField] private GameObject _cubePrefab;
        [SerializeField] private Transform _spawnPoint;
        [Range(0.5f, 10.0f)]
        [SerializeField] private float _delayBetweenNotes = 1.0f;

        #endregion

        #region PrivateFields

        private List<int> _inputNoteList;
        private int _noteNumber = 3;

        private Coroutine _currentChrono;
        #endregion

        #region UnityLifecycle

        private void Start()
        {
            _inputNoteList = new();

            for (int i = 0; i < _noteNumber; i++)
            {
                _inputNoteList.Add(-1); //default value
            }
            
            Debug.Log(_inputNoteList.Count);
        }

        #endregion

        #region PublicMethods


        public void ActivateNote(int noteIndex)
        {
            if (_currentChrono != null)
                StopCoroutine(_currentChrono);

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
                    CheckNoteCombo();
                    ResetStep();
                    break;
            }
        }

        #endregion

        #region PrivateMethods

        private void CheckNoteCombo()
        {
            for (int i = 0; i < _inputNoteList.Count; i++)
            {
                Debug.Log(_inputNoteList[i].ToString());
            }

            if (_inputNoteList[0] == 0 && _inputNoteList[1] == 0 && _inputNoteList[2] == 0)
            {
                Instantiate(_cubePrefab, _spawnPoint.position, _spawnPoint.rotation);
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
            Debug.Log("Step resetted");
        }

        #endregion

        #region Coroutines

        private IEnumerator NoteTimer(float delay)
        {
            yield return new WaitForSeconds(delay);

            ResetStep();
        }

        #endregion
    }
}
