using UnityEngine;
using GlimmerOfHope.Gameplay.Character.SpecialActions;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// A trigger collider that gives an impulse to the player
    /// </summary>
    public class Windcurrent: MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private Vector3 _airCurrentDirection;
        [SerializeField] private float _force;
        #endregion

        #region Private Properties
        private Vector3 ForceVector => _airCurrentDirection.normalized * _force;
        #endregion

        #region Private Methods
        private void OnTriggerEnter(Collider other)
        {

            if (other.TryGetComponent(out Movement movement))
                movement.SetAirCurrent(true, ForceVector);
        }

        private void OnTriggerExit(Collider other)
        {

            if (other.TryGetComponent(out Movement movement))
                movement.SetAirCurrent(false);
        }
        #endregion
    }
}
