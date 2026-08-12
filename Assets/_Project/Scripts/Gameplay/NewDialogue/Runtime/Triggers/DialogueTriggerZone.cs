using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Serialization;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Drop this on a GameObject with a Collider set to "Is Trigger", it Starts the dialogue automatically when the player walks in 
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DialogueTriggerZone : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip("Dialogue to start when the player enters the zone.")]
        [FormerlySerializedAs("dialogueGraph")]
        [SerializeField] private DialogueGraph _dialogueGraph;

        [Tooltip("Tag used to identify the player GameObject.")]
        [FormerlySerializedAs("playerTag")]
        [SerializeField] private string _playerTag = "Player";

        [Tooltip("If checked, the zone only fires once and then disables itself.")]
        [FormerlySerializedAs("triggerOnce")]
        [SerializeField] private bool _triggerOnce = true;

        [Tooltip("If checked, ignores re-entry while a dialogue is already playing.")]
        [FormerlySerializedAs("ignoreIfAlreadyPlaying")]
        [SerializeField] private bool _ignoreIfAlreadyPlaying = true;

        [Header("Ending")]
        [Tooltip("If the dialogue ends on an End node with one of these Outcome IDs, this zone disables itself for good.")]
        [FormerlySerializedAs("disableOnOutcomes")]
        [SerializeField] private List<string> _disableOnOutcomes = new List<string>();

        #endregion

        #region Private Fields

        private bool _hasTriggered;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.OnDialogueEndedWithOutcome += HandleDialogueEnded;
            Debug.Log($"[DEBUG] OnEnable - DialogueManager.Instance is {(DialogueManager.Instance == null ? "NULL" : "OK")}");

        }

        private void OnDisable()
        {
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.OnDialogueEndedWithOutcome -= HandleDialogueEnded;

        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered && _triggerOnce) return;
            if (!other.CompareTag(_playerTag)) return;

            if (_dialogueGraph == null)
            {
                Debug.LogWarning($"[DialogueTriggerZone] No dialogueGraph assigned on '{name}'.");
                return;
            }

            if (_ignoreIfAlreadyPlaying && DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
                return;

            _hasTriggered = true;
            DialogueManager.Instance.StartDialogue(_dialogueGraph);
        }

        #endregion

        #region Private Methods

        private void HandleDialogueEnded(DialogueGraph endedGraph, string outcomeId)
        {
            Debug.Log($"[DEBUG] HandleDialogueEnded called with outcomeId='{outcomeId}', graphMatch={endedGraph == _dialogueGraph}");

            if (endedGraph != _dialogueGraph) return;
            if (_disableOnOutcomes.Count == 0) return;
            if (!_disableOnOutcomes.Contains(outcomeId)) return;

            enabled = false;
        }

        #endregion

        #region Editor

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        #endregion
    }
}
