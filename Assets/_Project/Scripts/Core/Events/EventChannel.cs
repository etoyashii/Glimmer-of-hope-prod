using System;
using UnityEngine;

namespace GlimmerOfHope.Core.Events
{
    public abstract class EventChannel<T> : ScriptableObject
    {
        private Action<T> _onEvent;

        public void Raise(T value)
        {
            _onEvent?.Invoke(value);
        }

        public void Subscribe(Action<T> listener)
        {
            _onEvent += listener;
        }

        public void Unsubscribe(Action<T> listener)
        {
            _onEvent -= listener;
        }

        public void Clear()
        {
            _onEvent = null;
        }
    }
}
