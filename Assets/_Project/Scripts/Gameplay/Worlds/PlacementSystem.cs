using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class PlacementSystem : MonoBehaviour
    {
        #region SerializeFields

        [SerializeField] private GameObject _mouseIndicator;
        [SerializeField] private GameObject _cellIndicator;
        [SerializeField] private Grid _grid;
        [SerializeField] private InputManager _inputManager;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            Vector3 mousePosition = _inputManager.GetSelectedMapPosition();
            Vector3Int gridPosition = _grid.WorldToCell(mousePosition);
            _mouseIndicator.transform.position = mousePosition;
            _cellIndicator.transform.position = _grid.CellToWorld(gridPosition);
        }

        #endregion
    }
}
