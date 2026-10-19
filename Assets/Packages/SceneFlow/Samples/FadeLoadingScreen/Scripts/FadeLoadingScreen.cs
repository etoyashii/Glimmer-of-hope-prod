using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kebab.SceneFlow.Samples.FadeLoadingScreen
{
    public class FadeLoadingScreen : ALoadingScreen
    {
        [System.Serializable]
        public class Events
        {
            public UnityEvent onFadeInStart;
            public UnityEvent onFadeInComplete;
            public UnityEvent onFadeOutStart;
            public UnityEvent onFadeOutComplete;
            public UnityEvent onWaitingForContinue;
        }
        
        [Header("Fade Settings")]
        [Tooltip("Duration of the fade in.")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [Tooltip("Curve of the fade in.")]
        [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("Duration of the fade out.")]
        [SerializeField] private float fadeOutDuration = 0.5f;
        [Tooltip("Curve of the fade out.")]
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Header("Animation")]
        [SerializeField] private RectTransform background;
        [SerializeField] private float backgroundXDelta = 50f;
        [SerializeField] private Ease backgroundAnimEase = Ease.Linear;
        [SerializeField] private float backgroundAnimTime = 10f;
        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private List<Sprite> backgroundSprites;


        [Header("References")]
        [Tooltip("[Mandatory] Reference to the canvas group that gonna fade in and out.")]
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("[Optional] Change the fill amount of the referenced image from the loading progress percentage.")]
        [SerializeField] private Image loadingBarFill;

        [Space]
        [SerializeField] private Events events;

        public override void Show()
        {
            backgroundImage.sprite = GetRandomBackground();
            gameObject.SetActive(true);
            StartCoroutine(FadeInCoroutine());
            StartBackgroundAnim();
        }

        private Sprite GetRandomBackground()
        {
            if (backgroundSprites == null || backgroundSprites.Count == 0)
                return null;
            return backgroundSprites[Random.Range(0, backgroundSprites.Count-1)];
        }


        public override void Hide()
        {
            StartCoroutine(FadeOutCoroutine());
        }

        public override void UpdateProgress(float progress)
        {
            base.UpdateProgress(progress);

            // Update loading bar fill
            if (loadingBarFill != null)
                loadingBarFill.fillAmount = progress;

            // Show press button to continue
            if (SceneFlowManager.Settings.ActionToExitLoadingScreen && progress >= 1f)
                events.onWaitingForContinue?.Invoke();
        }


        private IEnumerator FadeInCoroutine()
        {
            float normalizedTime = 0;
            float alpha = 0;

            events.onFadeInStart?.Invoke();
            for (float t = 0; t < fadeInDuration; t += Time.unscaledDeltaTime)

            {
                normalizedTime = t / fadeInDuration;
                alpha = fadeInCurve.Evaluate(normalizedTime);
                canvasGroup.alpha = alpha;
                yield return null;
            }
            events.onFadeInComplete?.Invoke();
        }

        private void StartBackgroundAnim()
        {
            background.DOKill();
            background.DOAnchorPosX(-backgroundXDelta, backgroundAnimTime).SetEase(backgroundAnimEase)
                .ChangeStartValue(new Vector2(backgroundXDelta, 0f));
        }


        private IEnumerator FadeOutCoroutine()
        {
            float normalizedTime = 0;
            float alpha = 0;

            events.onFadeOutStart?.Invoke();
            for (float t = 0; t < fadeOutDuration; t += Time.unscaledDeltaTime)

            {
                normalizedTime = t / fadeOutDuration;
                alpha = fadeOutCurve.Evaluate(normalizedTime);
                canvasGroup.alpha = 1 - alpha;
                yield return null;
            }
            events.onFadeOutComplete?.Invoke();
            gameObject.SetActive(false);
        }

    }

}

