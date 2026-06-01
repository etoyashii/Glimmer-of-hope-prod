using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// This is the DestroyBlock spell, that destroy all objects on sphere range if they have Specific Layer setted
    /// </summary>
    public class DestroyBlock : MonoBehaviour
    {

        #region SerializeFields

        [SerializeField] private float _delay;
        [Range(0.5f,5.0f)]
        [SerializeField] private float _destroyDelay = 1.0f;

        #endregion

        #region PrivateFields

        private LayerMask _layerMask;

        #endregion


        #region UnityLifecycle

        private void Awake()
        {
            _layerMask = LayerMask.GetMask("DestroyableBlock");
        }

        #endregion

        #region PrivateMethods

        //temporary solution
        private void OnDestroyBlock(GameObject target)
        {
            StartCoroutine(WaitToDestroy(_destroyDelay, target));
        }

        #endregion

        #region PublicMethods

        public void UseSkill()
        {
            Ray ray = new(transform.position, transform.TransformDirection(Vector3.forward));

            RaycastHit[] raycastHits = Physics.SphereCastAll(ray, 10.0f, 20.0f, _layerMask);

            for (int i = 0; i < raycastHits.Length; i++)
            {
                OnDestroyBlock(raycastHits[i].transform.gameObject);
            }
        }

        #endregion

        #region Coroutines

        IEnumerator WaitToDestroy(float delay, GameObject go)
        {
            yield return new WaitForSeconds(delay);
            Destroy(go);
        }

        #endregion
    }
}
