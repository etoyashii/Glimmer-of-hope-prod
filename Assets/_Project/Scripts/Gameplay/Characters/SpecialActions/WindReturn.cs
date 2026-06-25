using GlimmerOfHope.Gameplay.Character.SpecialActions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineInternal;

namespace GlimmerOfHope.Gameplay
{
    #region Dependencies

    //needed for the isgrounded
    [RequireComponent(typeof(Rigidbody))]

    #endregion

    /// <summary>
    /// Script use when the player trigger a fall zone to return on a plateform
    /// On the player with a CharacterController
    /// </summary>
    public class WindReturn : MonoBehaviour
    {
        #region SerializeField

        [Tooltip("The max accepted angle to save the position of the gameobject.")]
        [SerializeField] private float maxAngleSavePos = 20f;

        #endregion

        #region Public properties

        //the total time of the return. Depends on the distance
        public float windDuration = 0.6f;
        public float baseDistance = 5f;

        //delay so we dont return at the edge of the plateform
        public float positionDelay = 0.4f;

        //curve use to move the object from one point to the other
        public AnimationCurve windCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        //start to save new pos after you return from a fall
        public float savePosDelay = 0.4f;

        #endregion

        #region Private Properties

        //queue used to save position depending on the time. We only use the recents one
        private Queue<(Vector3 pos, float time)> _positionBuffer = new Queue<(Vector3, float)>();

        //last pos save while being on the ground
        private Vector3 _lastSafePos;

        //if we are currently in the return animation
        private bool _isReturning;

        private Rigidbody _rb;
        private Movement _movement;
        private float _savePosCooldown = 0f;

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _movement = GetComponent<Movement>();
            _lastSafePos = transform.position;
        }

        void Update()
        {
            if (_savePosCooldown > 0f) _savePosCooldown -= Time.deltaTime;

            if (_movement.IsGrounded() && !_isReturning && _savePosCooldown <= 0f)
            {
                //check the angle if its correct
                if (Vector3.Angle(Vector3.up, _movement.lastHit.normal) > maxAngleSavePos) return;

                float now = Time.time;

                _positionBuffer.Enqueue((transform.position, now)); //add to the queue with the time

                while (_positionBuffer.Count > 1 &&
                       now - _positionBuffer.Peek().time >= positionDelay)
                {
                    _lastSafePos = _positionBuffer.Dequeue().pos; //update last safe pos with the end value of the queue
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

            //to avoid any movement during the animation
            _rb.useGravity = false;
            _movement.enabled = false;

            Vector3 startPos = transform.position;
            Vector3 endPos = _lastSafePos;

            //needed for the bezier curve
            float arcHeight = Mathf.Max(3f, Mathf.Abs(endPos.y - startPos.y) * 1.2f);
            Vector3 controlPoint = new Vector3(
                (startPos.x + endPos.x) / 2f,
                Mathf.Max(startPos.y, endPos.y) + arcHeight,
                (startPos.z + endPos.z) / 2f
                );

            float t = 0f;
            float distance = Vector3.Distance(startPos, endPos);
            float duration = windDuration * (distance / baseDistance);

            while (t < duration)
            {
                t += Time.deltaTime;
                float ratio = windCurve.Evaluate(t / duration);
                transform.position = QuadraticBezier(startPos, controlPoint, endPos, ratio);
                yield return null;
            }

            transform.position = endPos;
            _rb.useGravity = true;
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
