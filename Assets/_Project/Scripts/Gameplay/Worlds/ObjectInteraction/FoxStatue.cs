using GlimmerOfHope.Gameplay.Character.SpecialActions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Activate StatueFox dialog (onboarding commands) when player goes on triggerbox
    /// Watch out ! This script not working for more than 1 string (Work In Progress script)
    /// </summary>
    public class FoxStatue : MonoBehaviour
    {
        #region SerializeField

        [SerializeField] private int _statueId = 1;
        [SerializeField] private GameObject _skillUI;
        [SerializeField] private GameObject _jumpUI;
        [SerializeField] private GameObject _talkUI;
        [SerializeField] private TextMeshProUGUI _textMesh;
        [SerializeField] private List<string> _textList;
        [SerializeField] private Movement _playerMovemement;

        #endregion

        #region PrivateFields

        private bool _wasAlreadyTalked = false;

        #endregion

        #region UnityLifecycle

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                if (_wasAlreadyTalked == true) return;

                _wasAlreadyTalked = true;
                ToggleUI();
                Speak();
            }
        }

        private void Speak()
        {
            if (_textList.Count <= 0) return;

            _playerMovemement.SetMovementEnabled(false);
            _textMesh.text = _textList[0];

            if (_statueId < 0) return;

            PlayerSignalManager.Instance.SetSkillLearnId(_statueId);
        }

        #endregion

        #region PrivateMethods

        private void ToggleUI()
        {
            _skillUI.SetActive(!_skillUI.activeSelf);
            _talkUI.SetActive(!_talkUI.activeSelf);
        }

        #endregion
    }
}
