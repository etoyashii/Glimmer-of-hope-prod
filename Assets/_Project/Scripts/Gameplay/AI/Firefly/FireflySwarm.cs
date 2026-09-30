using UnityEngine;
using UnityEngine.Rendering;

namespace GlimmerOfHope.Gameplay.Firefly
{
    [DisallowMultipleComponent]
    public class FireflySwarm : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Swarm")]
        [Tooltip("Number of glowing dots in the group.")]
        [Range(1, 30)]
        [SerializeField] private int _count = 8;
        [Tooltip("Radius of the cloud around the firefly center. Follows the object scale.")]
        [Range(0.05f, 3.0f)]
        [SerializeField] private float _spread = 0.5f;
        [Tooltip("Size of one dot. Follows the object scale.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _dotSize = 0.07f;
        [Range(0.0f, 5.0f)]
        [SerializeField] private float _wanderSpeed = 0.8f;
        [Tooltip("How much each dot pulses. 0 = steady light.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float _twinkle = 0.4f;

        [Header("Visual")]
        [Tooltip("Leave empty to reuse the mesh of this object or of its LOD children.")]
        [SerializeField] private Mesh _dotMesh;
        [Tooltip("Leave empty to reuse the material of this object.")]
        [SerializeField] private Material _dotMaterial;
        [Tooltip("Hides the placeholder renderers (LODs included) once the dots are spawned.")]
        [SerializeField] private bool _hideSourceRenderer = true;

        #endregion

        #region Private Fields

        private Transform[] _dots;
        private Vector3[] _seeds;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            MeshFilter srcFilter = GetComponentInChildren<MeshFilter>(true);
            Renderer[] srcRenderers = GetComponentsInChildren<Renderer>(true);

            Mesh mesh = _dotMesh;
            Material mat = _dotMaterial;

            if (mesh == null && srcFilter != null) mesh = srcFilter.sharedMesh;
            if (mat == null && srcRenderers.Length > 0) mat = srcRenderers[0].sharedMaterial;

            if (mesh == null || mat == null)
            {
                Debug.LogWarning($"[FireflySwarm] '{name}' has no mesh or material to draw, disabling.", this);
                enabled = false;
                return;
            }

            if (_hideSourceRenderer)
            {
                for (int i = 0; i < srcRenderers.Length; i++)
                {
                    srcRenderers[i].enabled = false;
                }
            }

            SpawnDots(mesh, mat);
        }

        private void Update()
        {
            if (_dots == null) return;

            float t = Time.time * _wanderSpeed;

            for (int i = 0; i < _dots.Length; i++)
            {
                Vector3 seed = _seeds[i];

                float x = Mathf.PerlinNoise(seed.x + t, seed.y) - 0.5f;
                float y = Mathf.PerlinNoise(seed.y + t, seed.z) - 0.5f;
                float z = Mathf.PerlinNoise(seed.z + t, seed.x) - 0.5f;

                float pulse = 1.0f - _twinkle * (0.5f + 0.5f * Mathf.Sin(Time.time * 4.0f + seed.x));

                _dots[i].localPosition = new Vector3(x, y, z) * (_spread * 2.0f);
                _dots[i].localScale = Vector3.one * (_dotSize * pulse);
            }
        }

        #endregion

        #region Private Methods

        private void SpawnDots(Mesh mesh, Material mat)
        {
            _dots = new Transform[_count];
            _seeds = new Vector3[_count];

            for (int i = 0; i < _count; i++)
            {
                GameObject dot = new GameObject("Dot_" + i);
                dot.transform.SetParent(transform, false);

                dot.AddComponent<MeshFilter>().sharedMesh = mesh;

                MeshRenderer rend = dot.AddComponent<MeshRenderer>();
                rend.sharedMaterial = mat;
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows = false;

                _dots[i] = dot.transform;
                _seeds[i] = new Vector3(Random.Range(0.0f, 100.0f), Random.Range(0.0f, 100.0f), Random.Range(0.0f, 100.0f));
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1.0f, 0.95f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _spread * transform.lossyScale.x);
        }

        #endregion
    }
}
