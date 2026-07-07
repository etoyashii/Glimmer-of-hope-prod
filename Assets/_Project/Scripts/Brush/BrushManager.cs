using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Core
{
    /// <summary>
    /// Manages the data of the brush tool and draw the gizmo
    /// </summary>
    [ExecuteInEditMode]
    public class BrushManager : MonoBehaviour
    {
        #region Constants
        private int SEGMENTS = 320;          // Number of segments for the circle gizmo
        private float RAYCAST_DISTANCE = 100f; // Max distance for ground raycast
        #endregion

        #region Serialized Fields
        [SerializeField] private List<AssetsStruct> _assets = new(); // List of available asset templates
        [SerializeField] private GameObject _stokageAssets;          // Parent for active assets
        [SerializeField] private GameObject _stokageAssetsUseless;   // Parent for deleted/inactive assets
        [SerializeField] private LayerMask _groundLayer;              // Layer mask for ground detection
        [SerializeField] private bool _deleteMode = false;            // Toggle delete mode
        [SerializeField] private bool _clearMode = false;            // Toggle clear mode
        [Range(1, 100)]
        [SerializeField] private int _probClearAssets = 20;            // Pourcentage of assets cleared
        [Range(1f, 20f)]
        [SerializeField] private float _density = 4f;                 // Density of asset placement
        [SerializeField] private int _revertNumber = 10;              // Number of revert in memory
        [Range(0.0001f, 0.05f), Tooltip("Base value is 0.001")]
        [SerializeField] private float _multDensity = 0.001f;              // to adjust density
        [Tooltip("1 is the base value and serves as a size multiplier for all assets placed afterward.")]
        [SerializeField] private float _sizeMult = 1f;              // asset size multiplicator
        [Range(0f, 90f), Tooltip("Maximum slope angle (in degrees) on which assets can be placed. 0 = flat only, 90 = no restriction.")]
        [SerializeField] private float _maxSlopeAngle = 30f;        // Max slope angle for asset placement
        [SerializeField] private Terrain _terrain;                   // Terrain used for splatmap checks
        [SerializeField] private bool _useTerrainLayerFilter = false; // Enable/disable terrain layer filtering
        [SerializeField] private TerrainLayer _terrainLayer;         // Terrain layer required to place assets
        #endregion

        #region Private Fields
        private int _groundLayerMask; // Cached ground layer mask
        private Vector3 _pos;          // Current brush position
        #endregion

        #region Public Properties
        public float _circleRadius = 5f; // Radius of the brush circle
        [HideInInspector] public bool _lastActionWasAdd = true; // If action was add Asset = true else false
        [HideInInspector] public Color _actualColor = Color.green;
        public List<AssetsStruct> Assets => _assets;
        public GameObject StokageAssets => _stokageAssets;
        public GameObject StokageAssetsUseless => _stokageAssetsUseless;
        public LayerMask GroundLayer => _groundLayer;
        public bool DeleteMode => _deleteMode;
        public bool ClearMode => _clearMode;
        public int ProbClearAssets => _probClearAssets;
        public float Density => _density;
        public int GroundLayerMask => _groundLayerMask;
        public Vector3 Pos => _pos;
        public float RaycastDistance => RAYCAST_DISTANCE;
        public int RevertNumber => _revertNumber;
        public float MultDensity => _multDensity;
        public float SizeMult => _sizeMult;
        public float MaxSlopeAngle => _maxSlopeAngle;
        public Terrain UsedTerrain => _terrain;
        public bool UseTerrainLayerFilter => _useTerrainLayerFilter;
        public TerrainLayer TerrainLayer => _terrainLayer;
        public List<Transform> placedAssets = new();
        public List<Transform> unplacedAssets = new();
        public void SetPos(Vector3 pos) { _pos = pos; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns true if the given world position is on the configured TerrainLayer (splatmap check).
        /// If filtering is disabled or no TerrainLayer is set, always returns true.
        /// </summary>
        public bool IsOnTerrainLayer(Vector3 worldPos)
        {
            if (!_useTerrainLayerFilter || _terrainLayer == null || _terrain == null)
                return true;

            TerrainData data = _terrain.terrainData;
            Vector3 terrainPos = _terrain.transform.position;

            // Convert world position to alphamap coordinates
            float relX = Mathf.Clamp01((worldPos.x - terrainPos.x) / data.size.x);
            float relZ = Mathf.Clamp01((worldPos.z - terrainPos.z) / data.size.z);
            int mapX = Mathf.Clamp((int)(relX * data.alphamapWidth), 0, data.alphamapWidth - 1);
            int mapZ = Mathf.Clamp((int)(relZ * data.alphamapHeight), 0, data.alphamapHeight - 1);

            float[,,] alphamaps = data.GetAlphamaps(mapX, mapZ, 1, 1);

            // Find the index of the target TerrainLayer
            TerrainLayer[] layers = data.terrainLayers;
            int targetIndex = -1;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == _terrainLayer)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex < 0) return false;

            // Check if target layer is dominant at this position
            int dominantIndex = 0;
            float maxVal = 0f;
            for (int i = 0; i < alphamaps.GetLength(2); i++)
            {
                if (alphamaps[0, 0, i] > maxVal)
                {
                    maxVal = alphamaps[0, 0, i];
                    dominantIndex = i;
                }
            }

            return dominantIndex == targetIndex;
        }
        #endregion

        #region Unity Lifecycle
        private void OnValidate()
        {
            _groundLayerMask = _groundLayer.value;
        }
        private void OnEnable()
        {
            _groundLayerMask = _groundLayer.value;
        }

        void Update()
        {

        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Draws a circle gizmo on the ground
        /// </summary>
        private void OnDrawGizmos()
        {
            Handles.color = _actualColor;

            if (SEGMENTS <= 0) SEGMENTS = 32;

            // Calculate where to place circle point
            Vector3[] points = new Vector3[SEGMENTS + 1];
            for (int i = 0; i <= SEGMENTS; i++)
            {
                float angle = i * Mathf.PI * 2f / SEGMENTS;
                float x = Mathf.Cos(angle) * _circleRadius;
                float z = Mathf.Sin(angle) * _circleRadius;
                Vector3 pointOnCircle = new Vector3(x, 0, z) + _pos;

                Ray ray = new Ray(pointOnCircle + Vector3.up * RAYCAST_DISTANCE, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, RAYCAST_DISTANCE * 2f, _groundLayer))
                {
                    points[i] = hit.point;
                }
                else
                {
                    points[i] = pointOnCircle;
                }
            }

            // Draw the circle
            for (int i = 0; i < SEGMENTS; i++)
            {
                Handles.DrawLine(points[i], points[i + 1]);
            }
        }
        #endregion
    }
}