using System;
using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core;
using GlimmerOfHope.Editor;

namespace GlimmerOfHope.Gameplay
{

    /// <summary>
    /// 
    /// </summary>
    public class DetectTarget : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Detection")]
        [Slider(0.0f, 180.0f, SliderColor.Blue)]
        [SerializeField] private float _angleThreshold = 45.0f;
        [Tooltip("There's a square value so if it's 4, the minimum distance is 2")]
        [Slider(0.0f, 400.0f, SliderColor.Green)]
        [SerializeField] private float _minSqrDistance = 4.0f;
        [Tooltip("There's a square value so if it's 100, the maximum distance is 10")]
        [Slider(0.0f, 400.0f, SliderColor.Cyan)]
        [SerializeField] private float _maxSqrDistance = 100.0f;

        [Header("Target reference")]
        [SerializeField] private GameObject _target;

        private Vector3 _direction;
        private Vector3 _targetPosXZ;

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            UpdateTargetPos();
        }

        #endregion

        #region Public Methods

        public bool IsTargetInSight()
        {
            if (Vector3.Angle(_direction, transform.forward) < _angleThreshold)
                return true;

            return false;
        }

        public bool IsTargetInRange()
        {
            //using squared values here because it reduce the calcul cost
            if (_direction.sqrMagnitude > _minSqrDistance && _direction.sqrMagnitude < _maxSqrDistance)
                return true;

            return false;
        }

        public void UpdateTargetPos()
        {
            if (_target == null) return;

            _targetPosXZ = new(_target.transform.position.x, transform.position.y, _target.transform.position.z);
            _direction = _targetPosXZ - transform.position;
        }

        public void LookTarget()
        {
            transform.LookAt(_targetPosXZ);
        }

        public Vector3 GetTargetPosXZ()
        {
            return _targetPosXZ;
        }

        #endregion


        private void OnDrawGizmos()
        {
            if (_target == null) return;

            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position;

            Gizmos.DrawLine(origin, _targetPosXZ);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, Mathf.Sqrt(_maxSqrDistance));

            Gizmos.color = Color.green;
            int segments = 20;
            float maxDistance = Mathf.Sqrt(_maxSqrDistance);
            float angle = _angleThreshold;

            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = -angle * 0.5f + (angle * i / segments);
                float radians = currentAngle * Mathf.Deg2Rad;

                Vector3 localDirection = new Vector3(
                    Mathf.Sin(radians),
                    0,
                    Mathf.Cos(radians)
                );

                Vector3 worldDirection = transform.TransformDirection(localDirection);

                Gizmos.DrawLine(origin, origin + worldDirection * maxDistance);
            }
        }

    }
}