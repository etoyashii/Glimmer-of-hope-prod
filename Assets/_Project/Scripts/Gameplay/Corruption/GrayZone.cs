using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Project a mask (from and array in the manager) and return the data of positions etc
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Collider))]
    public class GrayZone : MonoBehaviour
    {
        public Vector2 size = new Vector2(10, 10);
        [Range(0, 1)]
        public float threshold = 0.5f;
        public int maskIndex;

        private void OnEnable()
        {
            GrayZoneManager.Register(this);
        }

        private void OnDisable()
        {
            GrayZoneManager.Unregister(this);
        }

        private void OnValidate()
        {
            GrayZoneManager.MarkDirty();
        }

#if UNITY_EDITOR
        // ca bouge quand ca bouge
        private void Update()
        {
            if (!Application.isPlaying && transform.hasChanged)
            {
                GrayZoneManager.MarkDirty();
                transform.hasChanged = false;
            }
        }

    // pour debug si probleme de taille
    /*    private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = transform.position;
            Vector3 extents = new Vector3(size.x, 0.1f, size.y);
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, extents);
            Gizmos.matrix = old;

            if (transform.lossyScale != Vector3.one)
            {
                Debug.LogWarning(
                    $"[GrayZone] '{name}' a un scale de {transform.lossyScale} au lieu de (1,1,1). " +
                    "Le scale fausse le calcul de position locale dans le shader (la zone peut couvrir tout le terrain). " +
                    "Utilise le champ 'size' pour dimensionner la zone, pas le Transform.",
                    this);
            }
        }*/
#endif

        public GrayZoneData GetData()
        {
            GrayZoneData data = new GrayZoneData();
            data.worldToLocal = transform.worldToLocalMatrix;
            data.size = size;
            data.threshold = threshold;
            data.maskIndex = maskIndex;
            return data;
        }
    }
}