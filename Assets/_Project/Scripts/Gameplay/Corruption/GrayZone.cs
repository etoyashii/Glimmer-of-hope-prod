using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class GrayZone : MonoBehaviour
    {
        public Vector2 size = new Vector2(10, 10);
        [Range(0, 1)]
        public float threshold = 0.5f;
        public int maskIndex;

        private void OnEnable()
        {
            GrayZoneManager.Register(this);
        }

        private void OnDisable()
        {
            GrayZoneManager.Unregister(this);
        }

        public GrayZoneData GetData()
        {
            GrayZoneData data = new GrayZoneData();
            data.worldToLocal = transform.worldToLocalMatrix.transpose;
            data.size = size;
            data.threshold = threshold;
            data.maskIndex = maskIndex;
            return data;
        }
    }
}