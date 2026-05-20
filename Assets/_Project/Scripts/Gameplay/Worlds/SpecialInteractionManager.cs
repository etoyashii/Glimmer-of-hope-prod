using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class SpecialInteractionManager : MonoBehaviour
    {
        #region State

        public enum ObjectType
        {
            None,
            Stele,
            NPC
        }

        #endregion

        #region SerializeFields

        [Header("References")]
        [SerializeField] private GameObject _codexPanel;
        [SerializeField] private GameObject _testStele; //require refacto for a better global system


        [Header("Cam Settings")]
        [SerializeField] private Camera _playerCamera;

        [SerializeField] private float _focusDistance = 3f;
        [SerializeField] private float _focusHeight = 1f;
        //[SerializeField] private Vector3 _focusOffset = new(0f, 0f, 0f); sadly broken
        [Range(1.5f, 10f)]
        [SerializeField] private float _smoothSpeed = 5f;
        [SerializeField] private float _focusDuration = 3f;
        [SerializeField] private float _unfocusDuration = 3f;
        [SerializeField] private bool _isLeftOffset = true;

        #endregion

        #region Private Fields

        private ObjectType _currentObjectType;

        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Coroutine _focusCoroutine;

        private float _screenRightOffset = 0.75f;
        private float _screenLeftOffset = 0.25f;

        #endregion

        #region Public Methods

        public void OnSteleInteract()
        {
            SpecialInteraction(ObjectType.Stele);
        }

        public void OnNPCInteract()
        {
            SpecialInteraction(ObjectType.NPC);
        }
        
        public void OpenInteraction()
        {
            Time.timeScale = 0f;

            CameraFocus(_testStele);
        }
        
        public void ExitInteraction()
        {
            StartCoroutine(UnfocusRoutine());
            ToggleCodex();
            ResetCurrentObjectType();

            Time.timeScale = 1f;
        }

        #endregion

        #region Private Methods

        private void SpecialInteraction(ObjectType speInter)
        {
            if (_currentObjectType != ObjectType.None) return;

            _currentObjectType = speInter;

            switch (speInter)
            {
                case ObjectType.Stele:
                    OpenInteraction();
                    ToggleCodex();
                    print("Stele");
                    break;

                case ObjectType.NPC:
                    print("NPC");
                    break;
            }
        }

        private void ToggleCodex()
        {
            _codexPanel.SetActive(!_codexPanel.activeSelf);
        }

        private void CameraFocus(GameObject target)
        {
            if (_focusCoroutine != null)
                StopCoroutine(_focusCoroutine);

            _originalPosition = _playerCamera.transform.position;
            _originalRotation = _playerCamera.transform.rotation;

            _focusCoroutine = StartCoroutine(FocusRoutine());
        }

        private void ResetCurrentObjectType()
        {
            _currentObjectType = ObjectType.None;
        }

        private Vector3 CalculateCameraOffsetPosition(Transform target, float distance, float height, float horizontalPercent)
        {
            Vector3 forward = (target.position - _playerCamera.transform.position).normalized;

            Vector3 basePosition = target.position - forward * distance + Vector3.up * height;

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            float offsetFactor = (horizontalPercent - 0.5f) * 2f;
            float horizontalOffset = offsetFactor * distance;

            return basePosition + right * horizontalOffset;
        }

        #endregion

        #region Coroutine

        private IEnumerator FocusRoutine()
        {
            float sideOffset = _isLeftOffset ? _screenLeftOffset : _screenRightOffset;
            Vector3 targetPosition = CalculateCameraOffsetPosition(_testStele.transform, _focusDistance, _focusHeight, sideOffset);

            //Quaternion targetRotation = Quaternion.LookRotation(_testStele.transform.position - targetPosition);

            float elapsedTime = 0f;

            while (elapsedTime < _focusDuration)
            {
                _playerCamera.transform.position = Vector3.Lerp(_playerCamera.transform.position, targetPosition, elapsedTime);
                //_playerCamera.transform.rotation = Quaternion.Slerp(_playerCamera.transform.rotation, targetRotation, elapsedTime);
                elapsedTime += Time.fixedDeltaTime * _smoothSpeed;

                yield return null;
            }
        }

        private IEnumerator UnfocusRoutine()
        {
            float elapsedTime = 0f;

            while (elapsedTime < _unfocusDuration)
            {
                _playerCamera.transform.position = Vector3.Lerp(_playerCamera.transform.position, _originalPosition, elapsedTime);
                //_playerCamera.transform.rotation = Quaternion.Slerp(_playerCamera.transform.rotation, _originalRotation, elapsedTime);
                elapsedTime += Time.fixedDeltaTime * _smoothSpeed;

                yield return null;
            }
        }

        #endregion
    }
}
