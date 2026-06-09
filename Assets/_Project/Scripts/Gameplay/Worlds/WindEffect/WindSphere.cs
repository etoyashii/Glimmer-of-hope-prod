using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Use on the sphere of the shooter
    /// </summary>
    public class WindSphere : MonoBehaviour
    {
        #region Public Properties

        public int index;

        #endregion

        #region Private Properties

        private ClothZone _zone;

        #endregion

        #region Unity Lifecycle

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "ClothZone")
            {
                _zone = other.GetComponent<ClothZone>();

                if (_zone != null)
                    _zone.AddSphereToCloth(this.gameObject);
                else
                    Debug.LogError("No script found on gameobject");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag == "ClothZone")
            {
                if (_zone == null)
                {
                    Debug.LogError("Zone wasnt register when enter");
                    return;
                }

                if (other.gameObject == _zone.gameObject)
                {
                    _zone.RemoveSphere(this);
                    Destroy(this.gameObject);
                }
            }
        }

        #endregion
    }
}
