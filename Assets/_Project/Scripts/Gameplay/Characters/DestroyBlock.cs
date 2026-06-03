using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// This is the DestroyBlock spell, that try getting DestroyEffect script component for all objects on sphere range if they have Specific Layer setted and then call the effect
    /// </summary>
    public class DestroyBlock : MonoBehaviour
    {
        #region SerializeFields

        [SerializeField] private float _delay;

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

        #region PublicMethods

        //Use a sphere raycast to detect specific objects based on specified layer
        //then check if the detected objects contains the DestroyEffect component
        //because we need to use the DestroyThis method contained on this script.
        //So only the objects that have DestroyEffect can be used (and destroy)
        public void UseSkill()
        {
            Ray ray = new(transform.position, transform.TransformDirection(Vector3.forward));

            RaycastHit[] raycastHits = Physics.SphereCastAll(ray, 10.0f, 20.0f, _layerMask);

            for (int i = 0; i < raycastHits.Length; i++)
            {
                //TODO: If there's not much LD element that require this check, I'll rework that into check list instead of TryGetComponent that is pretty bad optimizly speaking
                if (raycastHits[i].transform.gameObject.TryGetComponent<DestroyEffect>(out DestroyEffect destroyEffect))
                {
                    destroyEffect.DestroyThis();
                }
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
