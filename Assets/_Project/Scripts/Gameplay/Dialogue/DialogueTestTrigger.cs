using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using GlimmerOfHope.Core.Services;
using Debug = UnityEngine.Debug;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    /// <summary>
    /// Test trigger for dialogue system. Enable DIALOGUE_DEBUG symbol for verbose logs.
    /// </summary>
    public class DialogueTestTrigger : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private ConversationSO _testConversation;
        [SerializeField] private Key _triggerKey = Key.T;

        #endregion

        #region Private Fields

        private DialogueRunner _runner;
        private bool _keyboardChecked;

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

            CheckKeyboardOnce();

            if (Keyboard.current == null) return;

            if (Keyboard.current.tKey.wasPressedThisFrame)
                StartDialogue();
        }

        #endregion

        #region Private Methods

        private void CheckKeyboardOnce()
        {
            if (_keyboardChecked) return;
            _keyboardChecked = true;

            if (Keyboard.current == null)
                Debug.LogError("[DialogueTest] Keyboard.current is NULL - Input System issue");
        }

        private void TryGetRunner()
        {
            if (ServiceLocator.TryGet(out _runner))
                LogVerbose("DialogueRunner found");
        }

        private void StartDialogue()
        {
            LogVerbose("StartDialogue called");

            if (_runner == null)
            {
                TryGetRunner();
                if (_runner == null)
                {
                    Debug.LogError("[DialogueTest] DialogueRunner not found in ServiceLocator");
                    return;
                }
            }

            if (_testConversation == null)
            {
                Debug.LogError("[DialogueTest] No conversation assigned");
                return;
            }

            if (_runner.IsPlaying)
            {
                LogVerbose("Already playing, ignoring");
                return;
            }

            LogVerbose($"Starting: {_testConversation.ConversationId}");
            _runner.StartConversation(_testConversation);
        }

        [Conditional("DIALOGUE_DEBUG")]
        private void LogVerbose(string msg)
        {
            Debug.Log($"[DialogueTest] {msg}");
        }

        #endregion

        #region Debug GUI

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 200, 50), "START DIALOGUE (T)"))
                StartDialogue();

            string status = _runner != null ? "Runner: OK" : "Runner: NULL";
            string conv = _testConversation != null ? $"Conv: {_testConversation.ConversationId}" : "Conv: NULL";
            GUI.Label(new Rect(10, 70, 300, 20), $"{status} | {conv}");
        }
#endif

        #endregion
    }
}
