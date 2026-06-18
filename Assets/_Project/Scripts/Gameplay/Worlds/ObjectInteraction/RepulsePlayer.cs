using System;
using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core;

namespace GlimmerOfHope.Gameplay
{

    /// <summary>
    /// Detect when something goes on it. Depending on tag, it can repulse back the collided entity
    /// </summary>
    public class Bramble : MonoBehaviour
    {
        #region Serialized Fields

        [Range(300.0f, 5000.0f)]
        [SerializeField] private float _forceBack = 500.0f;
        #endregion

        #region Unity Lifecycle

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.gameObject.GetComponent<Rigidbody>().AddForce(Vector3.back * _forceBack);
            }
        }

        #endregion
    }
}