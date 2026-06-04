using System;
using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// The script that manage all behavior based on the light reception and which type of object contained this script
    /// </summary>
    public class LightReaction : MonoBehaviour
    {
        #region Enums

        public enum EntityType
        {
            None,
            ElevateFlower
        }

        #endregion

        #region SerializeFields

        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField] private Vector3 _velocity;
        [SerializeField] private float _movementDuration;

        [SerializeField] private EntityType _entityType;

        #endregion

        #region PrivateFields

        private bool _isMoving;
        private bool _isBacking;
        private Vector3 _targetPosition;
        private float _permissiveRange = 0.01f;

        #endregion

        #region PublicMethods

        public void ReactionToLight()
        {
            switch (_entityType)
            {
                case EntityType.None:
                    break;
                case EntityType.ElevateFlower:
                    MoveUpByLight();
                    break;
            }
        }

        #endregion

        #region UnityLifecycle

        private void Start()
        {
            if (transform.position != _startPoint.position && transform.position != _endPoint.position)
                ForcePosition(_startPoint.position);

            if (transform.position == _startPoint.position)
                _isBacking = false;
            else if (transform.position == _endPoint.position)
                _isBacking = true;

            SetTargetPosition();
        }

        private void Update()
        {
            if (_isMoving)
            {
                transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _velocity, _movementDuration);

                float distance = Vector3.Distance(transform.position, _endPoint.position);
                
                //When it's closed to target position, there's a logic changing the next target
                if (distance <= _permissiveRange && distance >= -_permissiveRange)
                {
                    ForcePosition(_endPoint.position);
                    ToggleIsBacking();
                    SetTargetPosition();

                    _isMoving = false;
                }
            }
        }

        private void ToggleIsBacking()
        {
            _isBacking = !_isBacking;
        }

        private void ForcePosition(Vector3 newPos)
        {
            transform.position = newPos;
        }

        #endregion

        #region PrivateMethods

        private void MoveUpByLight()
        {
            _isMoving = true;
        }

        private void SetTargetPosition()
        {
            //switch the target position depending on the next exepected movement
            _targetPosition = _isBacking ? _startPoint.position : _endPoint.position;
        }

        #endregion
    }
}
