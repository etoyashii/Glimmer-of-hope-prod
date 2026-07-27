using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GlimmerOfHope.Gameplay
{
    public class PostProcessEffects : MonoBehaviour
    {
        [Header("References")]
        public Volume volume;
        public CinemachineCamera cameraBegin;
        public CinemachineCamera cameraEnd;
        public CinemachineCamera cameraEndEnd;

        [Header("Timing")]
        [SerializeField]
        private float wakeUpDuration = 4f;

        private DepthOfField dof;
        private Vignette vignette;
        private ColorAdjustments colorAdjustments;
        private CinematicBarsEffect cineBars;

        void Start()
        {
            volume.profile.TryGet(out dof);
            volume.profile.TryGet(out vignette);
            volume.profile.TryGet(out colorAdjustments);
            volume.profile.TryGet(out cineBars);
        }

        public void Flash(float openTime, float closeTime)
        {
            CloseEyes(closeTime);
            OpenEyes(openTime);
        }

        public IEnumerator CloseEyes(float time)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / time);
                cineBars.barSize.value = Mathf.Lerp(0.05f, 0.5f, p);
                vignette.intensity.value = Mathf.Lerp(0.28f, 0.95f, p);
                yield return null;
            }
        }

        public void SetCorruptionEffect(float intensity)
        {
            if (intensity > 0)
            {
                intensity = Mathf.Clamp01(intensity);

                float eased = Mathf.SmoothStep(0f, 1f, intensity);

                cineBars.barSize.value = Mathf.Lerp(0f, 0.05f, eased);

                vignette.intensity.value = Mathf.Lerp(0f, 0.6f, eased);

                colorAdjustments.saturation.value = -Mathf.Lerp(-40f, 80f, eased);
            }
            else
            {
                cineBars.barSize.value = 0;

                vignette.intensity.value = 0;

                colorAdjustments.saturation.value = 40;
            }
            
        }
        public IEnumerator OpenEyes(float time)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / time);
                cineBars.barSize.value = Mathf.Lerp(0.5f, 0f, p);
                vignette.intensity.value = Mathf.Lerp(0.95f, 0f, p);
                yield return null;
            }
        }

        public void WakeUp()
        {
            if (dof != null) dof.gaussianMaxRadius.value = 7f;
            if (vignette != null) vignette.intensity.value = 0.95f;
            if (colorAdjustments != null) colorAdjustments.saturation.value = -80f;
            if (cineBars != null) cineBars.barSize.value = 0.5f;
            StartCoroutine(OpenEyesWakeUp());
        }

        IEnumerator StruggleOpen(float fromBar, float toBar, float fromVig, float toVig, float duration, float shakeMag = 0.04f)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);

                float eased = 1f - Mathf.Pow(1f - p, 3f);

                float shake = Mathf.Sin(t * 10f) * shakeMag * (1f - p)
                            + Mathf.Sin(t * 5f) * shakeMag * 0.1f * (1f - p);

                float barVal = Mathf.Clamp(Mathf.Lerp(fromBar, toBar, eased) + shake, 0f, 0.5f);
                float vigVal = Mathf.Clamp(Mathf.Lerp(fromVig, toVig, eased) + shake * 0.5f, 0f, 1f);

                cineBars.barSize.value = barVal;
                vignette.intensity.value = vigVal;

                yield return null;
            }
        }

        IEnumerator OpenEyesWakeUp()
        {
            float t;

            //tenta 1
            yield return StartCoroutine(StruggleOpen(0.5f, 0.25f, 0.95f, 0.55f,
                                                     duration: wakeUpDuration * 0.2f,
                                                     shakeMag: 0.05f));

            t = 0f;
            float fallback1 = 0.18f;
            while (t < fallback1)
            {
                t += Time.deltaTime;
                float p = Mathf.Pow(Mathf.Clamp01(t / fallback1), 2f);
                cineBars.barSize.value = Mathf.Lerp(0.25f, 0.44f, p);
                vignette.intensity.value = Mathf.Lerp(0.55f, 0.88f, p);
                yield return null;
            }
            OpenEyes(0.18f);

            //tenta 2
            yield return StartCoroutine(StruggleOpen(0.44f, 0.12f, 0.88f, 0.35f,
                                                     duration: wakeUpDuration * 0.25f,
                                                     shakeMag: 0.04f));

            t = 0f;
            float fallback2 = 0.12f;
            while (t < fallback2)
            {
                t += Time.deltaTime;
                float p = Mathf.Pow(Mathf.Clamp01(t / fallback2), 2f);
                cineBars.barSize.value = Mathf.Lerp(0.12f, 0.28f, p);
                vignette.intensity.value = Mathf.Lerp(0.35f, 0.65f, p);
                yield return null;
            }

            //tenta 3
            yield return StartCoroutine(StruggleOpen(0.28f, 0.05f, 0.65f, 0.28f,
                                                     duration: wakeUpDuration * 0.2f,
                                                     shakeMag: 0.02f));
            cameraBegin.Priority = 0;


            t = 0f;
            float settle = wakeUpDuration * 0.1f;
            while (t < settle)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / settle);
                dof.gaussianMaxRadius.value = Mathf.Lerp(7f, 0.5f, p);
                colorAdjustments.saturation.value = Mathf.Lerp(-80f, -10f, p);
                yield return null;
            }

            Flash(0.06f, 0.09f);

            // fiin
            t = 0f; float finalPhase = 0.15f;
            while (t < finalPhase)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / finalPhase);
                dof.gaussianMaxRadius.value = Mathf.Lerp(0.5f, 0f, p);
                colorAdjustments.saturation.value = Mathf.Lerp(-10f, 0f, p);
                yield return null;
            }

            vignette.intensity.value = 0.2f;
            cineBars.barSize.value = 0f;
            colorAdjustments.saturation.value = 0f;
            dof.gaussianMaxRadius.value = 0f;

            yield return new WaitForSeconds(0.5f);
            cameraEnd.Priority = 0;
            yield return new WaitForSeconds(2f);
            cameraEndEnd.Priority = 0;

        }
    }
}