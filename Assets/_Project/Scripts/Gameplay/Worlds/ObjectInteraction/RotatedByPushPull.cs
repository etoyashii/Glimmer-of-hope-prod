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
        [SerializeField] private GameObject _trunk;
        [SerializeField] private float _timeToRotate;
        //[SerializeField] private GameObject _foliage;

        #endregion

        #region PublicMethod

        public void Rotate()
        {
            Instantiate(_trunk);
            //transform.Rotate(_targetRotation);
        }

        #endregion

        #region Coroutines

        private IEnumerator ProgressivRotation()
        {
            float currentTime = 0.0f;
            float _currentMovementProgress = 0.0f;

            while (_currentMovementProgress < 1.0f)
            {
                _currentMovementProgress += Time.deltaTime / currentTime;
                _currentMovementProgress = Mathf.Clamp01(_currentMovementProgress);

                //transform.position = bezierPosition;

                yield return null;
            }

            while (currentTime < _timeToRotate)
            {
                transform.rotation = _targetRotation;

                currentTime += Time.deltaTime;
                yield return null;
            }

        }

        #endregion
    }
}
