using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace GlimmerOfHope.Gameplay
{
    public class CameraManager : MonoBehaviour
    {
        #region public properties
        public static CameraManager Instance { get; private set; }
        public CinemachineCamera[] cam;
        [Tooltip("La Main Camera de la scène (avec CinemachineBrain)")]
        public Camera mainCamera;
        #endregion

        #region private properties
        private CinemachineBrain _brain;
        private Camera _mainCamera;

        private CinemachineCamera _currentCam;

        // --- FreeCam ---
        private bool _isFreeCam = false;
        private float _freeCamYaw = 0f;
        private float _freeCamPitch = 0f;
        private float _freeCamSensitivity = 2f;
        private Mouse _mouse;
        private float _freeCamDistance = 5f;
        private Transform _freeCamTarget;

        // --- Shake System (Cinemachine Impulse) ---
        private CinemachineImpulseSource _impulseSource;
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _brain = FindFirstObjectByType<CinemachineBrain>();
            if (_brain == null)
                Debug.LogError("[CameraManager] Aucun CinemachineBrain trouvé !");

            _mainCamera = mainCamera != null ? mainCamera : Camera.main;
            if (_mainCamera == null)
                Debug.LogError("[CameraManager] Aucune Main Camera trouvée ! Assigne-la dans l'Inspector ou tague-la 'MainCamera'.");

            _mouse = Mouse.current;

            // Récupère ou ajoute dynamiquement la source d'impulsion sur le Manager
            _impulseSource = GetComponent<CinemachineImpulseSource>();
            if (_impulseSource == null)
            {
                _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            }
        }
        private void Update()
        {

            if (!_isFreeCam || _mainCamera == null) return;

            if (_mouse == null)
            {
                _mouse = Mouse.current;
                if (_mouse == null) return;
            }

            var delta = _mouse.delta.ReadValue();
            float mouseX = delta.x * _freeCamSensitivity * 0.1f;
            float mouseY = delta.y * _freeCamSensitivity * 0.1f;

            _freeCamYaw += mouseX;
            _freeCamPitch -= mouseY;
            _freeCamPitch = Mathf.Clamp(_freeCamPitch, -89f, 89f);

            if (_freeCamTarget != null)
            {
                Quaternion rotation = Quaternion.Euler(_freeCamPitch, _freeCamYaw, 0f);
                Vector3 offset = rotation * new Vector3(0f, 0f, -_freeCamDistance);
                _mainCamera.transform.position = _freeCamTarget.position + offset;
                _mainCamera.transform.rotation = rotation;
            }
            else
            {
                _mainCamera.transform.rotation = Quaternion.Euler(_freeCamPitch, _freeCamYaw, 0f);
            }
        }
        #endregion

        #region public Methods
        public void SwitchCamera(
            string cameraName,
            float blendTime = 0.5f,
            CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut)
        {
            var target = System.Array.Find(cam, c => c.name == cameraName);
            if (target == null) { Debug.LogWarning($"[CameraManager] Caméra non trouvée : {cameraName}"); return; }
            SwitchCamera(target, blendTime, blendStyle);
        }
        public void SwitchCamera(
            CinemachineCamera targetCamera,
            float blendTime = 0.5f,
            CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut)
        {
            if (targetCamera == null || _brain == null)
            {
                Debug.LogWarning("[CameraManager] Caméra cible ou Brain null !");
                return;
            }

            _brain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, blendTime);

            int highestPriority = 0;
            foreach (var c in cam)
                if (c.Priority > highestPriority)
                    highestPriority = c.Priority;

            targetCamera.Priority = highestPriority + 1;
            _currentCam = targetCamera;
        }
        public void SetFreeCam(bool enabled, float sensitivity = 2f, Transform target = null, float distance = 5f)
        {
            _freeCamSensitivity = sensitivity;
            _freeCamDistance = distance;
            _freeCamTarget = target;

            if (enabled)
            {
                if (_mainCamera == null) { Debug.LogError("[CameraManager] Main Camera introuvable !"); return; }

                _brain.enabled = false;
                _isFreeCam = true;

                if (_freeCamTarget != null)
                {
                    Vector3 dir = _mainCamera.transform.position - _freeCamTarget.position;
                    _freeCamDistance = dir.magnitude;
                    Vector3 angles = Quaternion.LookRotation(-dir.normalized).eulerAngles;
                    _freeCamYaw = angles.y;
                    _freeCamPitch = angles.x > 180f ? angles.x - 360f : angles.x;
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
                _brain.enabled = true;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                Debug.Log("[CameraManager] FreeCam OFF");
            }
        }
        public void ConfigureCamera(CameraSettings settings)
        {
            if (_currentCam == null) { Debug.LogWarning("[CameraManager] Aucune caméra active !"); return; }
            if (settings == null) { Debug.LogWarning("[CameraManager] CameraSettings est null !"); return; }

            ApplyFollow(_currentCam, settings);
            ApplyFOV(_currentCam, settings);
            ApplyDamping(_currentCam, settings);
        }

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
        /// Déclenche un tremblement de caméra via le système Cinemachine Impulse.
        /// </summary>
        public void ShakeCamera(float duration, float amplitude = 1f, float frequency = 1f)
        {
            if (_impulseSource == null) return;

            // On génère une direction aléatoire pour l'impact (X et Y)
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;

            // On applique l'amplitude directement comme multiplicateur de la force de l'impact
            _impulseSource.DefaultVelocity = randomDirection * amplitude;

            var envelope = _impulseSource.ImpulseDefinition.TimeEnvelope;

            // Pour que le shake commence instantanément, dure, puis s'estompe gentiment :
            envelope.AttackTime = 0f;
            envelope.SustainTime = duration * 0.7f;
            envelope.DecayTime = duration * 0.3f;

            _impulseSource.ImpulseDefinition.TimeEnvelope = envelope;
            _impulseSource.ImpulseDefinition.FrequencyGain = frequency;

            _impulseSource.GenerateImpulseWithVelocity(_impulseSource.DefaultVelocity);

            Debug.Log($"[CameraManager] Impulse Shake | amplitude={amplitude} | duration={duration} | frequency={frequency}");
        }
        public void StopShake()
        {
            if (CinemachineImpulseManager.Instance != null)
            {
                CinemachineImpulseManager.Instance.Clear();
            }
        }

        #endregion

        #region Private Methods
        private void ApplyFollow(CinemachineCamera camera, CameraSettings s)
        {
            if (s.Follow != null)
            {
                camera.Follow = s.Follow;
                camera.LookAt = s.LookAt != null ? s.LookAt : s.Follow;
            }

            if (s.FollowOffset == null) return;
            Vector3 offset = s.FollowOffset.Value;

            var f = camera.GetComponent<CinemachineFollow>();
            if (f != null) { f.FollowOffset = offset; return; }

            var o = camera.GetComponent<CinemachineOrbitalFollow>();
            if (o != null) { o.TargetOffset = offset; return; }

            var p = camera.GetComponent<CinemachinePositionComposer>();
            if (p != null) { p.TargetOffset = offset; return; }

            Debug.LogWarning($"[CameraManager] Aucun composant de position trouvé sur '{camera.name}'.");
        }

        private void ApplyFOV(CinemachineCamera camera, CameraSettings s)
        {
            if (s.FOV == null) return;
            camera.Lens.FieldOfView = s.FOV.Value;
        }

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

                var hl = camera.GetComponent<CinemachineHardLookAt>();
                if (hl != null)
                    Debug.LogWarning("[CameraManager] CinemachineHardLookAt n'a pas de damping de rotation.");
            }
        }
        #endregion

    }
}