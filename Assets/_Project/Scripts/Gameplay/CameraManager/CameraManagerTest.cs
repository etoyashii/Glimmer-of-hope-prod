using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static GlimmerOfHope.Gameplay.CameraManager;

namespace GlimmerOfHope.Gameplay
{
    public class CameraManagerTester : MonoBehaviour
    {
        #region public properties
        [Header("Cibles")]
        public GameObject followTarget;
        public GameObject lookAtTarget;

        [Header("Offsets")]
        public Vector3 offsetA = new Vector3(0f, 3f, -5f);
        public Vector3 offsetB = new Vector3(2f, 5f, -8f);

        [Header("FOV")]
        public float testFOV = 75f;

        [Header("Damping")]
        public Vector3 testPositionDamping = new Vector3(1f, 1f, 1f);
        public float testRotationDamping = 0.5f;

        [Header("Noms des caméras")]
        public string cam1Name = "Cam1";
        public string cam2Name = "Cam2";
        public string cam3Name = "Cam3";

        [Header("Free Cam")]
        public float freeCamSensitivity = 5f;
        public float freeCamDistance = 5f;

        [Header("Shake")]
        public float shakeDuration = 0.5f;
        public float shakeAmplitude = 2f;
        public float shakeFrequency = 2f;
        public CameraShapeType shakeShape = CameraShapeType.Bump;
        #endregion

