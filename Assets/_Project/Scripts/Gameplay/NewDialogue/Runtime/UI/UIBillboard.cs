using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Drop this on a world-space Canvas (floating button, dialogue bubble) so it always faces the camera
    /// </summary>
    public class UIBillboard : MonoBehaviour
    {
        #region Private Fields

        private Camera _camera;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            transform.forward = _camera.transform.forward;
        }

        #endregion
    }
}
