using System;
using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Handles waiting on a GateNode, whatever its mode (Timer, Flag, ScriptEvent).
    /// </summary>
    public class DialogueGateController
    {
        #region Private Fields
        private readonly MonoBehaviour _coroutineRunner;
        private readonly Action<string> _onAdvance; // receives the nextNodeId to play once unlocked
        private readonly Action<string> _onWaitingForScriptEvent;

        private Coroutine _timerCoroutine;
        private GateNode _pendingFlagNode;
        private GateNode _pendingScriptEventNode;
        #endregion

        #region Public Methods
        public DialogueGateController(MonoBehaviour coroutineRunner, Action<string> onAdvance, Action<string> onWaitingForScriptEvent)
        {
            _coroutineRunner = coroutineRunner;
            _onAdvance = onAdvance;
            _onWaitingForScriptEvent = onWaitingForScriptEvent;
        }

        public void BeginWait(GateNode node)
        {
            switch (node.gateTriggerType)
            {
                case DialogueGateTriggerType.ScriptEvent:
                    _pendingScriptEventNode = node;
                    _onWaitingForScriptEvent?.Invoke(node.gateEventId);
                    break;

                case DialogueGateTriggerType.Timer:
                    if (_timerCoroutine != null) _coroutineRunner.StopCoroutine(_timerCoroutine);
                    _timerCoroutine = _coroutineRunner.StartCoroutine(TimerRoutine(node));
                    break;

                case DialogueGateTriggerType.Flag:
                    _pendingFlagNode = node;
                    break;
            }
        }

        //Call every frame from DialogueManager,Update (only the Flag mode needs it)
        public void Tick()
        {
            if (_pendingFlagNode == null) return;

            bool flagMatches = DialogueFlags.Get(_pendingFlagNode.gateFlagName) == _pendingFlagNode.gateFlagExpectedValue;
            if (!flagMatches) return;

            var node = _pendingFlagNode;
            _pendingFlagNode = null;
            _onAdvance?.Invoke(node.GetNextNodeId());
        }

        //Call from DialogueManager, Returns true if it matched a wait in progress
        public bool TryUnlockScriptEvent(string eventId)
        {
            if (_pendingScriptEventNode == null) return false;
            if (_pendingScriptEventNode.gateEventId != eventId) return false;

            var node = _pendingScriptEventNode;
            _pendingScriptEventNode = null;
            _onAdvance?.Invoke(node.GetNextNodeId());
            return true;
        }

        //Cancels any wait in progress, typically at the end of a dialogue
        public void CancelWait()
        {
            if (_timerCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
            _pendingFlagNode = null;
            _pendingScriptEventNode = null;
        }
        #endregion

        #region Private Methods
        private IEnumerator TimerRoutine(GateNode node)
        {
            yield return new WaitForSeconds(node.gateTimerSeconds);
            _timerCoroutine = null;
            _onAdvance?.Invoke(node.GetNextNodeId());
        }
        #endregion
    }
}
