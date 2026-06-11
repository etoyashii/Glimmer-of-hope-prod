using UnityEngine;
using Unity.Cinemachine;

namespace GlimmerOfHope.Gameplay
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }
        public CinemachineCamera[] cam;
        private CinemachineBrain _brain;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Récupère le CinemachineBrain
            _brain = FindFirstObjectByType<CinemachineBrain>();
            if (_brain == null)
            {
                Debug.LogError("Aucun CinemachineBrain trouvé dans la scène !");
            }
        }

        // Switch par nom avec personnalisation du blend
        public void SwitchCamera(
            string cameraName,
            float blendTime = 0.5f,
            CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut)
        {
            CinemachineCamera target = System.Array.Find(cam, c => c.name == cameraName);
            if (target == null)
            {
                Debug.LogWarning($"Caméra non trouvée : {cameraName}");
                return;
            }
            SwitchCamera(target, blendTime, blendStyle);
        }

        // Switch par référence avec personnalisation du blend
        public void SwitchCamera(
            CinemachineCamera targetCamera,
            float blendTime = 0.5f,
            CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut)
        {
            if (targetCamera == null || _brain == null)
            {
                Debug.LogWarning("La caméra cible ou le CinemachineBrain est null !");
                return;
            }

            // Configure le blend pour le CinemachineBrain
            _brain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, blendTime);

            // Met à jour la priorité pour forcer le switch
            int highestPriority = 0;
            foreach (var camera in cam)
            {
                if (camera.Priority > highestPriority)
                {
                    highestPriority = camera.Priority;
                }
            }

            // Active la caméra cible en lui donnant la priorité la plus haute
            targetCamera.Priority = highestPriority + 1;
        }
    }
}