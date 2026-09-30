using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

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


        [Header("Phase text (optional)")]
        [Tooltip("Text showing the current phase. Add it yourself in the prefab and assign it here.")]
        public TMP_Text PhaseText;
        public string InhaleLabel = "Inhale";
        public string ExhaleLabel = "Exhale";
        public string HoldAfterInhaleLabel = "Hold";
        public string HoldAfterExhaleLabel = "Hold";

        [Header("Breath count text (optional)")]
        [Tooltip("Text showing the number of breaths completed. Add it yourself in the prefab and assign it here.")]
        public TMP_Text BreathCountText;
        public string BreathCountFormat = "{0} / {1}"; // {0} = BreathsCompleted, {1} = DesiredBreathCount

        [Header("UI exit (optional)")]
        [Tooltip("Text showing the instruction to quit the exercise. Add it yourself in the prefab and assign it here.")]
        public TMP_Text QuitPromptText;
        public string QuitPromptLabel = "Quit";

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
        }

        void OnDestroy()
        {
            Cycle.OnSuccess -= HandleSuccess;
            Cycle.OnMiss -= HandleMiss;
            Cycle.OnExerciseComplete -= HandleExerciseComplete;
        }

        void Start()
        {
            // The prefab is only instantiated when needed, so it's active right away.
            ActivateBreathingSystem();
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

        public  void HandleSuccess()
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

            if (DesiredScaleImage != null)
            {
                DesiredScaleImage.transform.localScale = Cycle.DesiredScale;
                DesiredScaleImage.color = DesiredScaleColor;
            }
        }

        public void UpdatePhaseText()
        {
            if (PhaseText == null) return;

            switch (Cycle.CurrentPhase)
            {
                case BreathPhase.Inhale:
                    PhaseText.text = $"{InhaleLabel} {Cycle.EstimatedTimeRemaining:F1}s";
                    break;

                case BreathPhase.Exhale:
                    PhaseText.text = $"{ExhaleLabel} {Cycle.EstimatedTimeRemaining:F1}s";
                    break;

                case BreathPhase.HoldAfterInhale:
                    PhaseText.text = $"{HoldAfterInhaleLabel} {Cycle.HoldTimer:F1}s";
                    break;

                case BreathPhase.HoldAfterExhale:
                    PhaseText.text = $"{HoldAfterExhaleLabel} {Cycle.HoldTimer:F1}s";
                    break;
            }
        }

        public void UpdateBreathCountText()
        {
            if (BreathCountText == null) return;

            BreathCountText.text = string.Format(BreathCountFormat, Cycle.BreathsCompleted, Cycle.DesiredBreathCount);
        }

        // Useful if you reuse the object without destroying/recreating it
        public void ActivateBreathingSystem()
        {
            IsActive = true;
            Cycle.ResetCycle();
            CurrentScaleColor = DefaultColor;

            if (QuitPromptText != null)
                QuitPromptText.text = QuitPromptLabel;
        }

        public void DeactivateBreathingSystem()
        {
            IsActive = false;
        }
        #endregion
    }
}