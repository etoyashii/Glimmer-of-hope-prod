using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class Repulse : MonoBehaviour
    {
        #region SerializeFields

        [Range(10.0f, 1000.0f)]
        [SerializeField] private float _forceImpulse = 200.0f;

        private float _attenuationRatio = 100.0f;

        #endregion
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Vector3 newVector = Vector3.back + Vector3.up / _attenuationRatio;

                collision.rigidbody.AddForce(newVector * _forceImpulse, ForceMode.Impulse);
                PlayerSignalManager.Instance.SendBlinkSignal();
            }
        }
    }
}
