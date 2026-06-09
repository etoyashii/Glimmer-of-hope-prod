using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// use to define the cloth of a specific zone to apply the wind sphere
    /// </summary>
    public class ClothZone : MonoBehaviour
    {
        #region SerializeField

        [SerializeField] private Cloth[] _cloths;

        #endregion

        #region Public Methods

        public void RemoveSphere(WindSphere windSphere)
        {
            foreach (Cloth cloth in _cloths) //remove the sphere on each cloth
            {
                var current = cloth.sphereColliders;
                current[windSphere.index].first = null;

                cloth.sphereColliders = current;
            }
        }
        public void AddSphereToCloth(GameObject obj)
        {
            int index = GetSafeIndex();

            if (index == -1) //if no index destroy the new sphere
            {
                Destroy(obj);
                return;
            }

            WindSphere bullet = obj.GetComponent<WindSphere>();
            bullet.index = index;

            foreach (Cloth cloth in _cloths) //add the sphere on each cloth
            {
                var current = cloth.sphereColliders;
                current[index].first = obj.GetComponent<SphereCollider>();

                cloth.sphereColliders = current;
            }
        }

        #endregion

        #region Private Methods

        private int GetSafeIndex() //use to get a free index of the list of sphere collider on the cloth
        {
            int index = 0;

            foreach (ClothSphereColliderPair cs in _cloths[0].sphereColliders)
            {
                if (cs.first == null)
                    return index;

                index++;
            }

            return -1;
        }

        #endregion
    }
}
