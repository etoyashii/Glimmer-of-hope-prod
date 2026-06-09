using System;
using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class GrowthByLight : MonoBehaviour
    {
        [SerializeField] private Transform _littleSprout;
        [SerializeField] private float _timeToGrow;
        [SerializeField] private Vector3 _targetScale = Vector3.one * 2.0f;
        //[SerializeField] private Animator _animator;

        public void Growth()
        {
            StartCoroutine(ProgressivGrowth());
            //AnimateGrowth();
        }

        private void AnimateGrowth()
        {
            //_animator.Play("Growing"); //example
        }

        private IEnumerator ProgressivGrowth()
        {
            Vector3 startScale = _littleSprout.localScale;
            float currentTime = 0.0f;

            while (currentTime < _timeToGrow)
            {
                currentTime += Time.deltaTime;
                float progress = currentTime / _timeToGrow;
                _littleSprout.localScale = Vector3.Lerp(startScale, _targetScale, progress);

                yield return null;
            }

            _littleSprout.localScale = _targetScale;
        }
    }
}
