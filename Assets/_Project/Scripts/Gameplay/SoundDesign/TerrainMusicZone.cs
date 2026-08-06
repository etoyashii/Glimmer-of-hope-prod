using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Audio
{

    public class TerrainMusicZone : MonoBehaviour
    {
        #region Public Fields
        [Tooltip("Fréquence de vérification en secondes (pas besoin de checker chaque frame).")]
        public float checkInterval = 0.5f;
        #endregion

        #region Private Properties
        private TerrainLayer lastLayer;
        private float timer;
        #endregion

        #region Unity LifeCycle
        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < checkInterval) { return; }
            timer = 0f;

            TerrainLayer dominantLayer = TerrainLayerUtility.GetDominantTerrainLayer(transform.position);
            if (dominantLayer != null && dominantLayer != lastLayer)
            {
                AmbientMusicManager.Instance.SetZone(dominantLayer);
                lastLayer = dominantLayer;
            }
        }
        #endregion
    }
}