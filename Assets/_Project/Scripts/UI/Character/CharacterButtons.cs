using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.UI.Widgets
{
    [RequireComponent(typeof(Button))]
    public class CharacterResetButton : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Event")]
        [Tooltip("Le channel de catégorie, pour rafraîchir la grille après reset.")]
        [SerializeField] private StringEventChannel _onCategorySelected;
        #endregion

        #region Private Fields
        private Button _button;
        private GlimmerOfHope.Gameplay.Characters.CharacterCreatorController _controller;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void Start()
        {
            _controller = ServiceLocator.Get<GlimmerOfHope.Gameplay.Characters.CharacterCreatorController>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }
        #endregion

        #region Private Methods
        private void OnClick()
        {
            if (_controller == null) return;

            _controller.ResetToDefaults();

            if (_onCategorySelected != null && _controller.Registry.Categories.Count > 0)
                _onCategorySelected.Raise(_controller.Registry.Categories[0].CategoryID);

            Debug.Log("[CharacterResetButton] Personnage réinitialisé.");
        }
        #endregion
    }

    [RequireComponent(typeof(Button))]
    public class CharacterConfirmButton : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Event (optionnel)")]
        [Tooltip("Fired quand le joueur confirme son personnage. Laisser vide si non utilisé.")]
        [SerializeField] private VoidEventChannel _onCharacterConfirmed;
        #endregion

        #region Private Fields
        private Button _button;
        private GlimmerOfHope.Gameplay.Characters.CharacterCreatorController _controller;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void Start()
        {
            _controller = ServiceLocator.Get<GlimmerOfHope.Gameplay.Characters.CharacterCreatorController>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }
        #endregion

        #region Private Methods
        private void OnClick()
        {
            if (_controller == null) return;

            // F4 : SaveToData ici quand le save system sera branché
            _onCharacterConfirmed?.Raise();

            Debug.Log("[CharacterConfirmButton] Personnage confirmé.");
        }
        #endregion
    }
}
