using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// This is the Shine spell, that try getting WeatherEffect script component for all objects on sphere range if they have Specific Layer setted and then call the effect
    /// </summary>
    public class Shine : MonoBehaviour
    {
        #region SerializeFields

        [SerializeField] private float _delay;

        #endregion

        #region PrivateFields

        private LayerMask _layerMask;

        #endregion


        #region UnityLifecycle

        private void Awake()
        {
            _layerMask = LayerMask.GetMask("WeatherSensitive");
        }

        #endregion

        #region PublicMethods

        public void UseSkill()
        {
            Ray ray = new(transform.position, transform.TransformDirection(Vector3.forward));

            RaycastHit[] raycastHits = Physics.SphereCastAll(ray, 10.0f, 20.0f, _layerMask);

            for (int i = 0; i < raycastHits.Length; i++)
            {
                //TODO: If there's not much LD element that require this check, I'll rework that into check list instead of TryGetComponent that is pretty bad optimizly speaking
                if (raycastHits[i].transform.gameObject.TryGetComponent<WeatherEffect>(out WeatherEffect weather))
                {
                    weather.ApplyEffect(WeatherEffect.WeatherEffectType.Sunny);
                }
            }
        }

        #endregion
    }
}
