using GlimmerOfHope.Core.Events;
using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    /// <summary>
    /// Triggers a dialogue event when the player enters the sphere collider.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class DialogueZoneTrigger : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Dialogue")]
        [SerializeField] private StringEventChannel _onDialogueEvent;
        [SerializeField] private string _dialogueKey;

        [Header("Settings")]
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private bool _triggerOnce = true;

        [Header("Camera")]
        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private Volume _volume;
        private CinematicBarsEffect _cineBars;

        #endregion

        #region Private Fields

        private bool _hasTriggered;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
            _volume.profile.TryGet(out _cineBars);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_playerTag)) return;
           // if (_triggerOnce && _hasTriggered) return;

            TriggerDialogue();
        }

        #endregion

        #region Private Methods

        private void TriggerDialogue()
        {
            Debug.Log("dsqdqs");
            _hasTriggered = true;

            //_onDialogueEvent.Raise(_dialogueKey);
            StartCoroutine(SetUpBars(true));
            _camera.Priority = 15;
        }

        private void OnTriggerExit(Collider other)
        {
            _camera.Priority = 0;
            StartCoroutine(SetUpBars(false));
        }

        IEnumerator SetUpBars(bool dir)
        {
            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                if (dir)
                    _cineBars.barSize.value = Mathf.Lerp(0f, 0.12f, t);
                else
                    _cineBars.barSize.value = Mathf.Lerp(0.12f, 0f, t);
                yield return null;
            }
        }

        #endregion
    }
}