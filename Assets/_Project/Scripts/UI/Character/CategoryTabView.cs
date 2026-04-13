using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlimmerOfHope.Gameplay.Characters;

namespace GlimmerOfHope.UI.Widgets
{
    /// <summary>
    /// Vue d'un onglet de catégorie.
    /// À attacher sur la racine du CategoryTabPrefab.
    ///
    /// Le designer crée son prefab librement, attache ce script,
    /// et drag les références vers ses propres éléments UI.
    /// Aucune hypothèse sur les noms ou la hiérarchie des enfants.
    /// </summary>
    public class CategoryTabView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Références UI — drag depuis ton prefab")]
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image    _icon;
        [SerializeField] private Button   _button;

        #endregion

        #region Private Fields

        private string _categoryId;
        private System.Action<string> _onClickCallback;

        #endregion

        #region Public API

        /// <summary>
        /// Initialise l'onglet avec les données de la catégorie.
        /// Appelé par CharacterCategoryTabsController au moment de l'instanciation.
        /// </summary>
        public void Setup(CharacterCategorySO category, System.Action<string> onClickCallback)
        {
            _categoryId      = category.CategoryID;
            _onClickCallback = onClickCallback;

            if (_label != null) _label.text = category.DisplayName;
            if (_icon  != null && category.CategoryIcon != null) _icon.sprite = category.CategoryIcon;

            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        #endregion

        #region Private Methods

        private void OnClick()
        {
            _onClickCallback?.Invoke(_categoryId);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        #endregion
    }
}
