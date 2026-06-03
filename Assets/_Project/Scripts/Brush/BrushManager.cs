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
        [SerializeField] private List<AssetTemplate> _assets = new(); // List of available asset templates
        [SerializeField] private GameObject _stokageAssets;          // Parent for active assets
        [SerializeField] private GameObject _stokageAssetsUseless;   // Parent for deleted/inactive assets
        [SerializeField] private LayerMask _groundLayer;              // Layer mask for ground detection
        [SerializeField] private bool _deleteMode = false;            // Toggle delete mode
        [Range(1f, 10f)]
        [SerializeField] private float _density = 4f;                 // Density of asset placement
        [SerializeField] private int _revertNumber = 10;              // Number of revert in memory
        #endregion

        #region Private Fields
        private int _groundLayerMask; // Cached ground layer mask
        private Vector3 _pos;          // Current brush position
        #endregion

        #region Public Properties
        public float _circleRadius = 5f; // Radius of the brush circle
        [HideInInspector] public bool _lastActionWasAdd = true; // If action was add Asset = true else false
        public List<AssetTemplate> Assets => _assets;
        public GameObject StokageAssets => _stokageAssets;
        public GameObject StokageAssetsUseless => _stokageAssetsUseless;
        public LayerMask GroundLayer => _groundLayer;
        public bool DeleteMode => _deleteMode;
        public float Density => _density;
        public int GroundLayerMask => _groundLayerMask;
        public Vector3 Pos => _pos;
        public float RaycastDistance => RAYCAST_DISTANCE;
        public int RevertNumber => _revertNumber;
        public void SetPos(Vector3 pos) { _pos = pos; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
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
            Handles.color = Color.white;

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