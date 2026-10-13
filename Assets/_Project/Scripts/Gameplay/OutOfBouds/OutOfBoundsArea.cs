using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public class OutOfBoundsArea : MonoBehaviour
    {
        [SerializeField] private BoxCollider boxCollider;

        private void Awake()
        {
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                other.transform.position = OutOfBoundsRespawn.GetNearestRespawnPosition(other.transform.position);
            }
        }

        #if UNITY_EDITOR
        [Header("Gizmos")]
        [SerializeField] private Color gizmoColor = Color.red;
        private void OnDrawGizmos()
        {
            if (boxCollider == null)
                return;
            Gizmos.color = gizmoColor;
            Gizmos.matrix = boxCollider.transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
        #endif
    }
}