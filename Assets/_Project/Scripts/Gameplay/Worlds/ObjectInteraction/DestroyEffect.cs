using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Here the script that regroup all actions before destroying the parent object
    /// </summary>
    public class DestroyEffect : MonoBehaviour
    {
        #region SerializeFields

        //Temporary variable. When we'll have an animation, we'll take the animation time, waiting the animation's end to destroy the object
        [Range(0.2f, 2.0f)]
        [SerializeField] private float _delayToDestroy = 0.5f;

        #endregion


        #region PublicMethods

        public void DestroyThis()
        {
            StartCoroutine(ToDoListBeforeDestroy(_delayToDestroy));
        }

        #endregion

        #region Coroutines

        IEnumerator ToDoListBeforeDestroy(float delay)
        {
            yield return new WaitForSeconds(delay);

            Destroy(this.gameObject);
        }

        #endregion
    }
}
