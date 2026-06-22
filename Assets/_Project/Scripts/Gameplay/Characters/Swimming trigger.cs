using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class Swimmingtrigger : MonoBehaviour
    {
        [SerializeField] public Swimming _swimming;

        private void OnTriggerEnter(Collider other)
        {

            if (!other.CompareTag("Player")) return;
            if (!_swimming._isSwimmingUnlocked) return;

            // Snap water surface Y from the top of the water collider
            _swimming._waterSurfaceY = GetComponent<Collider>().bounds.max.y;

            _swimming.EnterWater();
        }
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _swimming.ExitWater();
        }
    }
}
