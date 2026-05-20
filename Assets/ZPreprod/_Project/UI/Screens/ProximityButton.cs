using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.UI
{
    public class ProximityButton : MonoBehaviour
    {
        #region SerializeFields

        [SerializeField] private GameObject _targetObject;
        [SerializeField] private Button _specialActionButton;
        [SerializeField] private float _interactionDistance = 3f;
        [SerializeField] private Transform _player;
        [SerializeField] private Vector3 _offset;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            //_specialActionButton.onClick.AddListener(OnSpecialAction);
            _specialActionButton.gameObject.SetActive(false);
        }

        private void Update()
        {
            float distance = Vector3.Distance(_player.position, _targetObject.transform.position);
            bool isVisible = distance <= _interactionDistance;

            _specialActionButton.gameObject.SetActive(isVisible);

            if (isVisible)
            {
                Vector3 midPointWorld = (_player.position + _targetObject.transform.position) / 2f;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(midPointWorld);

                _specialActionButton.transform.position = screenPos + _offset;
            }
        }

        #endregion

        #region Private Methods

        //private void OnSpecialAction()
        //{
        //    Debug.Log("Special action : " + _targetObject.name);
        //}

        #endregion
    }
}
