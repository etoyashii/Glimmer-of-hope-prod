using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace GlimmerOfHope.Gameplay
{
    public class WakeUpEffect : MonoBehaviour
    {
        [Header("References")]
        public Volume volume;
        public CinemachineCamera cameraBegin;
        public CinemachineCamera cameraEnd;
        public CinemachineCamera cameraEndEnd;

        [Header("Timing")]
        public float wakeUpDuration = 4f;

        private DepthOfField dof;
        private Vignette vignette;
        private ColorAdjustments colorAdjustments;
        private LensDistortion lensDistortion;
        private CinematicBarsEffect cineBars;

        void Start()
        {
            volume.profile.TryGet(out dof);
            volume.profile.TryGet(out vignette);
            volume.profile.TryGet(out colorAdjustments);
            volume.profile.TryGet(out lensDistortion);
            volume.profile.TryGet(out cineBars);

            if (dof != null) dof.gaussianMaxRadius.value = 0f;
            if (vignette != null) vignette.intensity.value = 0f;
            if (colorAdjustments != null) colorAdjustments.saturation.value = 0f;
            if (lensDistortion != null) lensDistortion.intensity.value = 0f;
            if (cineBars != null) cineBars.barSize.value = 0;

/*            if (dof != null) dof.gaussianMaxRadius.value = 7f;
            if (vignette != null) vignette.intensity.value = 0.95f;
            if (colorAdjustments != null) colorAdjustments.saturation.value = -80f;
            if (lensDistortion != null) lensDistortion.intensity.value = -0.3f;
            if (cineBars != null) cineBars.barSize.value = 0.5f;*/

            // WakeUp();
        }

        public void WakeUp()
        {
            StartCoroutine(OpenEyes());
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

                if (cineBars != null) cineBars.barSize.value = barVal;
                if (vignette != null) vignette.intensity.value = vigVal;

                yield return null;
            }
        }

        IEnumerator OpenEyes()
        {
            float t;

            //tenta 1
            yield return StartCoroutine(StruggleOpen(0.5f, 0.25f, 0.95f, 0.55f,
                                                     duration: wakeUpDuration * 0.2f,
                                                     shakeMag: 0.05f));

            t = 0f; float fallback1 = 0.18f;
            while (t < fallback1)
            {
                t += Time.deltaTime;
                float p = Mathf.Pow(Mathf.Clamp01(t / fallback1), 2f);
                if (cineBars != null) cineBars.barSize.value = Mathf.Lerp(0.25f, 0.44f, p);
                if (vignette != null) vignette.intensity.value = Mathf.Lerp(0.55f, 0.88f, p);
                yield return null;
            }

            //tenta 2
            yield return StartCoroutine(StruggleOpen(0.44f, 0.12f, 0.88f, 0.35f,
                                                     duration: wakeUpDuration * 0.25f,
                                                     shakeMag: 0.04f));

            t = 0f; float fallback2 = 0.12f;
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


            // Effets 
            t = 0f; float settle = wakeUpDuration * 0.1f;
            while (t < settle)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / settle);
                if (dof != null) dof.gaussianMaxRadius.value = Mathf.Lerp(7f, 0.5f, p);
                if (colorAdjustments != null) colorAdjustments.saturation.value = Mathf.Lerp(-80f, -10f, p);
                if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(-0.3f, -0.05f, p);
                yield return null;
            }

            // clignement
            t = 0f;
            while (t < 0.06f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.06f);
                cineBars.barSize.value = Mathf.Lerp(0.05f, 0.5f, p);
                vignette.intensity.value = Mathf.Lerp(0.28f, 0.95f, p);
                yield return null;
            }
            t = 0f;
            while (t < 0.09f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.09f);
                cineBars.barSize.value = Mathf.Lerp(0.5f, 0f, p);
                vignette.intensity.value = Mathf.Lerp(0.95f, 0.2f, p);
                yield return null;
            }

            // fiin
            t = 0f; float finalPhase = 0.15f;
            while (t < finalPhase)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / finalPhase);
                if (dof != null) dof.gaussianMaxRadius.value = Mathf.Lerp(0.5f, 0f, p);
                if (colorAdjustments != null) colorAdjustments.saturation.value = Mathf.Lerp(-10f, 0f, p);
                if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(-0.05f, 0f, p);
                yield return null;
            }

            if (vignette != null) vignette.intensity.value = 0.2f;
            if (cineBars != null) cineBars.barSize.value = 0f;
            if (colorAdjustments != null) colorAdjustments.saturation.value = 0f;
            if (lensDistortion != null) lensDistortion.intensity.value = 0f;
            if (dof != null) dof.gaussianMaxRadius.value = 0f;

            yield return new WaitForSeconds(0.5f);
            cameraEnd.Priority = 0;
            yield return new WaitForSeconds(2f);
            cameraEndEnd.Priority = 0;

        }

        public IEnumerator Blink(float close, float open)
        {
            float t = 0f;
            while (t < close)
            {
                t += Time.deltaTime;
                float p = Mathf.Pow(Mathf.Clamp01(t / close), 2f);
                cineBars.barSize.value = Mathf.Lerp(0f, 0.4f, p);
                vignette.intensity.value = Mathf.Lerp(0f, 0.8f, p);
                dof.gaussianMaxRadius.value = Mathf.Lerp(0f, 7f, p);
                colorAdjustments.saturation.value = Mathf.Lerp(0f, -80f, p);
                yield return null;
            }

            t = 0f;
            while (t < open)
            {
                t += Time.deltaTime;
                float p = Mathf.Pow(Mathf.Clamp01(t / open), 2f);
                cineBars.barSize.value = Mathf.Lerp(0.4f, 0f, p);
                vignette.intensity.value = Mathf.Lerp(0.8f, 0f, p);
                dof.gaussianMaxRadius.value = Mathf.Lerp(7f, 0f, p);
                colorAdjustments.saturation.value = Mathf.Lerp(-80f, 0f, p);
                yield return null;
            }
        }
    }
}