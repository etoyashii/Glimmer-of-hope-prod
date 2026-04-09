using UnityEngine;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.Gameplay.Characters
{
    public class CharacterSystemBootstrapper : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Character System")]
        [SerializeField] private CharacterRegistrySO _characterRegistry;
        [SerializeField] private StringEventChannel _onCharacterPartChanged;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (_characterRegistry == null)
            {
                Debug.LogError("[CharacterSystemBootstrapper] _characterRegistry non assigné.", this);
                return;
            }

            if (_onCharacterPartChanged == null)
            {
                Debug.LogError("[CharacterSystemBootstrapper] _onCharacterPartChanged non assigné.", this);
                return;
            }

            ServiceLocator.Register(new CharacterCreatorController(
                _characterRegistry,
                _onCharacterPartChanged
            ));

            Debug.Log("[CharacterSystemBootstrapper] CharacterCreatorController enregistré.");
        }
        #endregion
    }
}
