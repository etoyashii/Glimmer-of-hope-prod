using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Central singleton for the dialogue system. Its job: receive "start this dialogue", then
    /// advance nodes one by one, delegating the actual work to the support classes
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class DialogueManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Fixed UI")]
        [Tooltip("Prefab for the fixed interaction panel (continue + choices). Must implement IDialogueInteractionUI.")]
        [FormerlySerializedAs("interactionUIPrefab")]
        [SerializeField] private GameObject _interactionUIPrefab;
        [Header("Proximity Check")]
        [Tooltip("Max distance the player can walk from the dialogue's start position before it auto-cancels. 0 disables it.")]
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
        public event Action<DialogueNodeBase> OnNodePlayed;
        public event Action<string> OnGateWaitingForEvent;
        public event Action<DialogueGraph, string> OnDialogueEndedWithOutcome;
        #endregion

        #region Private Fields
        private DialogueGraph _currentGraph;
        private DialogueNodeBase _currentNode;
        private DialogueBubblePresenter _bubble;
        private DialogueInteractionPresenter _interaction;
        private DialogueGateController _gate;
        private DialogueNodePresenter _nodePresenter;
        private DialogueProximityGuard _proximityGuard;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
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
        //Main entry point: starts a dialogue from any script.
        public void StartDialogue(DialogueGraph graph)
        {
            if (graph == null)
            {
                Debug.LogWarning("[DialogueManager] StartDialogue called with a null graph.");
                return;
            }
            if (IsPlaying)
            {
                Debug.LogWarning("[DialogueManager] A dialogue is already playing, ignoring this request.");
                return;
            }

            var firstNode = graph.GetFirstTypedDialogueNode();
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

        /// <summary>Call from any script (or via DialogueGateEventRelay) to unlock the Gate node currently waited on.</summary>
        public void NotifyGateEvent(string eventId) => _gate.TryUnlockScriptEvent(eventId);
        #endregion

        #region Private Methods
        /// <summary>
        /// Plays a node according to its runtime type. Silent nodes (Gate, If, Action) resolve
        /// immediately and chain on their own; only a DialogueLineNode shows something and waits.
        /// </summary>
        private void PlayNode(DialogueNodeBase node)
        {
            if (node == null || node.IsEnd)
            {
                EndDialogue((node as EndNode)?.outcomeId);
                return;
            }

            _currentNode = node;
            OnNodePlayed?.Invoke(node);

            switch (node)
            {
                case GateNode gateNode:
                    _interaction.Hide();
                    _gate.BeginWait(gateNode);
                    break;
                case ConditionNode conditionNode:
                    bool isTrue = DialogueLogicEvaluator.EvaluateCondition(conditionNode);
                    GoToNode(conditionNode.GetNextNodeId(isTrue ? 0 : 1));
                    break;
                case ActionNode actionNode:
                    DialogueLogicEvaluator.ExecuteAction(actionNode);
                    GoToNode(actionNode.GetNextNodeId());
                    break;
                case DialogueLineNode lineNode:
                    _nodePresenter.Present(lineNode);
                    break;
            }
        }

        private void HandleContinue()
        {
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
            PlayNode(_currentGraph.GetTypedNode(nextNodeId));
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
