using GlimmerOfHope.Gameplay.Character.SpecialActions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineInternal;

namespace GlimmerOfHope.Gameplay
{
    #region Dependencies

    [RequireComponent(typeof(CharacterController))]

    #endregion

    /// <summary>
    /// Script use when the player trigger a fall zone to return on a plateform
    /// On the player with a CharacterController
    /// </summary>
    public class WindReturn : MonoBehaviour
    {
        #region Public properties

        public float windDuration = 0.6f;
        public float positionDelay = 0.4f;
        public AnimationCurve windCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float savePosDelay = 0.4f;

        #endregion

        #region Private Properties

        private Queue<(Vector3 pos, float time)> _positionBuffer = new Queue<(Vector3, float)>(); 
        private Vector3 _lastSafePos;
        private bool _isReturning;
        private CharacterController _cc;
        private Movement _movement;
        private float _savePosCooldown = 0f;

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            _cc = GetComponent<CharacterController>();
            _movement = GetComponent<Movement>();
            _lastSafePos = transform.position;
        }

        void Update()
        {
            if (_savePosCooldown > 0f) _savePosCooldown -= Time.deltaTime;

            if (_cc.isGrounded && !_isReturning && _savePosCooldown <= 0f)
            {
                float now = Time.time;

                _positionBuffer.Enqueue((transform.position, now));

                while (_positionBuffer.Count > 1 &&
                       now - _positionBuffer.Peek().time >= positionDelay)
                {
                    _lastSafePos = _positionBuffer.Dequeue().pos;
                }
            }
        }

        #endregion

        #region Public Methods

        public void OnEnterKillZone()
        {
            if (!_isReturning)
            {
                StartCoroutine(ActivateReturnWind());
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator ActivateReturnWind()
        {
            _isReturning = true;

            _cc.enabled = false;
            _movement.enabled = false;

            Vector3 startPos = transform.position;
            Vector3 endPos = _lastSafePos;

            float arcHeight = Mathf.Max(3f, Mathf.Abs(endPos.y - startPos.y) * 1.2f);
            Vector3 controlPoint = new Vector3(
                (startPos.x + endPos.x) / 2f,
                Mathf.Max(startPos.y, endPos.y) + arcHeight,
                (startPos.z + endPos.z) / 2f
                );

            float t = 0f;

            while (t < windDuration)
            {
                t += Time.deltaTime;
                float ratio = windCurve.Evaluate(t / windDuration);
                //transform.position = Vector3.Lerp(startPos, lastSafePos, ratio);
                transform.position = QuadraticBezier(startPos, controlPoint, endPos, ratio);
                yield return null;
            }

            transform.position = _lastSafePos;
            _cc.enabled = true;
            _movement.enabled = true;
            _savePosCooldown = savePosDelay;
            _isReturning = false;
        }

        private Vector3 QuadraticBezier(Vector3 a, Vector3 control, Vector3 b, float t)
        {
            float u = 1f - t;
            return (u * u * a) + (2f * u * t * control) + (t * t * b);
        }

        #endregion
    }
}
