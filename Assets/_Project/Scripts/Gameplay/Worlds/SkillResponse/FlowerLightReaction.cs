using DG.Tweening.Plugins.Core.PathCore;
using GlimmerOfHope.Gameplay.Character.SpecialActions;
using System;
using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// The script that manage flower behavior based on the light reception
    /// </summary>
    public class FlowerLightReaction : LightReaction
    {
        #region Enums

        //Usefull for controlling the switch state,
        //block buggy interaction like TP the flower when moving by switching light
        public enum FlowerState
        {
            Idle,
            MovingToStart,
            MovingToEnd
        }

        private FlowerState _currentFlowerState = FlowerState.Idle;

        #endregion

        #region SerializeFields
        [Header("Movement stats")]
        [SerializeField] private float _movementDuration = 1.0f;

        //All serializeField variables after this comment is usefull for customizable curving movement
        [Header("Movement point ref")]
        [SerializeField] private Transform _startPoint;  // P0
        [SerializeField] private Transform _endPoint;    // P3

        [Header("Bezier points")]
        [SerializeField] private Transform _controlPoint1; // P1
        [SerializeField] private Transform _controlPoint2; // P2

        [SerializeField] private AnimationCurve _movementCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

        #endregion

        #region PrivateFields

        private float _currentMovementProgress;
        private float _currentCurveValue;


        #endregion

        #region UnityLifecycle

        private void Start()
        {
            //Check if the position base fits one of both start or end position. If not, Force it to the start position.
            if (Mathf.Approximately(Vector3.Distance(transform.position, _startPoint.position), 0.0f) == false &&
            Mathf.Approximately(Vector3.Distance(transform.position, _endPoint.position), 0.0f) == false)
            {
                ForcePosition(_startPoint.position);
            }
        }

        #endregion

        #region PublicMethods

        //Enlighten Skill call by detecting the parent LightReaction then launch this method (heritage)
        public override void PerformLight()
        {
            Debug.Log("Ligthed");
            if (IsStateIsCurrent(FlowerState.Idle) == false) return; //Switch security

            ChangeState(FlowerState.MovingToStart);
            StartCoroutine(Move());
        }

        //Enlighten Skill call by detecting the parent LightReaction then launch this method (heritage)
        public override void PerformUnlight()
        {
            if (IsStateIsCurrent(FlowerState.Idle) == false) return; //Switch security

            ChangeState(FlowerState.MovingToEnd);
            StartCoroutine(Move());
        }

        #endregion

        #region PrivateMethods

        private void ForcePosition(Vector3 newPos) => transform.position = newPos;

        //Calculate point on Bezier curve
        private Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            //Formula : B(t) = (1-t)³P0 + 3(1-t)²tP1 + 3(1-t)t²P2 + t³P3
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 point = uuu * p0; // (1-t)^3 * P0
            point += 3 * uu * t * p1;  // + 3*(1-t)^2*t * P1
            point += 3 * u * tt * p2;  // + 3*(1-t)*t^2 * P2
            point += ttt * p3;         // + t^3 * P3

            return point;
        }

        private void ChangeState(FlowerState flowerState)
        {
            if (flowerState == _currentFlowerState) return;

            _currentFlowerState = flowerState;
        }

        #endregion

        #region Coroutine

        IEnumerator Move()
        {
            //Resetting values
            _currentMovementProgress = 0.0f;
            _currentCurveValue = 0.0f;
            Vector3 bezierPosition = transform.position;

            //these below is to secure the flower behavior.
            //Ensuring that player can't move A to B when the start position is B, because it'll TP the flower.
            //Same logic the other case (B to A)
            if (IsStateIsCurrent(FlowerState.MovingToStart) && transform.position == _endPoint.position)
            {
                ChangeState(FlowerState.Idle);
                yield break;
            }
            else if (IsStateIsCurrent(FlowerState.MovingToEnd) && transform.position == _startPoint.position)
            {
                ChangeState(FlowerState.Idle);
                yield break;
            }

            while (_currentMovementProgress < 1.0f)
            {
                _currentMovementProgress += Time.deltaTime / _movementDuration;
                _currentMovementProgress = Mathf.Clamp01(_currentMovementProgress);

                _currentCurveValue = _movementCurve.Evaluate(_currentMovementProgress);

                //help player to follow a specific curves point
                if (IsStateIsCurrent(FlowerState.MovingToStart))
                {
                    bezierPosition = GetBezierPoint(_startPoint.position, _controlPoint1.position,
                        _controlPoint2.position, _endPoint.position, _currentCurveValue);
                }
                else if (IsStateIsCurrent(FlowerState.MovingToEnd))
                {
                    bezierPosition = GetBezierPoint(_endPoint.position, _controlPoint1.position,
                        _controlPoint2.position, _startPoint.position, _currentCurveValue);
                }

                transform.position = bezierPosition;

                yield return null;
            }

            ForcePosition(IsStateIsCurrent(FlowerState.MovingToEnd) ? _startPoint.position : _endPoint.position);
            ChangeState(FlowerState.Idle);
        }

        #endregion

        #region Helpers

        private bool IsStateIsCurrent(FlowerState state)
        {
            return state == _currentFlowerState;
        }

        #endregion

        #region Editor

        //Show the curve on editor, helping Level Designers to make a good flower movement
        private void OnDrawGizmosSelected()
        {
            if (_startPoint == null || _endPoint == null || _controlPoint1 == null || _controlPoint2 == null) return;

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_startPoint.position, 0.1f);
            Gizmos.DrawSphere(_endPoint.position, 0.1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_controlPoint1.position, 0.1f);
            Gizmos.DrawSphere(_controlPoint2.position, 0.1f);

            Gizmos.color = Color.white;
            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20.0f;
                Vector3 point = GetBezierPoint(
                    _startPoint.position,
                    _controlPoint1.position,
                    _controlPoint2.position,
                    _endPoint.position,
                    t
                );
                Gizmos.DrawSphere(point, 0.05f);
            }
        }

        #endregion

    }
}
