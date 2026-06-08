using System;
using System.Collections;
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
        [SerializeField] private float _transitionDelay = 1.0f;

        #endregion

        #region PrivateFields

        private LayerMask _layerMask;

        #endregion

        #region UnityLifecycle

        private void Start()
        {
            _layerMask = LayerMask.GetMask("GrowingVegetal");
        }

        #endregion

        #region PublicMethods

        public void IncreaseLight()
        {
            StartCoroutine(SwitchLight(_transitionDelay, true));
        }

        public void ReduceLight()
        {
            StartCoroutine(SwitchLight(_transitionDelay, false));
        }

        #endregion

        #region Coroutines

        //Increase or decrease light intensity depending on context (isIncreased boolean)
        //It's a smooth transition (SmoothStep) startIntensity to targetIntensity (0 or 12) delayed by time
        //and then secure float values the light intensity by setting up to the target
        IEnumerator SwitchLight(float delay, bool isIncreased)
        {
            float startIntensity = _light.intensity;
            float elapsedTime = 0f;
            float targetIntensity;

            if (isIncreased)
                targetIntensity = _maxIntensity;
            else
                targetIntensity = _minIntensity;

            while (elapsedTime < delay)
            {
                _light.intensity = Mathf.SmoothStep(startIntensity, targetIntensity, elapsedTime / delay);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (isIncreased) 
                ImpactEntities();

            _light.intensity = targetIntensity;
        }

        private void ImpactEntities()
        {
            Collider[] colliders = Physics.OverlapSphere(_light.transform.position, 100.0f, _layerMask);

            Debug.Log(_light.transform.position);
            for (int i = 0; i < colliders.Length; i++)
            {
                //TODO: If there's not much LD element that require this check, I'll rework that into check list instead of TryGetComponent that is pretty bad optimizly speaking
                if (colliders[i].transform.gameObject.TryGetComponent<GrowthByLight>(out GrowthByLight growth))
                {
                    growth.Growth();
                }
            }
        }

        #endregion
    }
}
