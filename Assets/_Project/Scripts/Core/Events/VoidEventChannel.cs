using System;
using UnityEngine;

namespace GlimmerOfHope.Core.Events
{
    [CreateAssetMenu(fileName = "New Void Event", menuName = "Glimmer/Events/Void Event")]
    public class VoidEventChannel : ScriptableObject
    {
        private Action _onEvent;

        public void Raise()
        {
            _onEvent?.Invoke();
        }

        public void Subscribe(Action listener)
        {
            _onEvent += listener;
        }

        public void Unsubscribe(Action listener)
        {
            _onEvent -= listener;
        }

        public void Clear()
        {
            _onEvent = null;
        }
    }
}
