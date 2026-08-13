using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Drop this anywhere to unlock a Gate node without writing code
    /// </summary>
    public class DialogueGateEventRelay : MonoBehaviour
    {
        #region Public Methods

        //Call with the same Event ID configured on the Gate node
        public void TriggerGateEvent(string eventId)
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("[DialogueGateEventRelay] No DialogueManager found in the scene.");
                return;
            }

            DialogueManager.Instance.NotifyGateEvent(eventId);
        }

        #endregion
    }
}
