using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Characters
{
    [CreateAssetMenu(menuName = "GlimmerOfHope/Characters/Registry")]
    public class CharacterRegistrySO : ScriptableObject
    {
        #region Serialized Fields
        [InfoBox("C'est ici qu'on reference toutes les categories de premier niveau. Un seul asset a connaitre.", EInfoBoxType.Normal)]
        [SerializeField] private List<CharacterCategorySO> _categories = new();

        [Header("SkinnedMesh")]
        [Tooltip("FBX maitre contenant tous les SkinnedMeshRenderers du personnage.")]
        [SerializeField] private GameObject _masterCharacterPrefab;
        #endregion

        #region Public Properties
        public IReadOnlyList<CharacterCategorySO> Categories => _categories;
        public GameObject MasterCharacterPrefab => _masterCharacterPrefab;
        #endregion

        #region Public Methods
        // Cherche dans les categories de premier niveau puis dans leurs sous-categories.
        public CharacterCategorySO GetCategoryById(string categoryId)
        {
            foreach (var cat in _categories)
            {
                if (cat == null) continue;
                if (cat.CategoryID == categoryId) return cat;

                foreach (var sub in cat.SubCategories)
                {
                    if (sub != null && sub.CategoryID == categoryId) return sub;
                }
            }
            return null;
        }

        // Retourne le parent d'une sous-categorie, ou null si c'est une categorie racine.
        public CharacterCategorySO GetParentCategory(string subCategoryId)
        {
            foreach (var cat in _categories)
            {
                if (cat == null) continue;
                foreach (var sub in cat.SubCategories)
                {
                    if (sub != null && sub.CategoryID == subCategoryId) return cat;
                }
            }
            return null;
        }

        // Retourne toutes les categories qui portent directement des parts (feuilles de l'arbre).
        // Les categories sans sous-categories sont des feuilles.
        // Les sous-categories sont des feuilles.
        // Les categories parentes (avec sous-categories) ne sont pas retournees.
        public IEnumerable<CharacterCategorySO> GetAllLeafCategories()
        {
            foreach (var cat in _categories)
            {
                if (cat == null) continue;
                if (cat.HasSubCategories)
                {
                    foreach (var sub in cat.SubCategories)
                        if (sub != null) yield return sub;
                }
                else
                {
                    yield return cat;
                }
            }
        }

        public CharacterPartSO GetPartById(string categoryId, string partId)
        {
            var category = GetCategoryById(categoryId);
            return category?.GetPartById(partId);
        }
        #endregion

        #region Editor
        private void OnValidate()
        {
            if (_categories == null || _categories.Count == 0)
                Debug.LogWarning("[CharacterRegistrySO] Aucune categorie referencee dans le Registry.", this);
        }
        #endregion
    }
}
