using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Drop this on an NPC , Spawns a world-space button above the NPC, 
    /// visible only when the player is in range and starts the dialogue on click.
    /// </summary>
    public class DialogueTriggerButton : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip("Dialogue to start on click.")]
        [FormerlySerializedAs("dialogueGraph")]
        [SerializeField] private DialogueGraph _dialogueGraph;

        [Tooltip("World-space button prefab (Canvas + Button). If empty, falls back to the Start node's offset for placement.")]
        [FormerlySerializedAs("buttonPrefab")]
        [SerializeField] private GameObject _buttonPrefab;

        [Tooltip("Transform to anchor the button to. Leave empty to use this GameObject.")]
        [FormerlySerializedAs("anchor")]
        [SerializeField] private Transform _anchor;

        [Header("Range Detection")]
        [FormerlySerializedAs("alwaysVisible")]
        [SerializeField] private bool _alwaysVisible;

        [FormerlySerializedAs("playerTag")]
        [SerializeField] private string _playerTag = "Player";

        [FormerlySerializedAs("interactionRange")]
        [SerializeField] private float _interactionRange = 3f;

        [FormerlySerializedAs("hideWhileDialoguePlaying")]
        [SerializeField] private bool _hideWhileDialoguePlaying = true;

        [Header("Ending")]
        [Tooltip("If the dialogue ends on an End node with one of these Outcome IDs, this button disables itself for good.")]
        [FormerlySerializedAs("disableOnOutcomes")]
        [SerializeField] private List<string> _disableOnOutcomes = new List<string>();

        #endregion

        #region Private Fields

        private GameObject _buttonInstance;
        private Transform _playerTransform;
        private bool _isVisible;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SpawnButton();
            FindPlayer();

            if (DialogueManager.Instance != null)
                DialogueManager.Instance.OnDialogueEndedWithOutcome += HandleDialogueEnded;
            Debug.Log($"[DEBUG] OnEnable - DialogueManager.Instance is {(DialogueManager.Instance == null ? "NULL" : "OK")}");

        }

        private void OnDisable()
        {
            if (_buttonInstance != null) Destroy(_buttonInstance);

            if (DialogueManager.Instance != null)
                DialogueManager.Instance.OnDialogueEndedWithOutcome -= HandleDialogueEnded;
        }

        private void Update()
        {
            if (_buttonInstance == null || _alwaysVisible) return;

            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            bool dialogueBlocking = _hideWhileDialoguePlaying && DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying;
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            bool shouldBeVisible = !dialogueBlocking && distance <= _interactionRange;

            if (shouldBeVisible != _isVisible)
                SetVisible(shouldBeVisible);
        }

        #endregion

        #region Private Methods

        private void SpawnButton()
        {
            if (_buttonPrefab == null)
            {
                Debug.LogWarning($"[DialogueTriggerButton] No buttonPrefab assigned on '{name}'.");
                return;
            }

            var anchorTransform = _anchor != null ? _anchor : transform;

            // Falls back to the offset configured on the dialogue's Start node.
            Vector3 offset = Vector3.up * 2f;
            var startNode = _dialogueGraph != null ? _dialogueGraph.GetStartNode() : null;
            if (startNode != null) offset = startNode.buttonOffset;

            _buttonInstance = Instantiate(_buttonPrefab, anchorTransform);
            _buttonInstance.transform.localPosition = offset;

            var button = _buttonInstance.GetComponentInChildren<Button>();
            if (button != null)
                button.onClick.AddListener(OnButtonClicked);
            else
                Debug.LogWarning($"[DialogueTriggerButton] buttonPrefab has no Button component on '{name}'.");

            SetVisible(_alwaysVisible);
        }

        private void FindPlayer()
        {
            if (_alwaysVisible) return;
            var playerObject = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerObject != null) _playerTransform = playerObject.transform;
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_buttonInstance != null) _buttonInstance.SetActive(visible);
        }

        private void OnButtonClicked()
        {
            if (_dialogueGraph == null)
            {
                Debug.LogWarning($"[DialogueTriggerButton] No dialogueGraph assigned on '{name}'.");
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("[DialogueTriggerButton] No DialogueManager found in the scene.");
                return;
            }

            DialogueManager.Instance.StartDialogue(_dialogueGraph);
        }

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

        private void OnDrawGizmosSelected()
        {
            if (_alwaysVisible) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }

        #endregion
    }
}
