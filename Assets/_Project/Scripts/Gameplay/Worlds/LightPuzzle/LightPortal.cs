using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Create a light beam for a puzzle
    /// </summary>
    public class LightPortal : MonoBehaviour
    {
        #region Serialize Field

        [SerializeField] private float _maxDistance = 10f;
        [SerializeField] private int _maxReflection = 5;
        [SerializeField] private GameObject _lightPrefab;
        [SerializeField] private LayerMask _obstacleMask;
        [SerializeField] private UnityEvent _win;

        #endregion

        #region Private Properties

        private List<Transform> _lightPool = new List<Transform>();

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            //create all the gameobject one time on start
            for (int i = 0; i < _maxReflection; i++)
            {
                GameObject light = Instantiate(_lightPrefab);
                light.SetActive(false);
                _lightPool.Add(light.transform);
            }
        }

        void FixedUpdate()
        {
            SimulateBeam();
        }

        #endregion

        #region Private Methodes

        private void SimulateBeam()
        {
            Vector3 currentPos = transform.position;
            Vector3 currentDir = transform.forward;
            float remainingDistance = _maxDistance;

            int segmentIndex = 0;

            while (segmentIndex < _lightPool.Count && remainingDistance > 0f)
            {
                Transform segment = _lightPool[segmentIndex];
                float segmentLength = remainingDistance;

                //move a little to avoid collision with the same surface
                Vector3 rayOrigin = currentPos + currentDir * 0.001f;

                bool hasHit = Physics.Raycast(rayOrigin, currentDir, out RaycastHit hit, remainingDistance, _obstacleMask);
                if (hasHit)
                {
                    segmentLength = hit.distance;

                    //if we hit the finish collider we dont reflect the light
                    if (hit.rigidbody.gameObject.CompareTag("Finish"))
                    {
                        hasHit = false;
                        //call win
                        _win?.Invoke();
                    }
                }

                segment.gameObject.SetActive(true);
                ApplySegmentTransform(segment, currentPos, currentDir, segmentLength);

                segmentIndex++;

                if (!hasHit)
                {
                    // nothing hit so we stop
                    break;
                }

                currentPos = hit.point;
                currentDir = Vector3.Reflect(currentDir, hit.normal);
            }

            for (int i = segmentIndex; i < _lightPool.Count; i++)
            {
                _lightPool[i].gameObject.SetActive(false);
            }
        }

        private void ApplySegmentTransform(Transform segment, Vector3 origin, Vector3 direction, float length)
        {
            //set the correct length 
            Vector3 scale = segment.localScale;
            scale.y = length / 2f;
            segment.localScale = scale;

            //set the correct position to have a static face
            segment.position = origin + direction * (length / 2f);

            //rotate to look at the correct direction
            segment.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        }

        #endregion
    }
}