using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Use for a simple move forward
    /// </summary>
    public class MoveForward : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private float _speed = 1f;

        #endregion

        #region Private Properties

        private Rigidbody _body;

        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _body = GetComponent<Rigidbody>();
        }

        void Update()
        {
            _body.linearVelocity = Vector3.forward * _speed;
        }

        #endregion
    }
}
