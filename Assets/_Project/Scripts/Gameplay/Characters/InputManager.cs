using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private Camera _sceneCamera;
        [SerializeField] private LayerMask _placementLayerMask;

        private Vector3 _lastPosition;

        public Vector3 GetSelectedMapPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
            mousePos.y = Mathf.Clamp(mousePos.y, 0, Screen.height);
            mousePos.z = _sceneCamera.nearClipPlane;

            Ray ray = _sceneCamera.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100, _placementLayerMask))
            {
                _lastPosition = hit.point;
            }

            return _lastPosition;
        }

    }
}
