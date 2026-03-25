using UnityEngine;
using GlimmerOfHope.Core.Events;

namespace GlimmerOfHope.Examples
{
    /// <summary>
    /// Exemple d'ÉCOUTE d'un EventChannel.
    /// Démontre le DÉCOUPLAGE : ce script ne connaît pas EventChannelTest,
    /// mais il reçoit quand même l'event !
    ///
    /// Usage : Drag le MÊME EventChannel que EventChannelTest sur ce script.
    /// Quand EventChannelTest.Raise() est appelé, ce script réagit aussi.
    /// </summary>
    public class EventChannelListener : MonoBehaviour
    {
        [Header("=== DRAG LE MÊME EVENT QUE L'AUTRE SCRIPT ===")]
        [SerializeField] private VoidEventChannel _eventToListen;

        [Header("=== RÉACTION ===")]
        [SerializeField] private string _reactionMessage = "Je suis Listener et j'ai reçu l'event !";

        private void OnEnable()
        {
            if (_eventToListen != null)
            {
                _eventToListen.Subscribe(React);
                Debug.Log($"[LISTENER] Je surveille : {_eventToListen.name}");
            }
        }

        private void OnDisable()
        {
            if (_eventToListen != null)
            {
                _eventToListen.Unsubscribe(React);
            }
        }

        private void React()
        {
            Debug.Log($"[LISTENER] {_reactionMessage}");
        }
    }
}
