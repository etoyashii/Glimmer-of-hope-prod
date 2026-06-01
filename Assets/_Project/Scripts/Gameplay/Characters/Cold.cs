using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// This is the cold spell, that set isTrigger collider to true for all objects on sphere range if they have Specific Layer setted  
    /// </summary>
    public class Cold : MonoBehaviour
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
            _layerMask = LayerMask.GetMask("SolidLiquify");
        }

        #endregion

        #region PrivateMethods

        private void OnCold(GameObject target)
        {
            target.GetComponent<Collider>().isTrigger = false;
        }

        #endregion

        #region PublicMethods

        public void UseSkill()
        {
            Ray ray = new(transform.position, transform.TransformDirection(Vector3.forward));

            RaycastHit[] raycastHits = Physics.SphereCastAll(ray, 10.0f, 20.0f, _layerMask);

            for (int i = 0; i < raycastHits.Length; i++)
            {
                OnCold(raycastHits[i].transform.gameObject);
            }
        }

        #endregion
    }
}
