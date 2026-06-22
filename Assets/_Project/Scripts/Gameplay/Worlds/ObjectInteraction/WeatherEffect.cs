using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// The WeatherEffect, that decide the action depending on the ObjectType and WeatherEffectType. It has to be attached on each 
    /// </summary>
    public class WeatherEffect : MonoBehaviour
    {
        public enum WeatherEffectType
        {
            None,
            Rainy,
            Sunny
        }

        public enum ObjectType
        {
            None,
            Seed,
            Ice,
            Lava //etc
        }

        [SerializeField] private ObjectType _objectType;
        public void ApplyEffect(WeatherEffectType weatherType) 
        {
            switch (weatherType)
            {
                case WeatherEffectType.Rainy:
                    RainEffect();
                    break;
                case WeatherEffectType.Sunny:
                    SunnyEffect();
                    break;
            }
        }

        private void RainEffect()
        {
            switch (_objectType)
            {
                case ObjectType.Seed:
                    Debug.Log("Rain on seed.");
                    break;
                case ObjectType.Ice:
                    Debug.Log("Rain on Ice.");
                    break;
                case ObjectType.Lava:
                    Debug.Log("Rain on Lava.");
                    break;
            }
        }

        private void SunnyEffect()
        {
            switch (_objectType)
            {
                case ObjectType.Seed:
                    Debug.Log("Sun on seed.");
                    break;
                case ObjectType.Ice:
                    Debug.Log("Sun on Ice.");
                    break;
                case ObjectType.Lava:
                    Debug.Log("Sun on Lava.");
                    break;
            }
        }
    }
}
