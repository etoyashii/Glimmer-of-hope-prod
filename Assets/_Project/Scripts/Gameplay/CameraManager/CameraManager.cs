using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Cinemachine.CinemachineImpulseDefinition;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Gère toutes les caméras Cinemachine de la scène :
    /// switch avec blend, configuration à la volée, FreeCam souris et shake par impulsion.
    /// Singleton persistant entre les scènes.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        #region Public Properties
        public static CameraManager Instance { get; private set; }

        /// Toutes les CinemachineCamera gérées, assignées dans l'Inspector.
        public CinemachineCamera[] cam;

        [Tooltip("La Main Camera de la scène (avec CinemachineBrain)")]
        public Camera mainCamera;

        /// Forme de l'impulsion utilisée pour le shake.
        public enum CameraShapeType { Bump, Explosion, Recoil, Rumble }
        #endregion

        #region Private Fields
        private CinemachineBrain _brain;
        private Camera _mainCamera;

        /// Caméra Cinemachine actuellement active.
        private CinemachineCamera _currentCam;

        // --- FreeCam ---
        private bool _isFreeCam = false;
        private float _freeCamYaw = 0f;        // rotation horizontale (degrés)
        private float _freeCamPitch = 0f;      // rotation verticale  (degrés, clampée ±89°)
        private float _freeCamSensitivity = 2f;
        private Mouse _mouse;
        private float _freeCamDistance = 5f;   // distance orbitale autour de la cible
        private Transform _freeCamTarget;      // null = rotation libre, sinon orbite

        // --- Shake (Cinemachine Impulse) ---
        private CinemachineImpulseSource _impulseSource;
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            // Singleton : détruit tout doublon et survit aux changements de scène
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Le Brain est nécessaire pour les blends et les priorités
            _brain = FindFirstObjectByType<CinemachineBrain>();
            if (_brain == null)
                Debug.LogError("[CameraManager] Aucun CinemachineBrain trouvé !");

            // Fallback sur Camera.main si rien n'est assigné dans l'Inspector
            _mainCamera = mainCamera != null ? mainCamera : Camera.main;
            if (_mainCamera == null)
                Debug.LogError("[CameraManager] Aucune Main Camera trouvée ! Assigne-la dans l'Inspector ou tague-la 'MainCamera'.");

            _mouse = Mouse.current;

            // Récupère ou ajoute dynamiquement la source d'impulsion sur ce GameObject
            _impulseSource = GetComponent<CinemachineImpulseSource>();
            if (_impulseSource == null)
                _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        private void Update()
        {
            // La FreeCam bypass le Brain et pilote directement la Main Camera
            if (!_isFreeCam || _mainCamera == null) return;

            // Tentative de récupération tardive de la souris (hot-plug)
            if (_mouse == null)
            {
                _mouse = Mouse.current;
                if (_mouse == null) return;
            }

            var delta = _mouse.delta.ReadValue();
            _freeCamYaw += delta.x * _freeCamSensitivity * 0.1f;
            _freeCamPitch -= delta.y * _freeCamSensitivity * 0.1f;
            _freeCamPitch = Mathf.Clamp(_freeCamPitch, -89f, 89f); // évite le gimbal lock

            Quaternion rotation = Quaternion.Euler(_freeCamPitch, _freeCamYaw, 0f);

            if (_freeCamTarget != null)
            {
                // Mode orbite : tourne autour de la cible à distance fixe
                _mainCamera.transform.position = _freeCamTarget.position + rotation * new Vector3(0f, 0f, -_freeCamDistance);
                _mainCamera.transform.rotation = rotation;
            }
            else
            {
                // Mode libre : rotation sur place sans déplacement
                _mainCamera.transform.rotation = rotation;
            }
        }
        #endregion

        #region public Methods

        /// <summary>
        /// Switche vers la caméra portant le nom donné, avec blend optionnel.
        /// </summary>
        public void SwitchCamera(
            string cameraName,
            float blendTime = 0.5f,
            CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut)
        {
            var target = System.Array.Find(cam, c => c.name == cameraName);
            if (target == null) { Debug.LogWarning($"[CameraManager] Caméra non trouvée : {cameraName}"); return; }
            SwitchCamera(target, blendTime, blendStyle);
        }

        /// <summary>
        /// Switche vers la caméra donnée en référence directe, avec blend optionnel.
        /// La priorité est incrémentée pour qu'elle prenne le dessus sur les autres.
        /// </summary>
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

            // Applique le style de blend souhaité au Brain avant le switch
            _brain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, blendTime);

            // Donne la priorité la plus haute à la caméra cible pour qu'elle s'active
            int highestPriority = 0;
            foreach (var c in cam)
                if (c.Priority > highestPriority)
                    highestPriority = c.Priority;

            targetCamera.Priority = highestPriority + 1;
            _currentCam = targetCamera;
        }

        /// <summary>
        /// Active ou désactive la FreeCam.
        /// En mode orbite (target != null), la caméra tourne autour de la cible.
        /// En mode libre (target == null), elle pivote sur place.
        /// Désactive le Brain pendant la FreeCam pour prendre le contrôle total.
        /// </summary>
        public void SetFreeCam(bool enabled, float sensitivity = 2f, Transform target = null, float distance = 5f)
        {
            _freeCamSensitivity = sensitivity;
            _freeCamDistance = distance;
            _freeCamTarget = target;

            if (enabled)
            {
                if (_mainCamera == null) { Debug.LogError("[CameraManager] Main Camera introuvable !"); return; }

                _brain.enabled = false; // désactive Cinemachine pour piloter la caméra manuellement
                _isFreeCam = true;

                // Initialise yaw/pitch depuis la position courante pour éviter un saut brutal
                if (_freeCamTarget != null)
                {
                    Vector3 dir = _mainCamera.transform.position - _freeCamTarget.position;
                    _freeCamDistance = dir.magnitude;
                    Vector3 angles = Quaternion.LookRotation(-dir.normalized).eulerAngles;
                    _freeCamYaw = angles.y;
                    _freeCamPitch = angles.x > 180f ? angles.x - 360f : angles.x; // normalise en [-180, 180]
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
                _brain.enabled = true; // rend le contrôle à Cinemachine

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                Debug.Log("[CameraManager] FreeCam OFF");
            }
        }

        /// <summary>
        /// Applique un "CameraSettings" sur la caméra active (follow, FOV, damping).
        /// </summary>
        public void ConfigureCamera(CameraSettings settings)
        {
            if (_currentCam == null) { Debug.LogWarning("[CameraManager] Aucune caméra active !"); return; }
            if (settings == null) { Debug.LogWarning("[CameraManager] CameraSettings est null !"); return; }

            ApplyFollow(_currentCam, settings);
            ApplyFOV(_currentCam, settings);
            ApplyDamping(_currentCam, settings);
        }

        /// <summary>Surcharge raccourcie pour assigner follow + offset + lookAt.</summary>
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
        /// Déclenche un tremblement de caméra via Cinemachine Impulse.
        /// La direction de l'impact est aléatoire sur XY.
        /// </summary>
        /// <param name="duration">Durée totale du shake en secondes.</param>
        /// <param name="amplitude">Force de l'impact (scale de la vélocité).</param>
        /// <param name="frequency">Fréquence des oscillations.</param>
        /// <param name="shape">Forme de la courbe d'impulsion.</param>
        public void ShakeCamera(float duration, float amplitude = 1f, float frequency = 1f, CameraShapeType shape = CameraShapeType.Bump)
        {
            if (_impulseSource == null) return;

            // Direction aléatoire normalisée sur le plan XY pour varier l'impact
            _impulseSource.DefaultVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized * amplitude;

            // Durée totale + enveloppe : attaque immédiate, sustain long, decay court
            _impulseSource.ImpulseDefinition.ImpulseDuration = duration;
            var envelope = _impulseSource.ImpulseDefinition.TimeEnvelope;
            envelope.AttackTime = 0f;
            envelope.SustainTime = duration * 0.7f;
            envelope.DecayTime = duration * 0.3f;
            _impulseSource.ImpulseDefinition.TimeEnvelope = envelope;
            _impulseSource.ImpulseDefinition.FrequencyGain = frequency;

            // Mapping de l'enum custom vers l'enum interne Cinemachine
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

        /// Stoppe immédiatement tous les shakes en cours.</summary>
        public void StopShake()
        {
            if (CinemachineImpulseManager.Instance != null)
                CinemachineImpulseManager.Instance.Clear();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Assigne le follow/lookAt et applique l'offset sur le composant de position détecté.
        /// Supporte CinemachineFollow, OrbitalFollow et PositionComposer.
        /// </summary>
        private void ApplyFollow(CinemachineCamera camera, CameraSettings s)
        {
            if (s.Follow != null)
            {
                camera.Follow = s.Follow;
                camera.LookAt = s.LookAt != null ? s.LookAt : s.Follow; // fallback : regarde la cible de follow
            }

            if (s.FollowOffset == null) return;
            Vector3 offset = s.FollowOffset.Value;

            // Applique l'offset sur le premier composant de position trouvé (ordre de priorité)
            var f = camera.GetComponent<CinemachineFollow>();
            if (f != null) { f.FollowOffset = offset; return; }

            var o = camera.GetComponent<CinemachineOrbitalFollow>();
            if (o != null) { o.TargetOffset = offset; return; }

            var p = camera.GetComponent<CinemachinePositionComposer>();
            if (p != null) { p.TargetOffset = offset; return; }

            Debug.LogWarning($"[CameraManager] Aucun composant de position trouvé sur '{camera.name}'.");
        }

        /// <summary>Applique le FOV si renseigné dans le CameraSettings.</summary>
        private void ApplyFOV(CinemachineCamera camera, CameraSettings s)
        {
            if (s.FOV == null) return;
            camera.Lens.FieldOfView = s.FOV.Value;
        }

        /// <summary>
        /// Applique le damping de position (Follow / OrbitalFollow)
        /// et de rotation (RotationComposer) si renseignés dans le CameraSettings.
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

                // HardLookAt ne supporte pas le damping de rotation
                var hl = camera.GetComponent<CinemachineHardLookAt>();
                if (hl != null)
                    Debug.LogWarning("[CameraManager] CinemachineHardLookAt n'a pas de damping de rotation.");
            }
        }
        #endregion
    }
}