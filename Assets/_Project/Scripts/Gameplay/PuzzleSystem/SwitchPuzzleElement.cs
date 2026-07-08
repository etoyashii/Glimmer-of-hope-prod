using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay.Puzzles
{
    /// <summary>
    /// A puzzle element representing a switch, lever, pressure plate, or any boolean-state trigger.
    /// Solved when the switch is in its required activation state.
    /// </summary>
    public class SwitchPuzzleElement : PuzzleElement
    {
        #region Serialized Fields

        [Header("Switch Element")]
        [Tooltip("If true, the switch must be ON to be considered solved. If false, it must be OFF.")]
        [SerializeField] private bool _solvedWhenOn = true;

        [Header("Events")]
        [Tooltip("Fired when the switch is turned on.")]
        public UnityEvent OnSwitchOn;

        [Tooltip("Fired when the switch is turned off.")]
        public UnityEvent OnSwitchOff;

        #endregion

        #region Public Properties

        public bool IsOn { get; private set; }

        #endregion

        #region PuzzleElement Implementation

        public override void CheckSolvedState()
        {
            SetSolved(IsOn == _solvedWhenOn);
        }

        protected override void OnReset()
        {
            // Switches start in off state
            SetOn(false, silent: true);
        }

        #endregion

        #region Public Methods

        /// <summary>Turns the switch on.</summary>
        public void Activate()
        {
            SetOn(true);
        }

        /// <summary>Turns the switch off.</summary>
        public void Deactivate()
        {
            SetOn(false);
        }

        /// <summary>Toggles the switch between on and off.</summary>
        public void Toggle()
        {
            SetOn(!IsOn);
        }

        #endregion

        #region Private Methods

        private void SetOn(bool value, bool silent = false)
        {
            if (IsOn == value) return;

            IsOn = value;

            if (!silent)
            {
                if (IsOn) OnSwitchOn?.Invoke();
                else OnSwitchOff?.Invoke();
            }

            CheckSolvedState();
        }

        #endregion
    }
}