using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Manage the activation of the branch
    /// </summary>

    public class TriggerZoneExpandBranch : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private GameObject platform;

        #endregion

        #region Public Properties

        public Transform toPoint; //where the branch must go when grow     on the branch ?
        public float maxLookAngle = 30f;
        public float duration = 10f;

        #endregion

        #region Private Properties

        private bool _isActive;
        private float _progress;

        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _progress = duration;

            platform.SetActive(false);
        }

        private void Update()
        {
            if (_isActive)
            {
                _progress -= Time.deltaTime;

                if (_progress <= 0f)
                {
                    _isActive = false;
                    platform.SetActive(false);
                    Debug.Log("La branche ce retracte");
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.tag == "Player")
            {
                //check if use of wind spell and wich one
                //if use of a wind spell check if he can activate the branch
                //check the view direction of the player if not looking at the branch doesnt work

                if (Keyboard.current.qKey.wasPressedThisFrame) //attire
                {
                    bool canActivate = CheckInteraction(false, other.transform);

                    if (canActivate)
                        GrowBranch();
                }
                else if (Keyboard.current.eKey.wasPressedThisFrame) //push
                {
                    bool canActivate = CheckInteraction(true, other.transform);

                    if (canActivate)
                        GrowBranch();
                }
            }
        }

        #endregion

        #region Private Methods

        private bool CheckInteraction(bool isPush, Transform tr)
        {
            Vector3 toPlayer = (tr.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, toPlayer);

            if (isPush && dot < 0)
                return CheckView(tr);
            else if (!isPush && dot > 0)
                return CheckView(tr);

            Debug.Log("Wrong interaction");
            return false;
        }

        private bool CheckView(Transform tr) //call only if its the correct interaction
        {
            float lookAngle = Vector3.Angle(transform.forward, tr.forward);

            if (lookAngle < maxLookAngle || lookAngle > 180f - maxLookAngle)
                return true;

            Debug.Log("Isnt looking in the correct direction : " + lookAngle);
            return false;
        }

        private void GrowBranch()
        {
            Debug.Log("Grow branch");

            _isActive = true;
            platform.SetActive(true);
            _progress = duration;
        }

        #endregion
    }
}
