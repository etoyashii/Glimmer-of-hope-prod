using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    public class Meteo : MonoBehaviour
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

        private void OnFreeze()
        {
        }

        #endregion

        #region PublicMethods

        public void UseSkill()
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, _layerMask))
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
                Debug.Log("Did Hit");
            }
            else
                Debug.Log("fhishfisdks");
        }

        #endregion
    }
}
