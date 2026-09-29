using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    public static class PuzzleLucioleTeleport
    {
        private const string PLAYER_TAG = "Player";
        private const float GROUND_PROBE_HEIGHT = 10.0f;
        private const float GROUND_PROBE_LENGTH = 50.0f;
        private const float GROUND_MARGIN = 0.2f;

        public static bool TeleportNear(Transform target, float distance)
        {
            if (target == null) return false;

            GameObject player = GameObject.FindGameObjectWithTag(PLAYER_TAG);

            if (player == null)
            {
                Debug.LogWarning("[PuzzleLucioleTeleport] Aucun objet tague Player dans la scene.");
                return false;
            }

            Vector3 side = target.right;
            side.y = 0.0f;

            if (side.sqrMagnitude < 0.01f)
            {
                side = Vector3.right;
            }

            Vector3 dest = FindGround(target.position + side.normalized * distance);

            Rigidbody rb = player.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = dest;
            }

            player.transform.position = dest;
            Physics.SyncTransforms();

            return true;
        }

        private static Vector3 FindGround(Vector3 point)
        {
            Vector3 start = point + Vector3.up * GROUND_PROBE_HEIGHT;

            if (!Physics.Raycast(start, Vector3.down, out RaycastHit hit, GROUND_PROBE_LENGTH, ~0, QueryTriggerInteraction.Ignore))
            {
                return point;
            }

            return hit.point + Vector3.up * GROUND_MARGIN;
        }
    }
}
