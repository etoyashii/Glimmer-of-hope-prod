using UnityEngine;

namespace GlimmerOfHope.Gameplay.Firefly
{
    public partial class FireflyAI
    {
        #region Gizmos

        private void OnDrawGizmos()
        {
            if (_lanternTransform != null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _wakeRadius);

            DrawOrbitPreview();

            if (_lanternTransform == null) return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(LanternEntryPoint, _lanternRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, LanternEntryPoint);
            Gizmos.DrawWireSphere(LanternEntryPoint, _arriveThreshold);
        }

        private void DrawOrbitPreview()
        {
            Vector3 center = transform.position;

            if (_context != null && _context.Player != null)
            {
                center = _context.PlayerCenter;
            }

            center += Vector3.up * _orbitHeightOffset;

            Gizmos.color = new Color(1.0f, 0.85f, 0.3f);

            int steps = 64;
            Vector3 prev = OrbitPoint(center, 0.0f);

            for (int i = 1; i <= steps; i++)
            {
                float rad = i * Mathf.PI * 2.0f / steps;
                Vector3 next = OrbitPoint(center, rad);

                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        private Vector3 OrbitPoint(Vector3 center, float rad)
        {
            float bob = Mathf.Sin(rad * _orbitWaveCount) * _orbitWaveHeight;

            return center + new Vector3(Mathf.Cos(rad) * _orbitRadius, bob, Mathf.Sin(rad) * _orbitRadius);
        }

        #endregion
    }
}
