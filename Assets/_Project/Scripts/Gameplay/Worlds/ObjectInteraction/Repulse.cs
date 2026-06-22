using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class Repulse : MonoBehaviour
    {
        #region SerializeFields

        [Range(10.0f, 1000.0f)]
        [SerializeField] private float _forceImpulse = 200.0f;

        #endregion
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Vector3 newVector = Vector3.back + Vector3.up;

                collision.rigidbody.AddForce(newVector * _forceImpulse, ForceMode.Impulse);
            }
        }
    }
}
