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
        [Range(0.0f, 20.0f)]
        [SerializeField] private float _lightRange = 2.0f;

        #endregion

        #region PrivateFields

        private LayerMask _layerMask;
        private bool _isLightActivated;

        #endregion


        #region UnityLifecycle

        private void Awake()
        {
            _layerMask = LayerMask.GetMask("LightSensitive");
        }

        #endregion

        #region PublicMethods

        public void IncreaseLight()
        {
            if (_isLightActivated) return;

            StartCoroutine(SwitchLight(_transitionDelay, true));
            DetectEntities(true);
            _isLightActivated = true;
        }

        public void ReduceLight()
        {
            if (_isLightActivated == false) return;

            StartCoroutine(SwitchLight(_transitionDelay, false));
            DetectEntities(false);
            _isLightActivated = false;
        }

        #endregion

        #region PrivateMethods

        //Use sphere raycast to detect 
        private void DetectEntities(bool isIncreasing)
        {
            Ray ray = new(transform.position, transform.TransformDirection(Vector3.forward));

            Collider[] colliders = Physics.OverlapSphere(transform.position, _lightRange, _layerMask);

            for (int i = 0; i < colliders.Length; i++)
            {
                //TODO: If there's not much LD element that require this check, I'll rework that into check list instead of TryGetComponent that is pretty bad optimizly speaking
                if (colliders[i].transform.gameObject.TryGetComponent<LightReaction>(out LightReaction lightReaction))
                {
                    if (isIncreasing)
                    {
                        lightReaction.PerformLight();
                    }
                    else
                    {
                        lightReaction.PerformUnlight();
                    }
                }
            }
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

            _light.intensity = targetIntensity;
        }

        #endregion

        #region Editor

        private void OnDrawGizmos()
        {
            // Set the color with custom alpha.
            Gizmos.color = new Color(1f, 0f, 0f, 1.0f); // Red with custom alpha

            // Draw the sphere.
            Gizmos.DrawSphere(transform.position, _lightRange);

            // Draw wire sphere outline.
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, _lightRange);
        }

        #endregion
    }
}
