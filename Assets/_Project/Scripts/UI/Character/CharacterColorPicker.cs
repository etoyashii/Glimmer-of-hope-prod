using System;
using System.Collections.Generic;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Gameplay.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.UI.Character
{
    // Roue chromatique interactive pour le character creator.
    // Pose ce composant sur le GO "ColorPickerSection" (genere par CharacterCreatorSidebarGenerator).
    // Enfants attendus : ColorWheel, SlidersPanel/ColorPreview, SlidersPanel/SliderRowR-G-B/Slider.
    [RequireComponent(typeof(CanvasGroup))]
    public class CharacterColorPicker : MonoBehaviour
    {
        #region Constants
        private const float CURSOR_SIZE    = 14f;
        private const string BASE_COLOR    = "_BaseColor";
        #endregion

        #region Serialized Fields
        [SerializeField] private StringEventChannel _onCategoryChanged;
        [SerializeField] private ColorPicker _colorPicker;
        [SerializeField] private TMP_InputField _hexInputField;
        #endregion

        #region Private State
        private CharacterCreatorController _controller;
        private CanvasGroup                _canvasGroup;
        private LayoutElement              _layoutElement;
        private string                     _currentCategoryId;
        #endregion

        #region Helpers
        private Color CurrentColor => _colorPicker.Color;

        #endregion


        #region Unity Lifecycle
        private void Start()
        {
            _controller    = ServiceLocator.Get<CharacterCreatorController>();
            _canvasGroup   = GetComponent<CanvasGroup>();
            _layoutElement = GetComponent<LayoutElement>();

            SetVisible(false);

            if (_onCategoryChanged != null)
                _onCategoryChanged.Subscribe(OnCategoryChanged);
        }

        void OnEnable()
        {
            _colorPicker.OnColorChanged += OnColorChanged;
            _hexInputField.onSubmit.AddListener(OnHexInputFieldChanged);
            CharacterColorPresetButton.OnSelectColor += OnColorPresetSelected;
        }

        private void OnDisable()
        {
            // Desinscription immediate a la desactivation (avant OnDestroy) pour eviter
            // que l'event soit recu alors que les composants Unity sont deja detruits.
            if (_onCategoryChanged != null)
                _onCategoryChanged.Unsubscribe(OnCategoryChanged);
            _colorPicker.OnColorChanged -= OnColorChanged;
            _hexInputField.onSubmit.RemoveListener(OnHexInputFieldChanged);
            CharacterColorPresetButton.OnSelectColor -= OnColorPresetSelected;
        }

        private void OnDestroy()
        {
            if (_onCategoryChanged != null)
                _onCategoryChanged.Unsubscribe(OnCategoryChanged); // filet de securite
        }
        #endregion

        #region Category
        private void OnCategoryChanged(string categoryId)
        {
            // Guard : l'event vient d'un ScriptableObject qui survit aux transitions de scene.
            // Si ce composant est detruit, on ignore.
            if (this == null) return;

            _currentCategoryId = categoryId;

            var category = _controller?.Registry?.GetCategoryById(categoryId);
            bool colorable = IsColorable(category);
            SetVisible(colorable);

            // Quand ignoreLayout change, le parent VLG (ContentArea) doit recalculer
            // son layout pour inclure ou exclure ColorPickerSection.
            // Note : pas de ?. ici - Unity fake-null passe le test C# et lancerait MRE.
            var parent = transform.parent;
            if (parent != null)
            {
                var parentRt = parent.GetComponent<RectTransform>();
                if (parentRt != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

            if (!colorable) return;

            SetColorDirect(_controller.GetCategoryColor(categoryId));
        }

        private bool IsColorable(CharacterCategorySO cat)
        {
            if (cat == null) return false;
            foreach (var part in cat.Parts)
                if (part != null && part.PartType == CharacterPartType.SkinnedMesh)
                    return true;
            return false;
        }
        #endregion

        #region Color Sync
        private void SetColorDirect(Color color)
        {
            _colorPicker.SetColor(color);
        }

        private void OnColorChanged(Color color)
        {
            NotifyController();
            _hexInputField.SetTextWithoutNotify(_colorPicker.GetColorHexCode());
        }

        private void OnColorPresetSelected(Color color)
        {
            _colorPicker.SetColor(color);
        }


        private void OnHexInputFieldChanged(string hexCode)
        {
            if (!_colorPicker.SetColor(hexCode))
                _hexInputField.SetTextWithoutNotify(_colorPicker.GetColorHexCode());
        }
        #endregion

        #region Controller
        private void NotifyController()
        {
            if (string.IsNullOrEmpty(_currentCategoryId) || _controller == null) return;
            _controller.SetCategoryColor(_currentCategoryId, CurrentColor);
        }
        #endregion

        #region Visibility
        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha          = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable   = visible;
            }
            if (_layoutElement != null)
                _layoutElement.ignoreLayout = !visible;
        }
        #endregion
    }
}
