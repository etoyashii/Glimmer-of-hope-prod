using UnityEngine;
using System;

namespace GlimmerOfHope.Gameplay
{
    public sealed class PlayerSignalManager : MonoBehaviour
    {
        #region Singleton
        private static PlayerSignalManager _instance;

        public static PlayerSignalManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("PlayerSignalManager: Aucune instance trouvée dans la scène. Ajoutez un GameObject avec ce composant.");
                    return null;
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region Events
        public event Action OnBlinkSignalSend;
        public event Action<int> OnSkillLearn;
        #endregion

        #region Private Fields
        private int _learnSkillId = -1;
        #endregion

        #region Public Methods
        public void SendBlinkSignal()
        {
            OnBlinkSignalSend?.Invoke();
        }

        public void SendSkillLearn()
        {
            if (_learnSkillId < 0) return;

            OnSkillLearn?.Invoke(_learnSkillId);
        }

        public void SetSkillLearnId(int index)
        {
            _learnSkillId = index;
        }
        #endregion
    }
}