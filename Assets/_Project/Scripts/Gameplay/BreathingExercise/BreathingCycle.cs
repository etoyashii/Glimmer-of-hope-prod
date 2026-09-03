using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public enum BreathPhase
    {
        Inhale,          
        HoldAfterInhale, 
        Exhale,          
        HoldAfterExhale  
    }

    /// <summary>
    /// Pure logic of the breathing cycle , target, phases, holds, counting.
    /// </summary>
    [Serializable]
    public class BreathingCycle
    {
        #region Public Fields
        [Header("Scale settings")]
        public float ErrorMargin = 0.1f;
        public float ScaleMax = 1.5f;
        public float ScaleMin = 0.5f;
        public float PlayerScaleSpeed = 0.5f;
        public float OvershootBuffer = 0.3f; // must be > ErrorMargin for a miss to be possible

        [Header("Hold phases (optional)")]
        public bool EnableHoldAfterInhale = false;
        public float HoldAfterInhaleDuration = 2f;
        public bool EnableHoldAfterExhale = false;
        public float HoldAfterExhaleDuration = 2f;

        [Header("Number of breaths")]
        public int DesiredBreathCount = 5;

        public Vector3 CurrentScale { get; private set; }
        public Vector3 DesiredScale { get; private set; }
        public BreathPhase CurrentPhase { get; private set; }
        public float HoldTimer { get; private set; }
        public int BreathsCompleted { get; private set; }

        /// <summary>
        /// Estimated time remaining before reaching the current target's margin
        /// </summary>
        public float EstimatedTimeRemaining
        {
            get
            {
                if (PlayerScaleSpeed <= 0f) return 0f;

                float distance = Mathf.Abs(CurrentScale.x - DesiredScale.x) - ErrorMargin;
                distance = Mathf.Max(distance, 0f);

                return distance / PlayerScaleSpeed;
            }
        }

        public event Action OnSuccess;
        public event Action OnMiss;
        public event Action OnExerciseComplete;
        #endregion

        #region Private Properties
        private bool _targetIsMax;
        #endregion

        #region Public Methods
        //Resets the cycle to its starting state (call on activation)
        public void ResetCycle()
        {
            CurrentScale = Vector3.one * ScaleMin;
            _targetIsMax = true;
            CurrentPhase = BreathPhase.Inhale;
            DesiredScale = Vector3.one * ScaleMax;
            BreathsCompleted = 0;
            HoldTimer = 0f;
        }

        //Call every frame from Update(), playerInhaling = whether the input is currently held
        public void Tick(float deltaTime, bool playerInhaling)
        {
            switch (CurrentPhase)
            {
                case BreathPhase.Inhale:
                case BreathPhase.Exhale:
                    UpdatePlayerScale(deltaTime, playerInhaling);
                    Check();
                    break;

                case BreathPhase.HoldAfterInhale:
                case BreathPhase.HoldAfterExhale:
                    UpdateHoldPhase(deltaTime);
                    break;
            }
        }
        #endregion

        #region Private Methods
        private void UpdatePlayerScale(float deltaTime, bool playerInhaling)
        {
            float step = PlayerScaleSpeed * deltaTime;
            float value = CurrentScale.x;

            value += playerInhaling ? step : -step;
            value = Mathf.Clamp(value, ScaleMin - OvershootBuffer, ScaleMax + OvershootBuffer);

            CurrentScale = new Vector3(value, value, value);
        }

        private void Check()
        {
            float diff = CurrentScale.x - DesiredScale.x;

            // Success, the player reached the target within the error margin.
            if (Mathf.Abs(diff) <= ErrorMargin)
            {
                OnSuccess?.Invoke();
                OnPhaseReached(success: true);
                return;
            }

            // Miss, overshot the target beyond the margin.
            bool missed = (_targetIsMax && diff > ErrorMargin) ||
                          (!_targetIsMax && diff < -ErrorMargin);

            if (missed)
            {
                OnMiss?.Invoke();
                OnPhaseReached(success: false);
            }

            // Otherwise, still on the way to the target, nothing changes.
        }

        private void OnPhaseReached(bool success)
        {
            if (CurrentPhase == BreathPhase.Inhale)
            {
                if (success && EnableHoldAfterInhale)
                    EnterHold(BreathPhase.HoldAfterInhale, HoldAfterInhaleDuration);
                else
                    SwitchToExhale();
            }
            else if (CurrentPhase == BreathPhase.Exhale)
            {
                if (success && EnableHoldAfterExhale)
                    EnterHold(BreathPhase.HoldAfterExhale, HoldAfterExhaleDuration);
                else
                    SwitchToInhale(completingBreath: true);
            }
        }

        private void UpdateHoldPhase(float deltaTime)
        {
            HoldTimer -= deltaTime;

            if (HoldTimer <= 0f)
            {
                if (CurrentPhase == BreathPhase.HoldAfterInhale)
                    SwitchToExhale();
                else if (CurrentPhase == BreathPhase.HoldAfterExhale)
                    SwitchToInhale(completingBreath: true);
            }
        }

        private void EnterHold(BreathPhase holdPhase, float duration)
        {
            CurrentPhase = holdPhase;
            HoldTimer = duration;
        }

        private void SwitchToExhale()
        {
            CurrentPhase = BreathPhase.Exhale;
            _targetIsMax = false;
            DesiredScale = Vector3.one * ScaleMin;
        }

        private void SwitchToInhale(bool completingBreath)
        {
            CurrentPhase = BreathPhase.Inhale;
            _targetIsMax = true;
            DesiredScale = Vector3.one * ScaleMax;

            if (completingBreath)
            {
                BreathsCompleted++;

                if (BreathsCompleted >= DesiredBreathCount)
                {
                    OnExerciseComplete?.Invoke();
                }
            }
        }
        #endregion
    }
}