using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Audio
{
    /// <summary>
    /// Détecte le Terrain Layer dominant sous le joueur et prévient
    /// AmbientMusicManager en lui passant directement la référence du TerrainLayer.
    ///
    /// Mise en place : attacher sur le Player. Rien à remplir dans l'Inspecteur de
    /// CE script — tout le mapping se fait côté AmbientMusicManager (liste
    /// "Zone Musics", où tu glisses les mêmes assets TerrainLayer).
    /// </summary>
    public class TerrainMusicZone : MonoBehaviour
    {
        [Tooltip("Fréquence de vérification en secondes (pas besoin de checker chaque frame).")]
        public float checkInterval = 0.5f;

        private TerrainLayer lastLayer;
        private float timer;

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
    }
}