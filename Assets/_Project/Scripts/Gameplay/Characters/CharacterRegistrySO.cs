using GlimmerOfHope.Gameplay.Characters;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Characters
{
    [CreateAssetMenu(menuName = "GlimmerOfHope/Characters/Registry")]
    public class CharacterRegistrySO : ScriptableObject
    {
        #region Serialized Fields
        [InfoBox("C'est ici qu'on référence toutes les catégories du système. Un seul asset à connaître.", EInfoBoxType.Normal)]
        [SerializeField] private List<CharacterCategorySO> _categories = new();
        #endregion

        #region Public Properties
        public IReadOnlyList<CharacterCategorySO> Categories => _categories;
        #endregion

        #region Public Methods
        public CharacterCategorySO GetCategoryById(string categoryID)
        {
            foreach (var category in _categories)
            {
                if (category != null && category.CategoryID == categoryID)
                    return category;
            }

            return null;
        }

        public CharacterPartSO GetPartById(string categoryID, string partID)
        {
            var category = GetCategoryById(categoryID);
            return category?.GetPartById(partID);
        }
        #endregion

        #region Editor
        private void OnValidate()
        {
            if (_categories == null || _categories.Count == 0)
                Debug.LogWarning("[CharacterRegistrySO] Aucune catégorie référencée dans le Registry.", this);
        }
        #endregion
    }
}
