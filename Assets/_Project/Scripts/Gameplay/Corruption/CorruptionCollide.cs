using UnityEngine;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Gameplay
{
    public class CorruptionCollide : MonoBehaviour
    {
        private BoxCollider box;

        private GrayZone gz;
        private void Awake()
        {
            box = GetComponent<BoxCollider>();
            gz = GetComponent<GrayZone>();
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.gameObject.CompareTag("Player"))
            {

            }
            else
            {
                if (other.gameObject.TryGetComponent(out PostProcessEffects effects))
                {
                    Vector3 localPos = transform.InverseTransformPoint(other.transform.position);

                    Vector3 halfSize = box.size * 0.5f;

                    float x = Mathf.Abs(localPos.x) / halfSize.x;
                    float z = Mathf.Abs(localPos.z) / halfSize.z;

                    float distance = Mathf.Max(x, z);

                    float intensity = 1f - Mathf.Clamp01(distance);

                    intensity = Mathf.Lerp(0.2f, 1f, intensity);
                    effects.SetCorruptionEffect(intensity);
                }
                else
                {
                    Debug.LogError("PostProccesEffects script not in the player");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.gameObject.CompareTag("Player"))
            {

            }
            else
            {
                if (other.gameObject.TryGetComponent(out PostProcessEffects effects))
                {
                    effects.SetCorruptionEffect(0f);
                    gz.PaintFromPoint(other.transform.position, targetRadius: 4f, duration: 6f);

                }
                else
                {
                    Debug.LogError("PostProccesEffects script not in the player");
                }
            }
        }
    }
}
