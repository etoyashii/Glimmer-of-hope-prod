using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay
{
    public class ButterflyColideMoss : MonoBehaviour
    {
        [SerializeField] private GameObject Butterfly;
        [SerializeField] UnityEvent _onValidated;
  
        public bool IsValidated { get; private set; }

        void Start()
        {
            IsValidated = false;
        }

        void OnCollisionEnter(Collision collision)
        {
            if(collision.gameObject.CompareTag("MossSphere"))
            {
                Debug.Log("Collision with Moss");
                Butterfly.SetActive(false);
                _onValidated?.Invoke();
                IsValidated = true;
            }
        }
    }
}
