using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Disables decorative objects that leave the camera frustum to save CPU
    /// (scripts, animation, physics) and shadow casting. Targets are the renderers under
    /// the decor roots set in the inspector, so gameplay (platforms, ground, player) is
    /// never affected. Distance and on-screen size reduction is handled by the LOD tool.
    /// </summary>
    public class OffscreenCuller : MonoBehaviour
    {
        #region SerializeFields

        [Header("Camera")]
        [Tooltip("Leave empty to use the CameraManager main camera, then Camera.main")]
        [SerializeField] private Camera _camera;

        [Header("Targets")]
        [Tooltip("Drag the parent objects of your decor here ([TREE], vegetation...). Everything under them is culled off-screen")]
        [SerializeField] private Transform[] _decorRoots;

        [Header("Tuning")]
        [Tooltip("Seconds between two frustum scans")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _scanInterval = 0.15f;

        [Tooltip("Extra world margin around the frustum so objects wake up before they pop in")]
        [Range(0f, 20f)]
        [SerializeField] private float _frustumMargin = 4f;

        #endregion

        #region Private Fields

        private readonly List<CullTarget> _targets = new List<CullTarget>();
        private readonly Plane[] _planes = new Plane[6];
        private float _scanCooldown;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ResolveCamera();

            if (_camera == null)
            {
                Debug.LogError("[OffscreenCuller] No camera found. Disabling.");
                enabled = false;
                return;
            }

            CollectTargets();
        }

        private void Update()
        {
            _scanCooldown -= Time.deltaTime;
            if (_scanCooldown > 0f) return;

            _scanCooldown = _scanInterval;
            Scan();
        }

        #endregion

        #region Private Methods

        private void ResolveCamera()
        {
            if (_camera != null) return;

            if (CameraManager.Instance != null && CameraManager.Instance.mainCamera != null)
                _camera = CameraManager.Instance.mainCamera;
            else
                _camera = Camera.main;
        }

        //Cache decor bounds once: decor is static so world bounds never move, and a
        //disabled object can no longer report its bounds on later scans.
        private void CollectTargets()
        {
            _targets.Clear();
            if (_decorRoots == null) return;

            var seen = new HashSet<int>();
            foreach (Transform root in _decorRoots)
            {
                if (root == null) continue;
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(false))
                    AddTarget(r, seen);
            }
        }

        private void AddTarget(Renderer r, HashSet<int> seen)
        {
            if (r == null || !seen.Add(r.gameObject.GetInstanceID())) return;
            _targets.Add(new CullTarget(r.gameObject, r.bounds));
        }

        private void Scan()
        {
            GeometryUtility.CalculateFrustumPlanes(_camera, _planes);

            foreach (CullTarget t in _targets)
            {
                Bounds b = t.Bounds;
                b.Expand(_frustumMargin * 2f);

                bool inView = GeometryUtility.TestPlanesAABB(_planes, b);
                if (inView != t.Go.activeSelf) t.Go.SetActive(inView);
            }
        }

        #endregion

        #region Types

        private struct CullTarget
        {
            public readonly GameObject Go;
            public readonly Bounds Bounds;

            public CullTarget(GameObject go, Bounds bounds)
            {
                Go = go;
                Bounds = bounds;
            }
        }

        #endregion
    }
}
