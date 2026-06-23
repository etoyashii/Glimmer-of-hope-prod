using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class Swimmingtrigger : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] public Swimming _swimming;
        #endregion

        #region Private Methods
        private void OnTriggerEnter(Collider other)
        {

            if (!other.CompareTag("Player")) return;
            if (!_swimming._isSwimmingUnlocked) return;

            
            _swimming._waterSurfaceY = GetComponent<Collider>().bounds.max.y;
            Debug.Log("" + GetComponent<Collider>().bounds.max.y);
            _swimming.EnterWater();
        }
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _swimming.ExitWater();
        }
        #endregion
    }
}
