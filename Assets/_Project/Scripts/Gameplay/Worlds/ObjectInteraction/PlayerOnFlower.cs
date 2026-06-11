using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class PlayerOnFlower : MonoBehaviour
    {
        #region SerializeFields

        [SerializeField] private GameObject _flowerWalls;

        #endregion

        #region UnityLifecycle

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                Debug.Log("trigger");
                _flowerWalls.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                _flowerWalls.SetActive(false);
            }
        }

        #endregion
    }
}
