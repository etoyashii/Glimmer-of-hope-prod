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
                    // Crée un nouveau GameObject si l'instance n'existe pas
                    GameObject singletonObject = new GameObject(typeof(PlayerSignalManager).Name);
                    _instance = singletonObject.AddComponent<PlayerSignalManager>();
                    DontDestroyOnLoad(singletonObject); // Optionnel : garde l'instance entre les scènes
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject); // Détruit les duplicates
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject); // Optionnel
        }
        #endregion

        #region Events
        public event Action OnBlinkSignalSend;
        public event Action<int> OnSkillLearn;
        #endregion

        #region Private Fields
        private int _learnSkillId = 5;
        #endregion

        #region Public Methods
        public void SendBlinkSignal()
        {
            OnBlinkSignalSend?.Invoke();
        }

        public void SendSkillLearn()
        {
            OnSkillLearn?.Invoke(_learnSkillId);
        }

        public void SetSkillLearnId(int index)
        {
            _learnSkillId = index;
        }
        #endregion
    }
}