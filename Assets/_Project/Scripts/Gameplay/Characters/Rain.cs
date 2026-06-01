using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// This is the Rain spell, that invoke an event Action for all objects on sphere range if they have Specific Layer setted  
    /// </summary>
    public class Rain : MonoBehaviour
    {
        #region SerializeFields

        [SerializeField] private float _delay;
        [SerializeField] private SkillNoteManager _skillNoteManager;

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
                if (raycastHits[i].transform.gameObject.TryGetComponent<WeatherEffect>(out WeatherEffect weather))
                {
                    weather.ApplyEffect(WeatherEffect.WeatherEffectType.Rainy);
                }
            }
        }

        #endregion
    }
}
