using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public sealed class PlayerSignalManager : MonoBehaviour
    {
        #region Singleton
        private PlayerSignalManager() { }

        private static PlayerSignalManager _instance;

        public static PlayerSignalManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new PlayerSignalManager();
            }
            return _instance;
        }

        #endregion

        #region Events

        public event Action OnBlinkSignalSend;

        #endregion

        #region PublicMethods

        public void SendBlinkSignal()
        {
            OnBlinkSignalSend?.Invoke();
        }

        #endregion
    }
}
