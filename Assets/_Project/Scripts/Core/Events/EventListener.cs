using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Core.Events
{
    public class EventListener : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _channel;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_channel != null)
                _channel.Subscribe(OnEventRaised);
        }

        private void OnDisable()
        {
            if (_channel != null)
                _channel.Unsubscribe(OnEventRaised);
        }

        private void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
