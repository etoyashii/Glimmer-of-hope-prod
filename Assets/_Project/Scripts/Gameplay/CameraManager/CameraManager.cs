using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Cinemachine.CinemachineImpulseDefinition;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Manages all Cinemachine cameras in the scene:
    /// switching with blend, on-the-fly configuration, mouse FreeCam, and impulse-based shake.
    /// Persistent singleton across scenes.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        #region Public Properties
        public static CameraManager Instance { get; private set; }

        /// All managed CinemachineCamera instances, assigned in the Inspector.
        public CinemachineCamera[] cam;

        [Tooltip("The scene's Main Camera (with CinemachineBrain)")]
        public Camera mainCamera;

        /// Impulse shape used for the shake.
        public enum CameraShapeType { Bump, Explosion, Recoil, Rumble }
        #endregion

        #region Private Fields
        private CinemachineBrain _brain;
        private Camera _mainCamera;

        /// Currently active Cinemachine camera.
        private CinemachineCamera _currentCam;

        // --- FreeCam ---
        private bool _isFreeCam = false;
        private float _freeCamYaw = 0f;        // horizontal rotation (degrees)
        private float _freeCamPitch = 0f;      // vertical rotation (degrees, clamped ±89°)
        private float _freeCamSensitivity = 2f;
        private Mouse _mouse;
        private float _freeCamDistance = 5f;   // orbital distance around the target
        private Transform _freeCamTarget;      // null = free rotation, otherwise orbit

        // --- Shake (Cinemachine Impulse) ---
        private CinemachineImpulseSource _impulseSource;
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            // Singleton: destroys any duplicate and survives scene changes
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // The Brain is required for blends and priorities
            _brain = FindFirstObjectByType<CinemachineBrain>();
            if (_brain == null)
                Debug.LogError("[CameraManager] No CinemachineBrain found!");

            // Fallback to Camera.main if nothing is assigned in the Inspector
            _mainCamera = mainCamera != null ? mainCamera : Camera.main;
            if (_mainCamera == null)
                Debug.LogError("[CameraManager] No Main Camera found! Assign it in the Inspector or tag it 'MainCamera'.");

            _mouse = Mouse.current;

            // Gets or dynamically adds the impulse source on this GameObject
            _impulseSource = GetComponent<CinemachineImpulseSource>();
            if (_impulseSource == null)
                _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        private void Update()
        {
            // FreeCam bypasses the Brain and drives the Main Camera directly
            if (!_isFreeCam || _mainCamera == null) return;

            // Late attempt to retrieve the mouse (hot-plug)
            if (_mouse == null)
            {
                _mouse = Mouse.current;
                if (_mouse == null) return;
            }

            var delta = _mouse.delta.ReadValue();
            _freeCamYaw += delta.x * _freeCamSensitivity * 0.1f;
            _freeCamPitch -= delta.y * _freeCamSensitivity * 0.1f;
            _freeCamPitch = Mathf.Clamp(_freeCamPitch, -89f, 89f); // avoids gimbal lock

            Quaternion rotation = Quaternion.Euler(_freeCamPitch, _freeCamYaw, 0f);

            if (_freeCamTarget != null)
            {
                // Orbit mode: rotates around the target at a fixed distance
                _mainCamera.transform.position = _freeCamTarget.position + rotation * new Vector3(0f, 0f, -_freeCamDistance);
                _mainCamera.transform.rotation = rotation;
            }
            else
            {
                // Free mode: rotation in place without movement
                _mainCamera.transform.rotation = rotation;
            }
        }
        #endregion

        #region public Methods

        /// <summary>
        /// Switches to the camera with the given name, with optional blend.
        /// </summary>
        public void SwitchCamera(
            string cameraName,
            float blendTime = 0.5f,
            CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut)
        {
            var target = System.Array.Find(cam, c => c.name == cameraName);
            if (target == null) { Debug.LogWarning($"[CameraManager] Camera not found: {cameraName}"); return; }
            SwitchCamera(target, blendTime, blendStyle);
        }

        /// <summary>
        /// Switches to the given camera by direct reference, with optional blend.
        /// Priority is incremented so it takes precedence over the others.
        /// </summary>
        public void SwitchCamera(
            CinemachineCamera targetCamera,
            float blendTime = 0.5f,
            CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut)
        {
            if (targetCamera == null || _brain == null)
            {
                Debug.LogWarning("[CameraManager] Target camera or Brain is null!");
                return;
            }

            // Applies the desired blend style to the Brain before switching
            _brain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, blendTime);

            // Gives the target camera the highest priority so it becomes active
            int highestPriority = 0;
            foreach (var c in cam)
                if (c.Priority > highestPriority)
                    highestPriority = c.Priority;

            targetCamera.Priority = highestPriority + 1;
            _currentCam = targetCamera;
        }

        /// <summary>
        /// Enables or disables FreeCam.
        /// In orbit mode (target != null), the camera rotates around the target.
        /// In free mode (target == null), it rotates in place.
        /// Disables the Brain during FreeCam to take full control.
        /// </summary>
        public void SetFreeCam(bool enabled, float sensitivity = 2f, Transform target = null, float distance = 5f)
        {
            _freeCamSensitivity = sensitivity;
            _freeCamDistance = distance;
            _freeCamTarget = target;

            if (enabled)
            {
                if (_mainCamera == null) { Debug.LogError("[CameraManager] Main Camera not found!"); return; }

                _brain.enabled = false; // disables Cinemachine to manually drive the camera
                _isFreeCam = true;

                // Initializes yaw/pitch from the current position to avoid a sudden jump
                if (_freeCamTarget != null)
                {
                    Vector3 dir = _mainCamera.transform.position - _freeCamTarget.position;
                    _freeCamDistance = dir.magnitude;
                    Vector3 angles = Quaternion.LookRotation(-dir.normalized).eulerAngles;
                    _freeCamYaw = angles.y;
                    _freeCamPitch = angles.x > 180f ? angles.x - 360f : angles.x; // normalizes to [-180, 180]
                }
                else
                {
                    Vector3 angles = _mainCamera.transform.eulerAngles;
                    _freeCamYaw = angles.y;
                    _freeCamPitch = angles.x > 180f ? angles.x - 360f : angles.x;
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                Debug.Log($"[CameraManager] FreeCam ON | target={(_freeCamTarget != null ? _freeCamTarget.name : "none")} | dist={_freeCamDistance:F1}");
            }
            else
            {
                _isFreeCam = false;
                _brain.enabled = true; // hands control back to Cinemachine

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                Debug.Log("[CameraManager] FreeCam OFF");
            }
        }

        /// <summary>
        /// Applies a "CameraSettings" to the active camera (follow, FOV, damping).
        /// </summary>
        public void ConfigureCamera(CameraSettings settings)
        {
            if (_currentCam == null) { Debug.LogWarning("[CameraManager] No active camera!"); return; }
            if (settings == null) { Debug.LogWarning("[CameraManager] CameraSettings is null!"); return; }

            ApplyFollow(_currentCam, settings);
            ApplyFOV(_currentCam, settings);
            ApplyDamping(_currentCam, settings);
        }

        /// <summary>Shortcut overload to assign follow + offset + lookAt.</summary>
        public void ConfigureCamera(Transform follow, Vector3 offset, Transform lookAt = null)
        {
            ConfigureCamera(new CameraSettings
            {
                Follow = follow,
                FollowOffset = offset,
                LookAt = lookAt != null ? lookAt : follow
            });
        }

        /// <summary>
        /// Triggers a camera shake via Cinemachine Impulse.
        /// The impact direction is random on the XY plane.
        /// </summary>
        /// <param name="duration">Total shake duration in seconds.</param>
        /// <param name="amplitude">Impact force (velocity scale).</param>
        /// <param name="frequency">Oscillation frequency.</param>
        /// <param name="shape">Shape of the impulse curve.</param>
        public void ShakeCamera(float duration, float amplitude = 1f, float frequency = 1f, CameraShapeType shape = CameraShapeType.Bump)
        {
            if (_impulseSource == null) return;

            // Normalized random direction on the XY plane to vary the impact
            _impulseSource.DefaultVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized * amplitude;

            // Total duration + envelope: immediate attack, long sustain, short decay
            _impulseSource.ImpulseDefinition.ImpulseDuration = duration;
            var envelope = _impulseSource.ImpulseDefinition.TimeEnvelope;
            envelope.AttackTime = 0f;
            envelope.SustainTime = duration * 0.7f;
            envelope.DecayTime = duration * 0.3f;
            _impulseSource.ImpulseDefinition.TimeEnvelope = envelope;
            _impulseSource.ImpulseDefinition.FrequencyGain = frequency;

            // Maps the custom enum to the internal Cinemachine enum
            _impulseSource.ImpulseDefinition.ImpulseShape = shape switch
            {
                CameraShapeType.Bump => ImpulseShapes.Bump,
                CameraShapeType.Explosion => ImpulseShapes.Explosion,
                CameraShapeType.Recoil => ImpulseShapes.Recoil,
                CameraShapeType.Rumble => ImpulseShapes.Rumble,
                _ => ImpulseShapes.Bump,
            };

            _impulseSource.GenerateImpulseWithVelocity(_impulseSource.DefaultVelocity);

            Debug.Log($"[CameraManager] Shake | shape={shape} | amplitude={amplitude} | duration={duration} | frequency={frequency}");
        }

        /// Immediately stops all ongoing shakes.</summary>
        public void StopShake()
        {
            if (CinemachineImpulseManager.Instance != null)
                CinemachineImpulseManager.Instance.Clear();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Assigns follow/lookAt and applies the offset to the detected position component.
        /// Supports CinemachineFollow, OrbitalFollow, and PositionComposer.
        /// </summary>
        private void ApplyFollow(CinemachineCamera camera, CameraSettings s)
        {
            if (s.Follow != null)
            {
                camera.Follow = s.Follow;
                camera.LookAt = s.LookAt != null ? s.LookAt : s.Follow; // fallback: looks at the follow target
            }

            if (s.FollowOffset == null) return;
            Vector3 offset = s.FollowOffset.Value;

            // Applies the offset to the first position component found (priority order)
            var f = camera.GetComponent<CinemachineFollow>();
            if (f != null) { f.FollowOffset = offset; return; }

            var o = camera.GetComponent<CinemachineOrbitalFollow>();
            if (o != null) { o.TargetOffset = offset; return; }

            var p = camera.GetComponent<CinemachinePositionComposer>();
            if (p != null) { p.TargetOffset = offset; return; }

            Debug.LogWarning($"[CameraManager] No position component found on '{camera.name}'.");
        }

        /// <summary>Applies the FOV if set in CameraSettings.</summary>
        private void ApplyFOV(CinemachineCamera camera, CameraSettings s)
        {
            if (s.FOV == null) return;
            camera.Lens.FieldOfView = s.FOV.Value;
        }

        /// <summary>
        /// Applies position damping (Follow / OrbitalFollow)
        /// and rotation damping (RotationComposer) if set in CameraSettings.
        /// </summary>
        private void ApplyDamping(CinemachineCamera camera, CameraSettings s)
        {
            if (s.PositionDamping.HasValue)
            {
                Vector3 d = s.PositionDamping.Value;
                var f = camera.GetComponent<CinemachineFollow>();
                if (f != null) { f.TrackerSettings.PositionDamping = d; }
                else
                {
                    var o = camera.GetComponent<CinemachineOrbitalFollow>();
                    if (o != null) o.TrackerSettings.PositionDamping = d;
                }
            }

            if (s.RotationDamping.HasValue)
            {
                float d = s.RotationDamping.Value;
                var rc = camera.GetComponent<CinemachineRotationComposer>();
                if (rc != null) { rc.Damping = new Vector2(d, d); return; }

                // HardLookAt does not support rotation damping
                var hl = camera.GetComponent<CinemachineHardLookAt>();
                if (hl != null)
                    Debug.LogWarning("[CameraManager] CinemachineHardLookAt has no rotation damping.");
            }
        }
        #endregion
    }
}