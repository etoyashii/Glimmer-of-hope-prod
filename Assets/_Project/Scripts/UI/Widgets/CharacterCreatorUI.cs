using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using TMPro;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.UI.Widgets
{

    public class CharacterCreatorUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Events")]
        [SerializeField] private StringEventChannel _onPartChanged;

        [Header("Category Navigation")]
        [SerializeField] private Transform _categoryTabContainer;
        [SerializeField] private GameObject _categoryTabPrefab;

        [Header("Parts Grid")]
        [SerializeField] private Transform _partsGridContainer;
        [SerializeField] private GameObject _partButtonPrefab;

        [Header("Actions")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _resetButton;

        #endregion

        #region Private Fields

        private GlimmerOfHope.Gameplay.Characters.CharacterCreatorController _controller;
        private string _activeCategoryId;

        private readonly List<GameObject> _spawnedTabs = new();
        private readonly List<GameObject> _spawnedButtons = new();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _controller = ServiceLocator.Get<GlimmerOfHope.Gameplay.Characters.CharacterCreatorController>();

            if (_controller == null)
            {
                Debug.LogError("[CharacterCreatorUI] CharacterCreatorController introuvable dans le ServiceLocator.");
                return;
            }

            BuildCategoryTabs();
            BindButtons();

            if (_controller.Registry.Categories.Count > 0)
                ShowCategory(_controller.Registry.Categories[0].CategoryID);
        }

        private void OnEnable()
        {
            if (_onPartChanged != null)
                _onPartChanged.Subscribe(OnPartChanged);
        }

        private void OnDisable()
        {
            if (_onPartChanged != null)
                _onPartChanged.Unsubscribe(OnPartChanged);
        }

        #endregion

        #region Private Methods

        private void BuildCategoryTabs()
        {
            ClearSpawned(_spawnedTabs, _categoryTabContainer);

            foreach (var category in _controller.Registry.Categories)
            {
                if (category == null) continue;

                var tab = Instantiate(_categoryTabPrefab, _categoryTabContainer);

                // Label
                //var labelTmp = FindChildTMP(tab, "Label");
                //if (labelTmp != null)
                //    labelTmp.text = category.DisplayName;

                // Icône
                //var iconImg = FindChildImage(tab, "Icon");
                //if (iconImg != null && category.CategoryIcon != null)
                //    iconImg.sprite = category.CategoryIcon;

                // Click
                var btn = tab.GetComponent<Button>();
                var captured = category.CategoryID;
                if (btn != null)
                    btn.onClick.AddListener(() => ShowCategory(captured));

                _spawnedTabs.Add(tab);
            }

            ForceRebuild(_categoryTabContainer);
        }

        private void ShowCategory(string categoryId)
        {
            _activeCategoryId = categoryId;
            ClearSpawned(_spawnedButtons, _partsGridContainer);

            var category = _controller.Registry.GetCategoryById(categoryId);
            if (category == null)
            {
                Debug.LogWarning($"[CharacterCreatorUI] Catégorie '{categoryId}' introuvable dans le registry.");
                return;
            }

            //var partsLabel = FindSiblingTMP(_partsGridContainer, "PartsLabel");
            //if (partsLabel != null)
            //    partsLabel.text = category.DisplayName.ToUpper();

            foreach (var part in category.Parts)
            {
                if (part == null) continue;

                var btn = Instantiate(_partButtonPrefab, _partsGridContainer);

                // Thumbnail
                //var thumbImg = FindChildImage(btn, "Thumbnail");
                //if (thumbImg != null && part.Thumbnail != null)
                //    thumbImg.sprite = part.Thumbnail;

                // Label
                //var labelTmp = FindChildTMP(btn, "Label");
                //if (labelTmp != null)
                //    labelTmp.text = part.DisplayName;

                // Click
                var capturedCat = categoryId;
                var capturedPart = part.PartID;
                var partBtn = btn.GetComponent<Button>();
                if (partBtn != null)
                    partBtn.onClick.AddListener(() => _controller.SelectPart(capturedCat, capturedPart));

                _spawnedButtons.Add(btn);
            }

            ForceRebuild(_partsGridContainer);
        }

        private void BindButtons()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(OnConfirm);

            if (_resetButton != null)
                _resetButton.onClick.AddListener(OnReset);
        }

        private void OnPartChanged(string categoryId)
        {
            // Extension possible : highlighter le bouton sélectionné
        }

        private void OnConfirm()
        {
            Debug.Log("[CharacterCreatorUI] Personnage confirmé.");
            // F4 : brancher sur SaveManager ici
        }

        private void OnReset()
        {
            _controller.ResetToDefaults();

            if (!string.IsNullOrEmpty(_activeCategoryId))
                ShowCategory(_activeCategoryId);
        }

        #endregion

        #region Layout Helpers

        private static void ForceRebuild(Transform container)
        {
            if (container == null) return;
            var rt = container.GetComponent<RectTransform>();
            if (rt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        private static void ClearSpawned(List<GameObject> list, Transform container)
        {
            foreach (var go in list)
                if (go != null) Destroy(go);
            list.Clear();
        }

        #endregion

        #region Child Lookup Helpers


        //private static TextMeshProUGUI FindChildTMP(GameObject root, string childName)
        //{
        //    var child = FindDeep(root.transform, childName);
        //    return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        //}


        //private static Image FindChildImage(GameObject root, string childName)
        //{
        //    var child = FindDeep(root.transform, childName);
        //    return child != null ? child.GetComponent<Image>() : null;
        //}


        //private static TextMeshProUGUI FindSiblingTMP(Transform container, string siblingName)
        //{
        //    if (container == null || container.parent == null) return null;
        //    var sibling = container.parent.Find(siblingName);
        //    return sibling != null ? sibling.GetComponent<TextMeshProUGUI>() : null;
        //}

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var result = FindDeep(child, name);
                if (result != null) return result;
            }
            return null;
        }

        #endregion
    }
}