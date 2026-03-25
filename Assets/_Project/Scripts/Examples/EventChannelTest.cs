using UnityEngine;
using UnityEngine.InputSystem;
using GlimmerOfHope.Core.Events;

namespace GlimmerOfHope.Examples
{
    /// <summary>
    /// Exemple d'utilisation des EventChannels.
    /// Montre comment TRIGGER un event et ÉCOUTER sa propre action.
    /// Voir aussi : EventChannelListener.cs pour l'écoute depuis un autre script.
    /// </summary>
    public class EventChannelTest : MonoBehaviour
    {
        [Header("=== DRAG UN EVENT CHANNEL ICI ===")]
        [SerializeField] private VoidEventChannel _testEvent;

        private void OnEnable()
        {
            if (_testEvent != null)
            {
                _testEvent.Subscribe(HandleEvent);
                Debug.Log($"[EXEMPLE] Abonné à : {_testEvent.name}");
            }
        }

        private void OnDisable()
        {
            if (_testEvent != null)
            {
                _testEvent.Unsubscribe(HandleEvent);
            }
        }

        private void Update()
        {
            // Nouveau Input System : appuie ESPACE pour trigger
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TriggerEvent();
            }
        }

        private void TriggerEvent()
        {
            if (_testEvent != null)
            {
                Debug.Log($"[EXEMPLE] >>> DÉCLENCHEMENT de {_testEvent.name} <<<");
                _testEvent.Raise();
            }
            else
            {
                Debug.LogWarning("[EXEMPLE] Aucun event assigné ! Drag un .asset sur le champ.");
            }
        }

        private void HandleEvent()
        {
            Debug.Log($"[EXEMPLE] EVENT REÇU : {_testEvent.name} a été déclenché !");
        }
    }
}
