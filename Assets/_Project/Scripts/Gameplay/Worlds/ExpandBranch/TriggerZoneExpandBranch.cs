using UnityEngine;
using UnityEngine.InputSystem;
using static Codice.Client.Common.EventTracking.TrackFeatureUseEvent.Features.DesktopGUI.Filters;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Manage the activation of the branch
    /// </summary>
    public class TriggerZoneExpandBranch : MonoBehaviour
    {
        public Vector3 toPoint; //where the branch must go when grow     on the branch ?
        public float maxLookAngle = 30f;

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
                        Debug.Log("Grow branch");
                }
                else if (Keyboard.current.eKey.wasPressedThisFrame) //push
                {
                    bool canActivate = CheckInteraction(true, other.transform);

                    if (canActivate)
                        Debug.Log("Grow branch");
                }
            }
        }

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
    }
}
