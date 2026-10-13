using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.Gameplay.UI
{
    public class BreathingPointElement : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private AnimationCurve _popAnimationCurve;
        [SerializeField] private float _popAnimationTime;
        [SerializeField] private Image _fillImage;

        private bool _isOn = false;

        private void Awake()
        {
            _fillImage.gameObject.SetActive(false);
        }

        public void SetOn(bool isOn, bool animate)
        {
            if (isOn == _isOn)
                return;
            _isOn = isOn;
            _fillImage.gameObject.SetActive(isOn);
            if (isOn)
                StartCoroutine(PopAnimation());
        }

        private IEnumerator PopAnimation()
        {
            for (float t = 0; t < _popAnimationTime; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * _popAnimationCurve.Evaluate(t / _popAnimationTime);
                yield return 0;
            }
            transform.localScale = Vector3.one;
        }


    }
}
