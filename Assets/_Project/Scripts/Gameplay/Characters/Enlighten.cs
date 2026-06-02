using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// This is the Enlighten spell, that called by SkillNoteManager to switch the Lueur's light
    /// </summary>
    public class Enlighten : MonoBehaviour
    {
        #region SerializeFields

        [SerializeField] private Light _light;
        [Range(0.0f, 10.0f)]
        [SerializeField] private float _minIntensity = 0.0f;
        [Range(0.0f, 20.0f)]
        [SerializeField] private float _maxIntensity = 12.0f;

        #endregion

        #region PublicMethods

        public void IncreaseLight()
        {
            _light.intensity = _maxIntensity;
        }

        public void ReduceLight()
        {
            _light.intensity = _minIntensity;
        }

        #endregion
    }
}
