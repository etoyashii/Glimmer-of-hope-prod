using TMPro;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Fps
{
    [AddComponentMenu("Utils/FPS Display")]
    public class FpsDisplay : MonoBehaviour
    {
        #region Public Fields
        public TMP_Text text;
        [Tooltip("Intervalle de mise à jour en secondes, pour éviter que le chiffre change trop vite pour être lisible.")]
        public float updateInterval = 0.5f;

        float timer;
        int frames;
        #endregion

        #region Unity Lifecycle
        void Reset()
        {
            text = GetComponent<TMP_Text>();
        }

        void Update()
        {
            frames++;
            timer += Time.unscaledDeltaTime;

            if (timer >= updateInterval)
            {
                float fps = frames / timer;
                if (text != null) text.text = $"{fps:F0} FPS";

                timer = 0f;
                frames = 0;
            }
        }
        #endregion
    }
}