using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Central singleton for the dialogue system. It receive "start this dialogue", then
    /// the nodes one by one, delegating the actual work to the support classes
    /// </summary>
    [DefaultExecutionOrder(-100)] 
    public class DialogueManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Fixed UI")]
        [Tooltip("Prefab for the fixed interaction panel (continue + choices). Used when followSpeaker = true. Must implement IDialogueInteractionUI.")]
        [FormerlySerializedAs("interactionUIPrefab")]
        [SerializeField] private GameObject _interactionUIPrefab;

        [Header("Proximity Check")]
        [Tooltip("Max distance the player can walk away from where they were when the dialogue started, before it auto-cancels. Set to 0 to disable.")]
        [SerializeField] private float _maxDistanceFromStart = 3f;

        [Tooltip("Tag used to find the player GameObject.")]
        [SerializeField] private string _playerTag = "Player";
        #endregion

        #region Public Properties
        public static DialogueManager Instance { get; private set; }
        public bool IsPlaying => _currentGraph != null;
        #endregion

        #region Events
        public event Action<DialogueGraph> OnDialogueStarted;
        public event Action OnDialogueEnded;
        public event Action<DialogueNode> OnNodePlayed;

        /// <summary>Fired when a Gate node in ScriptEvent mode is reached and waiting on NotifyGateEvent(id).</summary>
        public event Action<string> OnGateWaitingForEvent;

        /// <summary>Fired at the end of every dialogue: the graph, and the reached End node's Outcome ID (empty if none/interrupted).</summary>
        public event Action<DialogueGraph, string> OnDialogueEndedWithOutcome;
        #endregion

        #region Private Fields
        private DialogueGraph _currentGraph;
        private DialogueNode _currentNode;

        private DialogueBubblePresenter _bubble;
        private DialogueInteractionPresenter _interaction;
        private DialogueGateController _gate;
        private DialogueNodePresenter _nodePresenter;
        private DialogueProximityGuard _proximityGuard;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Simple singleton: if another DialogueManager already exists, remove this one.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _bubble = new DialogueBubblePresenter();
            _bubble.SetCallbacks(HandleContinue, HandleChoiceSelected);

            _interaction = new DialogueInteractionPresenter(_interactionUIPrefab);
            _interaction.SetCallbacks(HandleContinue, HandleChoiceSelected);

            _gate = new DialogueGateController(this, GoToNode, eventId => OnGateWaitingForEvent?.Invoke(eventId));
            _nodePresenter = new DialogueNodePresenter(_bubble, _interaction);
            _proximityGuard = new DialogueProximityGuard(_playerTag, _maxDistanceFromStart);
        }

        private void Update()
        {
            _gate.Tick();

            if (_proximityGuard.HasPlayerMovedTooFar())
                EndDialogue("player_left_range");
        }
        #endregion

        #region Public Methods
        /// <summary>Main entry point: starts a dialogue from any script.</summary>
        public void StartDialogue(DialogueGraph graph)
        {
            if (graph == null)
            {
                Debug.LogWarning("[DialogueManager] StartDialogue called with a null graph.");
                return;
            }

            if (IsPlaying)
            {
                Debug.LogWarning("[DialogueManager] A dialogue is already playing, interrupting it.");
                return;
            }

            var firstNode = graph.GetFirstDialogueNode();
            if (firstNode == null)
            {
                Debug.LogWarning($"[DialogueManager] Graph '{graph.name}' has no valid first node (Start not connected?).");
                return;
            }

            _currentGraph = graph;
            _proximityGuard.BeginTracking();
            OnDialogueStarted?.Invoke(graph);
            PlayNode(firstNode);
        }

        /// <summary>Call from any script (or via DialogueGateEventRelay) to unlock the Gate node currently being waited on.</summary>
        public void NotifyGateEvent(string eventId) => _gate.TryUnlockScriptEvent(eventId);
        #endregion

        #region Private Methods

        //Plays a node according to its type. nodes (Gate, If, Action) resolve and chain on their own, only the Dialogues nodes shows something

        private void PlayNode(DialogueNode node)
        {
            Debug.Log($"[DEBUG] PlayNode called with: {(node == null ? "NULL" : $"type={node.nodeType}, id={node.nodeId}, outcomeId={node.outcomeId}")}");

            if (node == null || node.IsEnd)
            {
                EndDialogue(node?.outcomeId);
                return;
            }

            _currentNode = node;
            OnNodePlayed?.Invoke(node);

            if (node.IsGate)
            {
                _interaction.Hide(); // nothing to pick while waiting
                _gate.BeginWait(node);
                return;
            }

            if (node.IsCondition)
            {
                bool conditionIsTrue = DialogueLogicEvaluator.EvaluateCondition(node);
                GoToNode(node.GetNextNodeId(conditionIsTrue ? 0 : 1)); // choices[0] = True, choices[1] = False
                return;
            }

            if (node.IsAction)
            {
                DialogueLogicEvaluator.ExecuteAction(node);
                GoToNode(node.GetNextNodeId());
                return;
            }

            _nodePresenter.Present(node);
        }

        private void HandleContinue()
        {
            // If the text is still typing out, a click completes it instead of jumping to the next node.
            if (_bubble.IsRevealingText)
            {
                _bubble.CompleteTextReveal();
                return;
            }

            if (_currentNode == null) return;
            GoToNode(_currentNode.GetNextNodeId());
        }

        private void HandleChoiceSelected(int choiceIndex)
        {
            if (_currentNode == null) return;
            GoToNode(_currentNode.GetNextNodeId(choiceIndex));
        }

        private void GoToNode(string nextNodeId)
        {
            var next = _currentGraph.GetNode(nextNodeId);
            PlayNode(next); // PlayNode already handles next == null / IsEnd -> EndDialogue()
        }

        private void EndDialogue(string outcomeId = null)
        {
            var endedGraph = _currentGraph;

            _proximityGuard.StopTracking();
            _gate.CancelWait();
            _bubble.Cleanup();
            _interaction.Cleanup();

            _currentGraph = null;
            _currentNode = null;

            OnDialogueEnded?.Invoke();
            OnDialogueEndedWithOutcome?.Invoke(endedGraph, outcomeId ?? "");
        }
        #endregion
    }
}
