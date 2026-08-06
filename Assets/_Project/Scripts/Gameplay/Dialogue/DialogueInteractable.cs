using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Gameplay.Character.SpecialActions;
using Debug = UnityEngine.Debug;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    /// <summary>
    /// Trigger-based dialogue interaction.
    /// Place on a GameObject with a trigger Collider. Player enters range,
    /// presses interact key, dialogue starts and movement locks.
    /// Supports conditional conversations based on FlagManager state.
    /// </summary>
    public class DialogueInteractable : MonoBehaviour
    {
        #region Constants

        private const string PLAYER_TAG = "Player";
        private const string PROMPT_TEXT = "Parler (E)";
        private const float PROMPT_WIDTH = 160f;
        private const float PROMPT_HEIGHT = 30f;

        #endregion

        #region Serialized Fields

        [Header("Dialogue")]
        [SerializeField] private ConversationSO _conversation;

        [Header("Conditional")]
        [Tooltip("If flag is set, this conversation plays instead.")]
        [SerializeField] private ConversationSO _conditionalConversation;
        [SerializeField] private string _requiredFlag;
        [Tooltip("Deactivate this GameObject after conditional dialogue ends.")]
        [SerializeField] private bool _hideAfterConditional;
        
        #endregion

        #region Private Fields

        private DialogueRunner _runner;
        private Movement _playerMovement;
        private bool _playerInRange;
        private bool _isDialogueActive;
        private bool _usedConditional;
        
        #endregion

        #region Properties

        public bool CanInteract => _playerInRange && !_isDialogueActive;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            TryGetRunner();
        }

        private void Update()
        {
            if (_runner == null)
                TryGetRunner();

            if (!_playerInRange || _isDialogueActive) return;
            if (Keyboard.current == null) return;
            if (!Keyboard.current.eKey.wasPressedThisFrame) return;

            StartDialogue();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(PLAYER_TAG)) return;

            _playerInRange = true;
            _playerMovement = other.GetComponent<Movement>();
            LogVerbose("Player entered range");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(PLAYER_TAG)) return;

            _playerInRange = false;
            _playerMovement = null;
            LogVerbose("Player left range");
        }

        private void OnDestroy()
        {
            if (_runner != null)
                _runner.OnDialogueEnd -= HandleDialogueEnd;
        }

        #endregion

        #region Public Methods

        public void StartDialogue()
        {
            if (_runner == null)
            {
                TryGetRunner();
                if (_runner == null)
                {
                    Debug.LogError("[DialogueInteractable] DialogueRunner not found");
                    return;
                }
            }

            if (_conversation == null)
            {
                Debug.LogError("[DialogueInteractable] No conversation assigned");
                return;
            }

            if (_runner.IsPlaying) return;

            var conversation = ResolveConversation(out _usedConditional);

            _isDialogueActive = true;
            _playerMovement?.SetMovementEnabled(false);

            _runner.OnDialogueEnd += HandleDialogueEnd;
            _runner.StartConversation(conversation);

            LogVerbose($"Started: {conversation.ConversationId}");
        }

        #endregion

        #region Private Methods

        private void TryGetRunner()
        {
            if (ServiceLocator.TryGet(out _runner))
                LogVerbose("DialogueRunner found");
        }

        private ConversationSO ResolveConversation(out bool isConditional)
        {
            isConditional = false;

            if (string.IsNullOrEmpty(_requiredFlag) || _conditionalConversation == null)
                return _conversation;

            if (!ServiceLocator.TryGet<FlagManager>(out var flags))
                return _conversation;

            if (!flags.HasFlag(_requiredFlag))
                return _conversation;

            isConditional = true;
            return _conditionalConversation;
        }

        private void HandleDialogueEnd()
        {
            _runner.OnDialogueEnd -= HandleDialogueEnd;
            _playerMovement?.SetMovementEnabled(true);
            _isDialogueActive = false;

            if (_usedConditional && _hideAfterConditional)
                gameObject.SetActive(false);

            LogVerbose("Dialogue ended, movement restored");
        }

        [Conditional("DIALOGUE_DEBUG")]
        private void LogVerbose(string msg)
        {
            Debug.Log($"[DialogueInteractable] {msg}");
        }

        #endregion

        #region Editor

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!_playerInRange || _isDialogueActive) return;

            var rect = new Rect(
                (Screen.width - PROMPT_WIDTH) * 0.5f,
                Screen.height * 0.7f,
                PROMPT_WIDTH,
                PROMPT_HEIGHT
            );

            GUI.Box(rect, PROMPT_TEXT);
        }
#endif

        #endregion
    }
}
