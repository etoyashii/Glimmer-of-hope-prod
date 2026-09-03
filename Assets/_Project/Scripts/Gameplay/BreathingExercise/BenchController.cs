using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using GlimmerOfHope.Gameplay.Interaction;

namespace GlimmerOfHope.Gameplay
{
    public class BenchController : MonoBehaviour
    {
        #region Public Fields
        [Header("Interactable (this bench)")]
        [Tooltip("Reference to this bench's Interactable component, used to hide its prompt during the exercise.")]
        public Interactable BenchInteractable;

        [Header("Camera")]
        public BenchCameraController CameraController;

        [Header("Player positioning")]
        [Tooltip("Empty transform placed on the seat, oriented with the player's back to the bench camera. Position and rotation are copied onto the player when they sit down.")]
        public Transform PlayerSeatPosition;
        [Tooltip("Reference to the player's transform. If left empty, the script looks for a GameObject tagged 'Player' at runtime.")]
        public Transform PlayerTransform;
        private Transform _player;
        private Rigidbody _playerRigidbody;
        private bool _playerRigidbodyWasKinematic;

        [Header("Breathing")]
        public GameObject BreathingCanvasPrefab;
        private GameObject _breathingInstance;
        private BreathingExercise _breathingExercise;

        [Header("End of exercise behaviour")]
        [Tooltip("If true, the player automatically stands up as soon as the required number of breaths is reached. If false, they stay seated and must leave manually via the UI (exit button).")]
        public bool AutoStandUpOnComplete = true;

        [Header("Progression (on this bench)")]
        [Tooltip("Triggered every time the player finishes all breaths on this bench, no matter how many times.")]
        public UnityEvent OnBreathingSessionCompleted;

        [Tooltip("Milestones: add one entry per desired completion count (e.g. 1, 5, 10) with its own event.")]
        public List<CompletionMilestone> CompletionMilestones = new List<CompletionMilestone>();

        [Tooltip("Number of times the full exercise has been completed on this bench.")]
        public int TimesCompleted = 0;

        [Header("Sit / Stand")]
        [Tooltip("Triggered when the player sits down (e.g. disable movement input).")]
        public UnityEvent OnSatDown;
        [Tooltip("Triggered when the player stands up, regardless of the reason: exit button, automatic end of exercise, etc. (e.g. re-enable movement input).")]
        public UnityEvent OnStoodUp;
        #endregion

        #region Private Properties
        private bool _isSitting = false;
        #endregion

        #region Public Methods
        //Hook this up to Interactable.OnInteracted int Inspector
        public void OnBenchInteracted()
        {
            if (!_isSitting)
                SitDown();
        }

        #endregion

        #region Private Methods

        // Moves and rotates the player onto the bench's seat position, facing away from the bench camera (back to camera).
        private void PositionPlayerOnBench()
        {
            if (PlayerSeatPosition == null) return;

            _player = PlayerTransform;
            if (_player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    _player = playerObject.transform;
            }

            if (_player == null) return;

            // A CharacterController blocks direct transform changes while enabled.
            CharacterController controller = _player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;


            _playerRigidbody = _player.GetComponent<Rigidbody>();
            if (_playerRigidbody != null)
            {
                _playerRigidbodyWasKinematic = _playerRigidbody.isKinematic;
                _playerRigidbody.linearVelocity = Vector3.zero;
                _playerRigidbody.angularVelocity = Vector3.zero;
                _playerRigidbody.isKinematic = true;
            }

            _player.position = PlayerSeatPosition.position;

            // Only copy the Y rotation (yaw) so the player stays upright,
            // even if PlayerSeatPosition has any unwanted tilt on X/Z.
            _player.rotation = Quaternion.Euler(0f, PlayerSeatPosition.eulerAngles.y, 0f);

            if (controller != null)
                controller.enabled = true;
        }

        private void SitDown()
        {
            _isSitting = true;

            CameraController.ActivateBenchCamera();
            PositionPlayerOnBench();

            // Hide the bench's interaction prompt while seated.
            if (BenchInteractable != null)
                BenchInteractable.DisableInteraction();

            if (_breathingInstance == null && BreathingCanvasPrefab != null)
            {
                _breathingInstance = Instantiate(BreathingCanvasPrefab);
                _breathingExercise = _breathingInstance.GetComponentInChildren<BreathingExercise>(true);

                if (_breathingExercise != null)
                {
                    _breathingExercise.ActivateBreathingSystem();
                    _breathingExercise.OnExerciseComplete.AddListener(HandleExerciseComplete);
                    _breathingExercise.OnQuitRequested.AddListener(HandleQuitRequested);
                }
            }

            OnSatDown?.Invoke();
        }

        private void StandUp()
        {
            _isSitting = false;

            CameraController.DeactivateBenchCamera();

            if (_playerRigidbody != null)
            {
                _playerRigidbody.isKinematic = _playerRigidbodyWasKinematic;
                _playerRigidbody = null;
            }

            if (_breathingInstance != null)
            {
                if (_breathingExercise != null)
                {
                    _breathingExercise.OnExerciseComplete.RemoveListener(HandleExerciseComplete);
                    _breathingExercise.OnQuitRequested.RemoveListener(HandleQuitRequested);
                }

                Destroy(_breathingInstance);
                _breathingInstance = null;
                _breathingExercise = null;
            }

            // Show the bench's interaction prompt again.
            if (BenchInteractable != null)
                BenchInteractable.EnableInteraction();

            OnStoodUp?.Invoke();
        }

        //Called when BreathingExercise reaches the requested number of breaths
        private void HandleExerciseComplete()
        {
            TimesCompleted++;

            // General event, triggered on every complete session, regardless of count.
            OnBreathingSessionCompleted?.Invoke();

            // Specific milestones, triggered only when TimesCompleted matches exactly
            foreach (var milestone in CompletionMilestones)
            {
                if (milestone.RequiredCompletions == TimesCompleted)
                    milestone.OnReached?.Invoke();
            }

            if (AutoStandUpOnComplete)
                StandUp();

        }

        //Called when the player presses the exit button in the breathing UI
        private void HandleQuitRequested()
        {
            StandUp();
        }

        #endregion
    }
}