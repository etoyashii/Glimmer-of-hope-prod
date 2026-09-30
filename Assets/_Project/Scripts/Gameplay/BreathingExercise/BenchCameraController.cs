using UnityEngine;
namespace GlimmerOfHope.Gameplay
{
    public class BenchCameraController : MonoBehaviour
    {
        #region Public fields
        [Header("Bench camera pose")]
        public Transform BenchCameraPose;
        [Header("Camera references")]
        public Camera BenchCamera; // dedicated camera, disabled by default
        #endregion

        #region Private Properties
        private Camera _previousMainCamera;
        private Transform _previousCameraParent;
        #endregion
        
        #region Public Methods
        public void ActivateBenchCamera()
        {
            if (BenchCamera == null || BenchCameraPose == null) return;
            // Position the camera at the configured pose
            BenchCamera.transform.position = BenchCameraPose.position;
            BenchCamera.transform.rotation = BenchCameraPose.rotation;
            // Save and disable the current main camera
            _previousMainCamera = Camera.main;
            if (_previousMainCamera != null)
                _previousMainCamera.gameObject.SetActive(false);
            BenchCamera.gameObject.SetActive(true);
        }
        public void DeactivateBenchCamera()
        {
            if (BenchCamera != null)
                BenchCamera.gameObject.SetActive(false);
            if (_previousMainCamera != null)
                _previousMainCamera.gameObject.SetActive(true);
        }
        #endregion
    }
}