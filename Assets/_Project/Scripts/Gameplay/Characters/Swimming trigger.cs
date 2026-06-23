using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// To put on ther water to trigger the swimming animations of the player 
    /// </summary>
    /// 
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
