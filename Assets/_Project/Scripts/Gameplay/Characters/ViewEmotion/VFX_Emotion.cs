using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Script to manage the vfx link to the emotion of a npc
    /// </summary>
    public class VFX_Emotion : MonoBehaviour
    {
        [SerializeField] private GlobalValueShader _manager;

        [SerializeField] private VisualEffect _vfx;
        [ColorUsage(showAlpha:true, hdr: true)]
        [SerializeField] private Color _color;
        [Range(0f, 1f)]
        [SerializeField] private float _amplitude = 0.01f;
        [SerializeField] private float _frequency = 1f;
        [SerializeField] private float _rotationSpeed = 1f;
        [SerializeField] private float _rotationRange = 90f;

        // Update is called once per frame
        void Update()
        {
            _vfx.SetVector4("Color", _color);
            _vfx.SetFloat("Amplitude", _amplitude);
            _vfx.SetFloat("Frequency", _frequency);
            _vfx.SetFloat("RotationSpeed", _rotationSpeed);
            _vfx.SetFloat("RotationRange", _rotationRange);

            float distance = Vector3.Distance(transform.position, _manager.playerTransform.position);
            float currentCircleSize = _manager.currentPropRadius * _manager.radiusBigCircle;

            if (distance <= currentCircleSize && _manager.viewEmotionIsActive)
            {
                _vfx.gameObject.SetActive(false);
            }
            else
            {
                _vfx.gameObject.SetActive(true);
            }
        }
    }
}
