using UnityEngine;
using TMPro;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.UI.Widgets
{
    /// <summary>
    /// Met à jour un TMP_Text avec le nom de la catégorie sélectionnée.
    ///
    /// Setup designer :
    ///   1. Attacher ce script sur le même GameObject que le TMP_Text voulu.
    ///   2. Assigner _onCategorySelected (même channel que les autres composants).
    ///   3. Optionnel : cocher _uppercase pour afficher en majuscules.
    ///   4. Optionnel : renseigner _prefix (ex: "Catégorie : ").
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class CharacterCategoryLabel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Event")]
        [SerializeField] private StringEventChannel _onCategorySelected;

        [Header("Options")]
        [SerializeField] private bool   _uppercase = true;
        [SerializeField] private string _prefix    = "";

        #endregion

        #region Private Fields

        private TMP_Text _text;
        private GlimmerOfHope.Gameplay.Characters.CharacterCreatorController _controller;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            _controller = ServiceLocator.Get<GlimmerOfHope.Gameplay.Characters.CharacterCreatorController>();
        }

        private void OnEnable()
        {
            if (_onCategorySelected != null)
                _onCategorySelected.Subscribe(OnCategorySelected);
        }

        private void OnDisable()
        {
            if (_onCategorySelected != null)
                _onCategorySelected.Unsubscribe(OnCategorySelected);
        }

        #endregion

        #region Private Methods

        private void OnCategorySelected(string categoryId)
        {
            if (_controller == null || _text == null) return;

            var category = _controller.Registry.GetCategoryById(categoryId);
            if (category == null) return;

            var displayName = _prefix + category.DisplayName;
            _text.text = _uppercase ? displayName.ToUpper() : displayName;
        }

        #endregion
    }
}
