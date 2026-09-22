using System;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Gameplay.Characters;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private const int   WHEEL_TEX_SIZE = 128;
        private const float CURSOR_SIZE    = 14f;
        private const string BASE_COLOR    = "_BaseColor";
        #endregion

        #region Serialized Fields
        [SerializeField] private StringEventChannel _onCategoryChanged;
        #endregion

        #region Private State
        private CharacterCreatorController _controller;
        private CanvasGroup                _canvasGroup;
        private LayoutElement              _layoutElement;
        private string                     _currentCategoryId;
        private Color                      _currentColor  = Color.white;
        private bool                       _syncingSliders;

        private RectTransform _wheelRt;
        private RectTransform _cursorRt;
        private Image         _previewImg;
        private Slider        _sliderR;
        private Slider        _sliderG;
        private Slider        _sliderB;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _controller    = ServiceLocator.Get<CharacterCreatorController>();
            _canvasGroup   = GetComponent<CanvasGroup>();
            _layoutElement = GetComponent<LayoutElement>();

            SetVisible(false);
            SetupUI();

            if (_onCategoryChanged != null)
                _onCategoryChanged.Subscribe(OnCategoryChanged);
        }

        private void OnDisable()
        {
            // Desinscription immediate a la desactivation (avant OnDestroy) pour eviter
            // que l'event soit recu alors que les composants Unity sont deja detruits.
            if (_onCategoryChanged != null)
                _onCategoryChanged.Unsubscribe(OnCategoryChanged);
        }

        private void OnDestroy()
        {
            if (_onCategoryChanged != null)
                _onCategoryChanged.Unsubscribe(OnCategoryChanged); // filet de securite
        }
        #endregion

        #region Setup
        private void SetupUI()
        {
            SetupWheel();
            SetupSliders();
        }

        private void SetupWheel()
        {
            var wheelTf = transform.Find("ColorWheel");
            if (wheelTf == null)
            {
                Debug.LogWarning("[CharacterColorPicker] ColorWheel introuvable.");
                return;
            }

            _wheelRt = wheelTf.GetComponent<RectTransform>();

            var rawImg = wheelTf.GetComponent<RawImage>();
            if (rawImg != null)
                rawImg.texture = GenerateHueWheelTexture(WHEEL_TEX_SIZE);

            // Calque transparent pour capturer les clics/drags sur la roue
            var inputGo  = new GameObject("WheelInput");
            inputGo.transform.SetParent(wheelTf, false);
            var inputRt  = inputGo.AddComponent<RectTransform>();
            inputRt.anchorMin = Vector2.zero;
            inputRt.anchorMax = Vector2.one;
            inputRt.offsetMin = Vector2.zero;
            inputRt.offsetMax = Vector2.zero;
            var inputImg = inputGo.AddComponent<Image>();
            inputImg.color = Color.clear;
            var relay = inputGo.AddComponent<WheelInputRelay>();
            relay.OnWheelInput += OnWheelInput;

            // Curseur (enfant de WheelInput, au-dessus)
            var cursorGo = new GameObject("WheelCursor");
            cursorGo.transform.SetParent(inputGo.transform, false);
            _cursorRt = cursorGo.AddComponent<RectTransform>();
            _cursorRt.sizeDelta          = new Vector2(CURSOR_SIZE, CURSOR_SIZE);
            _cursorRt.anchoredPosition   = Vector2.zero;
            _cursorRt.anchorMin          = new Vector2(0.5f, 0.5f);
            _cursorRt.anchorMax          = new Vector2(0.5f, 0.5f);
            _cursorRt.pivot              = new Vector2(0.5f, 0.5f);
            cursorGo.AddComponent<Image>().color = Color.black;

            var previewTf = transform.Find("SlidersPanel/ColorPreview");
            if (previewTf != null)
                _previewImg = previewTf.GetComponent<Image>();
        }

        private void SetupSliders()
        {
            _sliderR = FindSlider("SlidersPanel/SliderRowR/Slider");
            _sliderG = FindSlider("SlidersPanel/SliderRowG/Slider");
            _sliderB = FindSlider("SlidersPanel/SliderRowB/Slider");

            if (_sliderR) _sliderR.onValueChanged.AddListener(_ => OnSliderChanged());
            if (_sliderG) _sliderG.onValueChanged.AddListener(_ => OnSliderChanged());
            if (_sliderB) _sliderB.onValueChanged.AddListener(_ => OnSliderChanged());
        }

        private Slider FindSlider(string path)
        {
            var tf = transform.Find(path);
            if (tf == null)
            {
                Debug.LogWarning($"[CharacterColorPicker] Slider introuvable : {path}");
                return null;
            }
            return tf.GetComponent<Slider>();
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

        #region Wheel Input
        private void OnWheelInput(PointerEventData e)
        {
            if (_wheelRt == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _wheelRt, e.position, e.pressEventCamera, out var local))
                return;

            float radius  = _wheelRt.rect.width * 0.5f;
            float dist    = local.magnitude;
            float clamped = Mathf.Min(dist, radius);
            float hue     = (Mathf.Atan2(local.y, local.x) / (Mathf.PI * 2f) + 1f) % 1f;
            float sat     = clamped / radius;

            _currentColor = Color.HSVToRGB(hue, sat, 1f);

            float angle = Mathf.Atan2(local.y, local.x);
            _cursorRt.anchoredPosition = new Vector2(
                Mathf.Cos(angle) * sat * radius,
                Mathf.Sin(angle) * sat * radius);

            SyncSlidersFromColor();
            UpdatePreview();
            NotifyController();
        }
        #endregion

        #region Slider Input
        private void OnSliderChanged()
        {
            if (_syncingSliders) return;
            if (_sliderR == null || _sliderG == null || _sliderB == null) return;

            _currentColor = new Color(_sliderR.value, _sliderG.value, _sliderB.value);
            SyncCursorFromColor();
            UpdatePreview();
            NotifyController();
        }
        #endregion

        #region Color Sync
        private void SetColorDirect(Color color)
        {
            _currentColor = color;
            SyncSlidersFromColor();
            SyncCursorFromColor();
            UpdatePreview();
        }

        private void SyncSlidersFromColor()
        {
            _syncingSliders = true;
            if (_sliderR) _sliderR.value = _currentColor.r;
            if (_sliderG) _sliderG.value = _currentColor.g;
            if (_sliderB) _sliderB.value = _currentColor.b;
            _syncingSliders = false;
        }

        private void SyncCursorFromColor()
        {
            if (_wheelRt == null || _cursorRt == null) return;
            Color.RGBToHSV(_currentColor, out float h, out float s, out _);
            float radius = _wheelRt.rect.width * 0.5f;
            float angle  = h * Mathf.PI * 2f;
            _cursorRt.anchoredPosition = new Vector2(
                Mathf.Cos(angle) * s * radius,
                Mathf.Sin(angle) * s * radius);
        }

        private void UpdatePreview()
        {
            if (_previewImg != null)
                _previewImg.color = _currentColor;
        }
        #endregion

        #region Controller
        private void NotifyController()
        {
            if (string.IsNullOrEmpty(_currentCategoryId) || _controller == null) return;
            _controller.SetCategoryColor(_currentCategoryId, _currentColor);
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

        #region Texture Generation
        private static Texture2D GenerateHueWheelTexture(int size)
        {
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float center = size * 0.5f;
            float radius = center - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx   = x - center;
                    float dy   = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > radius)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float hue = (Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 1f) % 1f;
                    float sat = dist / radius;
                    Color c   = Color.HSVToRGB(hue, sat, 1f);
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
        #endregion
    }

    // Relai d'input pointer sur la roue chromatique.
    internal class WheelInputRelay : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public event Action<PointerEventData> OnWheelInput;

        public void OnPointerDown(PointerEventData e) => OnWheelInput?.Invoke(e);
        public void OnDrag(PointerEventData e)        => OnWheelInput?.Invoke(e);
    }
}
