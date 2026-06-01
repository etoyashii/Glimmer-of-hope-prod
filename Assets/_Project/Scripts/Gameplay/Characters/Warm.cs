using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    public class Warm : MonoBehaviour
    {
        #region SerializeFields

        [SerializeField] private float _delay;
        [SerializeField] private SkillNoteManager _skillNoteManager;

        #endregion

        #region PrivateFields

        private LayerMask _layerMask;

        #endregion


        #region UnityLifecycle

        private void Awake()
        {
            _layerMask = LayerMask.GetMask("TestFreeze");
        }

        #endregion

        #region PrivateMethods

        private void OnWarm(GameObject target)
        {
            Debug.Log(target.name);
            target.GetComponent<Collider>().enabled = false;
        }

        #endregion

        #region PublicMethods

        public void UseSkill()
        {
            Ray ray = new(transform.position, transform.TransformDirection(Vector3.forward));

            RaycastHit[] raycastHits = Physics.SphereCastAll(ray, 10.0f, 20.0f, _layerMask);

            for (int i = 0; i < raycastHits.Length; i++)
            {
                OnWarm(raycastHits[i].transform.gameObject);
            }

        }

        #endregion
    }
}
