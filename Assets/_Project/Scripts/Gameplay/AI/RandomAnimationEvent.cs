using System;
using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class RandomAnimationEvent : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _triggerName;
        [SerializeField] private float _minEventWaitTime;
        [SerializeField] private float _maxEventWaitTime;

        private Coroutine _coroutine;

        void OnEnable()
        {
            StartCoroutine(AnimCoroutine());
        }

        void OnDisable()
        {
            StopCoroutine(_coroutine);
        }

        private IEnumerator AnimCoroutine()
        {
            float time = 0f;
            while (true)
            {
                time = UnityEngine.Random.Range(_minEventWaitTime, _maxEventWaitTime);
                yield return new WaitForSeconds(time);
                _animator.SetTrigger(_triggerName);
            }
        }
    }
}
