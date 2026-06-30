using DG.Tweening.Plugins.Core.PathCore;
using System.Collections;
using UnityEngine;
using static GlimmerOfHope.Gameplay.FlowerLightReaction;

namespace GlimmerOfHope.Gameplay
{
    public class RotatedByPushPull : MonoBehaviour
    {
        #region SerializeField

        [SerializeField] private Quaternion _targetRotation;
        [SerializeField] private float _timeToRotate;

        #endregion

        #region PublicMethod

        public void Rotate()
        {
            StartCoroutine(RotateProgressively());
        }

        #endregion

        #region Coroutines

        private IEnumerator RotateProgressively()
        {
            float currentTime = 0.0f;
            Quaternion startRota = transform.rotation;

            while (currentTime < _timeToRotate)
            {
                float progress = currentTime / _timeToRotate;

                transform.rotation = Quaternion.Lerp(startRota, _targetRotation, progress);
                currentTime += Time.deltaTime;
                yield return null;
            }

            transform.rotation = _targetRotation;
        }

        #endregion
    }
}
