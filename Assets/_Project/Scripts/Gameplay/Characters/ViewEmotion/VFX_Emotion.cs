using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace GlimmerOfHope.Gameplay
{
    public class VFX_Emotion : MonoBehaviour
    {
        [SerializeField] private VisualEffect vfx;
        [ColorUsage(showAlpha:true, hdr: true)]
        [SerializeField] private Color color;
        [Range(0f, 1f)]
        [SerializeField] private float amplitude = 0.01f;
        [SerializeField] private float frequency = 1f;
        [SerializeField] private float rotationSpeed = 1f;
        [SerializeField] private float rotationRange = 90f;

        // Update is called once per frame
        void Update()
        {
            vfx.SetVector4("Color", color);
            vfx.SetFloat("Amplitude", amplitude);
            vfx.SetFloat("Frequency", frequency);
            vfx.SetFloat("RotationSpeed", rotationSpeed);
            vfx.SetFloat("RotationRange", rotationRange);
        }
    }
}
