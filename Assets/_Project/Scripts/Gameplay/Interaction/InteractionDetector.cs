using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay.Interaction
{
    /// <summary>
    /// Detects the closest to center, in range Interactable and lets the
    /// player trigger it. On Mobile the trigger is a direct tap on the
    /// prompt UI button. On KeyboardMouse and Gamepad the trigger is the
    /// Interact InputAction, masked per scheme the same way as Jump.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("Origin used for the distance check, usually the player root.")]
        [SerializeField] private Transform _originTransform;

        [SerializeField] private Camera _playerCamera;

        [SerializeField] private InteractionPromptUI _promptUI;

        [Header("Detection Settings")]
        [SerializeField] private float _detectionRadius = 3f;

        [SerializeField] private LayerMask _interactableLayer;

        [Range(0f, 0.5f)]
        [Tooltip("Maximum distance from the viewport center, 0 is dead center, 0.5 is screen edge.")]
        [SerializeField] private float _centeredViewportRadius = 0.15f;

        [Tooltip("Time in seconds between two detection checks, used to reduce cost.")]
        [SerializeField] private float _checkInterval = 0.1f;

        [Header("Obstruction Check")]
        [SerializeField] private bool _useObstructionCheck = true;
        [SerializeField] private LayerMask _obstructionMask;

        [Header("Input")]
        [Tooltip("Interact action")]
        [SerializeField] private InputActionReference _interactAction;

        #endregion

        #region Private Fields

        private readonly Collider[] _candidatesBuffer = new Collider[16];
        private Interactable _currentInteractable;
        private float _checkTimer;

        #endregion

        #region Public Properties

        public Interactable CurrentInteractable => _currentInteractable;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_interactAction != null)
            {
                _interactAction.action.Enable();
                _interactAction.action.performed += OnInteractActionPerformed;
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnSchemeChanged.AddListener(OnSchemeChanged);
                ApplyBindingMask(InputManager.Instance.CurrentScheme);
            }
        }

        private void OnDisable()
        {
            if (_interactAction != null)
            {
                _interactAction.action.performed -= OnInteractActionPerformed;
                _interactAction.action.Disable();
            }

            if (InputManager.Instance != null)
                InputManager.Instance.OnSchemeChanged.RemoveListener(OnSchemeChanged);

            SetCurrentInteractable(null);
        }

        private void Update()
        {
            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f) return;

            _checkTimer = _checkInterval;
            SetCurrentInteractable(FindBestInteractable());
        }

        #endregion

        #region Private Methods - Detection

        private Interactable FindBestInteractable()
        {
            int count = Physics.OverlapSphereNonAlloc(
                _originTransform.position,
                _detectionRadius,
                _candidatesBuffer,
                _interactableLayer
            );

            Interactable best = null;
            float bestViewportDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Interactable candidate = _candidatesBuffer[i].GetComponentInParent<Interactable>();
                if (candidate == null || !candidate.IsInteractable) continue;

                Vector3 viewportPoint = _playerCamera.WorldToViewportPoint(candidate.transform.position);
                if (viewportPoint.z <= 0f) continue;

                float viewportDistance = Vector2.Distance(
                    new Vector2(viewportPoint.x, viewportPoint.y),
                    new Vector2(0.5f, 0.5f)
                );

                if (viewportDistance > _centeredViewportRadius) continue;
                if (viewportDistance >= bestViewportDistance) continue;

                if (_useObstructionCheck && IsObstructed(candidate)) continue;

                best = candidate;
                bestViewportDistance = viewportDistance;
            }

            return best;
        }

        private bool IsObstructed(Interactable candidate)
        {
            Vector3 origin = _playerCamera.transform.position;
            Vector3 target = candidate.transform.position;

            if (!Physics.Linecast(origin, target, out RaycastHit hit, _obstructionMask))
                return false;

            return hit.collider.GetComponentInParent<Interactable>() != candidate;
        }

        private void SetCurrentInteractable(Interactable newInteractable)
        {
            if (newInteractable == _currentInteractable) return;

            if (_currentInteractable != null)
                _currentInteractable.SetFocused(false);

            _currentInteractable = newInteractable;

            if (_currentInteractable != null)
            {
                _currentInteractable.SetFocused(true);
                _promptUI.Show(_currentInteractable, TriggerInteract);
            }
            else
            {
                _promptUI.Hide();
            }
        }

        #endregion

        #region Private Methods - Input

        private void OnInteractActionPerformed(InputAction.CallbackContext context)
        {
            TriggerInteract();
        }

        private void TriggerInteract()
        {
            _currentInteractable?.Interact();
        }

        private void OnSchemeChanged(InputManager.ControlScheme scheme)
        {
            ApplyBindingMask(scheme);
        }

        /// <summary>
        /// On mobile the action is disabled, the tap goes through the prompt UI button.
        /// On other schemes only the relevant bindings are active, same pattern as Jump.
        /// </summary>
        private void ApplyBindingMask(InputManager.ControlScheme scheme)
        {
            if (_interactAction == null) return;

            _interactAction.action.bindingMask = scheme switch
            {
                InputManager.ControlScheme.Mobile => null,
                InputManager.ControlScheme.KeyboardMouse => InputBinding.MaskByGroup("Keyboard/Mouse"),
                InputManager.ControlScheme.Gamepad => InputBinding.MaskByGroup("Gamepad"),
                _ => null
            };

            if (scheme == InputManager.ControlScheme.Mobile)
                _interactAction.action.Disable();
            else
                _interactAction.action.Enable();
        }

        #endregion
    }
}