        #region private properties
        private InputAction _keyH, _keyJ;
        private InputAction _key1, _key2, _key3;
        private InputAction _keyQ;
        private InputAction _keyF, _keyG, _keyR, _keyL, _keyD, _keyV;
        private InputAction _keyB, _keyC;
        private bool _freeCamActive = false;
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            // Fallback : cherche le Player par tag si followTarget non assigné
            if (followTarget == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) followTarget = player;
                else Debug.LogWarning("[Tester] Pas de followTarget et pas de tag 'Player'.");
            }
        }

        private void Start() => RegisterInputs();

        private void OnDestroy()
        {
            _key1.Dispose(); _key2.Dispose(); _key3.Dispose();
            _keyQ.Dispose();
            _keyF.Dispose(); _keyG.Dispose(); _keyR.Dispose();
            _keyL.Dispose(); _keyD.Dispose(); _keyV.Dispose();
            _keyB.Dispose(); _keyC.Dispose();
            _keyH.Dispose();
            _keyJ.Dispose();
        }
        #endregion

        #region Private Methods
        private void RegisterInputs()
        {
            // Création et binding des touches
            _key1 = new InputAction("Key1", InputActionType.Button, "<Keyboard>/1");
            _key2 = new InputAction("Key2", InputActionType.Button, "<Keyboard>/2");
            _key3 = new InputAction("Key3", InputActionType.Button, "<Keyboard>/3");
            _keyQ = new InputAction("KeyQ", InputActionType.Button, "<Keyboard>/q");
            _keyF = new InputAction("KeyF", InputActionType.Button, "<Keyboard>/f");
            _keyG = new InputAction("KeyG", InputActionType.Button, "<Keyboard>/g");
            _keyR = new InputAction("KeyR", InputActionType.Button, "<Keyboard>/r");
            _keyL = new InputAction("KeyL", InputActionType.Button, "<Keyboard>/l");
            _keyD = new InputAction("KeyD", InputActionType.Button, "<Keyboard>/d");
            _keyV = new InputAction("KeyV", InputActionType.Button, "<Keyboard>/v");
            _keyB = new InputAction("KeyB", InputActionType.Button, "<Keyboard>/b");
            _keyC = new InputAction("KeyC", InputActionType.Button, "<Keyboard>/c");

            _key1.performed += _ => TestSwitchByName(cam1Name);
            _key2.performed += _ => TestSwitchByName(cam2Name);
            _key3.performed += _ => TestSwitchByName(cam3Name);
            _keyQ.performed += _ => TestSwitchByReference();
            _keyF.performed += _ => TestFollow();
            _keyG.performed += _ => TestFollowWithFOV();
            _keyR.performed += _ => TestResetOffset();
            _keyL.performed += _ => TestFollowWithSeparateLookAt();
            _keyD.performed += _ => TestDampingOnly();
            _keyV.performed += _ => TestFOVOnly();
            _keyB.performed += _ => TestSwitchCut();
            _keyC.performed += _ => TestToggleFreeCam();

            _key1.Enable(); _key2.Enable(); _key3.Enable();
            _keyQ.Enable();
            _keyF.Enable(); _keyG.Enable(); _keyR.Enable();
            _keyL.Enable(); _keyD.Enable(); _keyV.Enable();
            _keyB.Enable(); _keyC.Enable();

            _keyH = new InputAction("KeyH", InputActionType.Button, "<Keyboard>/h");
            _keyJ = new InputAction("KeyJ", InputActionType.Button, "<Keyboard>/j");
            _keyH.performed += _ => TestShake();
            _keyJ.performed += _ => TestStopShake();
            _keyH.Enable();
            _keyJ.Enable();
        }

        private void TestShake()
        {
            if (!EnsureManager()) return;
            Debug.Log($"[TEST][H] ShakeCamera | duration={shakeDuration} | amplitude={shakeAmplitude} | frequency={shakeFrequency} | shape={shakeShape}");
            CameraManager.Instance.ShakeCamera(shakeDuration, shakeAmplitude, shakeFrequency, shakeShape);
        }

        private void TestStopShake()
        {
            if (!EnsureManager()) return;
            Debug.Log("[TEST][J] StopShake");
            CameraManager.Instance.StopShake();
        }

        private void TestToggleFreeCam()
        {
            if (!EnsureManager()) return;
            _freeCamActive = !_freeCamActive;
            Debug.Log($"[TEST][C] FreeCam → {(_freeCamActive ? "ON" : "OFF")}");

            Transform target = followTarget != null ? followTarget.transform : null;
            CameraManager.Instance.SetFreeCam(_freeCamActive, freeCamSensitivity, target, freeCamDistance);
        }

        private void TestSwitchByName(string camName)
        {
            if (!EnsureManager()) return;
            Debug.Log($"[TEST] SwitchCamera(name={camName})");
            CameraManager.Instance.SwitchCamera(camName);
        }

        private void TestSwitchByReference()
        {
            if (!EnsureManager()) return;
            var cams = CameraManager.Instance.cam;
            if (cams == null || cams.Length == 0) { Debug.LogWarning("[TEST] cam[] est vide !"); return; }
            Debug.Log($"[TEST] SwitchCamera(ref={cams[0].name}, Linear 1s)");
            CameraManager.Instance.SwitchCamera(cams[0], 1f, CinemachineBlendDefinition.Styles.Linear);
        }

        private void TestSwitchCut()
        {
            if (!EnsureManager()) return;
            Debug.Log($"[TEST] SwitchCamera(name={cam2Name}, Cut)");
            CameraManager.Instance.SwitchCamera(cam2Name, 0f, CinemachineBlendDefinition.Styles.Cut);
        }

        private void TestFollow()
        {
            if (!EnsureTarget()) return;
            Debug.Log($"[TEST][F] Follow={followTarget.name} offset={offsetA}");
            CameraManager.Instance.ConfigureCamera(CameraSettings.WithFollow(followTarget.transform, offsetA));
        }

        private void TestFollowWithFOV()
        {
            if (!EnsureTarget()) return;
            Debug.Log($"[TEST][G] Follow={followTarget.name} offset={offsetB} FOV={testFOV}");
            CameraManager.Instance.ConfigureCamera(new CameraSettings
            {
                Follow = followTarget.transform,
                FollowOffset = offsetB,
                LookAt = followTarget.transform,
                FOV = testFOV
            });
        }

        private void TestResetOffset()
        {
            if (!EnsureTarget()) return;
            Debug.Log("[TEST][R] Reset offset (0,0,0)");
            CameraManager.Instance.ConfigureCamera(new CameraSettings
            {
                Follow = followTarget.transform,
                FollowOffset = Vector3.zero,
                LookAt = followTarget.transform
            });
        }

        private void TestFollowWithSeparateLookAt()
        {
            if (!EnsureTarget()) return;
            if (lookAtTarget == null) { Debug.LogWarning("[TEST][L] lookAtTarget est null !"); return; }
            Debug.Log($"[TEST][L] Follow={followTarget.name} LookAt={lookAtTarget.name}");
            CameraManager.Instance.ConfigureCamera(
                CameraSettings.WithFollowAndLookAt(followTarget.transform, lookAtTarget.transform, offsetA));
        }

        private void TestDampingOnly()
        {
            if (!EnsureManager()) return;
            Debug.Log($"[TEST][D] Damping pos={testPositionDamping} rot={testRotationDamping}");
            CameraManager.Instance.ConfigureCamera(CameraSettings.WithDamping(testPositionDamping, testRotationDamping));
        }

        private void TestFOVOnly()
        {
            if (!EnsureManager()) return;
            Debug.Log($"[TEST][V] FOV={testFOV}");
            CameraManager.Instance.ConfigureCamera(CameraSettings.WithFOV(testFOV));
        }

        // Vérifie que le CameraManager est disponible
        private bool EnsureManager()
        {
            if (CameraManager.Instance != null) return true;
            Debug.LogError("[Tester] CameraManager.Instance est null !");
            return false;
        }

        // Vérifie que le CameraManager et le followTarget sont disponibles
        private bool EnsureTarget()
        {
            if (!EnsureManager()) return false;
            if (followTarget != null) return true;
            Debug.LogWarning("[Tester] followTarget est null !");
            return false;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 320));
            GUILayout.Box(
                "[CameraManagerTester]\n" +
                $"[1] Switch → {cam1Name}\n" +
                $"[2] Switch → {cam2Name}\n" +
                $"[3] Switch → {cam3Name}\n" +
                "[A] Switch ref cams[0] (Linear 1s)\n" +
                $"[F] Follow + offsetA {offsetA}\n" +
                $"[G] Follow + offsetB {offsetB} + FOV {testFOV}\n" +
                "[R] Reset offset (0,0,0)\n" +
                $"[L] Follow + LookAt séparé ({(lookAtTarget != null ? lookAtTarget.name : "non assigné")})\n" +
                $"[D] Damping pos={testPositionDamping} rot={testRotationDamping}\n" +
                $"[V] FOV → {testFOV}\n" +
                "[B] Switch Cut instantané\n" +
                $"[C] FreeCam toggle ({(_freeCamActive ? "ON ✓" : "OFF")})\n" +
                $"[H] Shake dur={shakeDuration} amp={shakeAmplitude} freq={shakeFrequency}\n" +
                "[J] Stop Shake\n"
            );
            GUILayout.EndArea();
        }
        #endregion
    }
}