using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;


namespace GlimmerOfHope.Gameplay
{
    public class Repulse : MonoBehaviour
    {
        #region SerializeFields

        [Range(10.0f, 1000.0f)]
        [SerializeField] private float _forceImpulse = 200.0f;

        private float _attenuationRatio = 100.0f;
        [SerializeField] [Range(0f, 1f)] private float _open;
        [SerializeField][Range(0f, 1f)] private float _close;

        private WakeUpEffect WE;

        private void Start()
        {
            WE = GetComponent<WakeUpEffect>();
        }

        #endregion
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                StartCoroutine(WE.Blink(_close, _open));
                Vector3 newVector = Vector3.back + Vector3.up / _attenuationRatio;

                collision.rigidbody.AddForce(newVector * _forceImpulse, ForceMode.Impulse);
                PlayerSignalManager.Instance.SendBlinkSignal();
                
            }
        }
        
    }
}
