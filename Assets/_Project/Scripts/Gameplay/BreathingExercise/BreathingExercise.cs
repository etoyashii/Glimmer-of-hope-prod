using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using GlimmerOfHope.Gameplay.UI;
using DG.Tweening;
using System;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Component to attach to the breathing exercise's UI GameObject, Handles input and display (images, colors, text) 
    /// </summary>
    public class BreathingExercise : MonoBehaviour
    {
        #region Public Fields
        [Header("Breathing cycle")]
        public BreathingCycle Cycle = new BreathingCycle();

        [Header("UI References")]
        public Image CurrentScaleImage;
        public Image DesiredScaleImage;
        public Color DesiredScaleColor = Color.white;
        public Color CurrentScaleColor = Color.white;
        public Color SuccessColor = Color.green;
        public Color FailColor = Color.red;
        public Color DefaultColor = Color.white;
        public float ColorFlashDuration = 0.3f; // how long the success/fail color shows before returning to neutral

        [Header("Animation")]
        public float DesiredScaleAnimTime = 0.5f;
        public Ease DesiredScaleAnimEase = Ease.InCirc;

        [Header("Time text")]
        public TMP_Text TimeText;
        public string TimeTextFormat = "{0:F0}s";

        [Header("Phase text (optional)")]
        [Tooltip("Text showing the current phase. Add it yourself in the prefab and assign it here.")]
        public TMP_Text PhaseText;
        public string InhaleLabel = "Inhale";
        public string ExhaleLabel = "Exhale";
        public string HoldAfterInhaleLabel = "Hold";
        public string HoldAfterExhaleLabel = "Hold";

        [Header("Breath count text (optional)")]
        public BreathingPoints BreathingPoints = null;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnExerciseComplete;
        public UnityEngine.Events.UnityEvent OnQuitRequested;

        [Header("State")]
        public bool IsActive = false;

        public int BreathsCompleted => Cycle.BreathsCompleted;
        #endregion

        #region Private Properties
        private Coroutine _colorFlashCoroutine;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            Cycle.OnSuccess += HandleSuccess;
            Cycle.OnMiss += HandleMiss;
            Cycle.OnExerciseComplete += HandleExerciseComplete;
            Cycle.OnDesiredScaleChanged += OnDesiredScaleChanged;
        }

        void OnDestroy()
        {
            Cycle.OnSuccess -= HandleSuccess;
            Cycle.OnMiss -= HandleMiss;
            Cycle.OnExerciseComplete -= HandleExerciseComplete;
            Cycle.OnDesiredScaleChanged -= OnDesiredScaleChanged;
        }

        private void OnDesiredScaleChanged(Vector3 desiredScale)
        {
            if (DesiredScaleImage != null)
                DesiredScaleImage.transform.DOScale(desiredScale, DesiredScaleAnimTime).SetEase(DesiredScaleAnimEase);
        }

        void Start()
        {
            // The prefab is only instantiated when needed, so it's active right away.
            ActivateBreathingSystem();
            DesiredScaleImage.transform.localScale = Cycle.DesiredScale;
        }

        void Update()
        {
            if (!IsActive) return;

            Cycle.Tick(Time.deltaTime, ReadInhaleInput());

            ApplyVisuals();
            UpdatePhaseText();
            UpdateBreathCountText();
        }
        #endregion

        #region Public Methods

        //Hook this up directly to the OnClick() of a UI exit button.
        public void RequestQuit()
        {
            OnQuitRequested?.Invoke();
        }

        public bool ReadInhaleInput()
        {
            bool inhaling = false;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                inhaling = true;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                inhaling = true;

            return inhaling;
        }

        public void HandleSuccess()
        {
            FlashColor(SuccessColor);
        }

        public void HandleMiss()
        {
            FlashColor(FailColor);
        }

        public void HandleExerciseComplete()
        {
            OnExerciseComplete?.Invoke();
            DeactivateBreathingSystem();
        }

        // Briefly shows a color (success/fail) then returns to DefaultColor
        public void FlashColor(Color color)
        {
            if (_colorFlashCoroutine != null)
                StopCoroutine(_colorFlashCoroutine);

            _colorFlashCoroutine = StartCoroutine(ColorFlashRoutine(color));
        }

        public IEnumerator ColorFlashRoutine(Color color)
        {
            CurrentScaleColor = color;
            yield return new WaitForSeconds(ColorFlashDuration);
            CurrentScaleColor = DefaultColor;
        }

        public void ApplyVisuals()
        {
            if (CurrentScaleImage != null)
            {
                CurrentScaleImage.transform.localScale = Cycle.CurrentScale;
                CurrentScaleImage.color = CurrentScaleColor;
            }


        }

        public void UpdatePhaseText()
        {
            if (PhaseText == null) return;

            switch (Cycle.CurrentPhase)
            {
                case BreathPhase.Inhale:
                    PhaseText.text = $"{InhaleLabel}";
                    TimeText.text = string.Format(TimeTextFormat, Cycle.EstimatedTimeRemaining);
                    break;

                case BreathPhase.Exhale:
                    PhaseText.text = $"{ExhaleLabel}";
                    TimeText.text = string.Format(TimeTextFormat, Cycle.EstimatedTimeRemaining);
                    break;

                case BreathPhase.HoldAfterInhale:
                    PhaseText.text = $"{HoldAfterInhaleLabel}";
                    TimeText.text = string.Format(TimeTextFormat, Cycle.HoldTimer);
                    break;

                case BreathPhase.HoldAfterExhale:
                    PhaseText.text = $"{HoldAfterExhaleLabel}";
                    TimeText.text = string.Format(TimeTextFormat, Cycle.HoldTimer);
                    break;
            }
        }

        public void UpdateBreathCountText()
        {
            if (BreathingPoints == null) return;

            BreathingPoints.SetValue(Cycle.BreathsCompleted, true);
        }

        // Useful if you reuse the object without destroying/recreating it
        public void ActivateBreathingSystem()
        {
            IsActive = true;
            Cycle.ResetCycle();
            CurrentScaleColor = DefaultColor;
        }

        public void DeactivateBreathingSystem()
        {
            IsActive = false;
        }
        #endregion
    }
}