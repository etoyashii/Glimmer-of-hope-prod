using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class OutOfBoundsRespawn : MonoBehaviour
    {
        private static HashSet<OutOfBoundsRespawn> Instances = new HashSet<OutOfBoundsRespawn>();

        public static Vector3 GetNearestRespawnPosition(Vector3 position)
        {
            float minDist = Mathf.Infinity;
            Vector3 minDistPos = Vector3.zero;

            float testDist = 0f;
            Vector3 testPos = Vector3.zero;
            foreach (OutOfBoundsRespawn outOfBoundsRespawn in Instances)
            {
                testPos = outOfBoundsRespawn.transform.position;
                testDist = Vector3.Distance(position, testPos);
    
                if (testDist < minDist)
                {
                    minDist = testDist;
                    minDistPos = testPos;
                }
            }

            return minDistPos;
        }

        private void Awake()
        {
            Instances.Add(this);
        }

        private void OnDestroy()
        {
            Instances.Remove(this);
        }

#if UNITY_EDITOR
        [Header("Gizmos")]
        [SerializeField] private Color gizmoColor = Color.green;
        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
#endif
    }
}
