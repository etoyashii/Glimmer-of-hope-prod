using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Use on any object to activate the wind zone (only with a character controller)
    /// </summary>
    public class FallZone : MonoBehaviour
    {
        #region Unity Lifecycle
        private void OnTriggerEnter(Collider other)
        {
            WindReturn windReturn = other.GetComponent<WindReturn>();
            if (windReturn != null) windReturn.OnEnterKillZone();
        }

        #endregion
    }
}
