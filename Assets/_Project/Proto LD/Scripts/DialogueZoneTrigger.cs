using UnityEngine;
using GlimmerOfHope.Core.Events;

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

        #endregion

        #region Private Fields

        private bool _hasTriggered;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_playerTag)) return;
            if (_triggerOnce && _hasTriggered) return;

            TriggerDialogue();
        }

        #endregion

        #region Private Methods

        private void TriggerDialogue()
        {
            _hasTriggered = true;
            _onDialogueEvent.Raise(_dialogueKey);
        }

        #endregion
    }
}