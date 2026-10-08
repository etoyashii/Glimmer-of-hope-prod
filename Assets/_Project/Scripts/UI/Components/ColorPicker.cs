
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlimmerOfHope.UI
{
    public class ColorPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        #region SerializedFields
        [Header("References")]
        [SerializeField] private RectTransform _wheelRt;
        [SerializeField] private RectTransform _cursorRt;
        [SerializeField] private Image preview;
        #endregion

        private Color _color = Color.white;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                UpdateCursor();
                OnColorChanged?.Invoke(_color);
            }
        }

        #region Events
        public event Action<Color> OnColorChanged;
        #endregion

        #region Public Methods
        public void SetColorWithoutNotify(Color color)
        {
            _color = color;
        }

        void OnEnable()
        {
        }

        public string GetColorHexCode()
        {
            return string.Format("#{0}", ColorUtility.ToHtmlStringRGB(_color));
        }

        public bool SetColor(string hexCode, bool notify = true)
        {
            if (!hexCode.StartsWith('#'))
                hexCode = $"#{hexCode}";
            if (!ColorUtility.TryParseHtmlString(hexCode, out Color color))
                return false;
            _color = color;
            if (notify)
                OnColorChanged?.Invoke(_color);
            UpdateCursor();
            return true;
        }

        public void SetColor(Color color, bool notify = true)
        {
            _color = color;
            if (notify)
                OnColorChanged?.Invoke(color);
            UpdateCursor();
        }

        #endregion

        #region InputHandling
        public void OnPointerDown(PointerEventData eventData)
        {
            OnWheelInput(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnWheelInput(eventData);
        }

        private void OnWheelInput(PointerEventData e)
        {
            if (_wheelRt == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _wheelRt, e.position, e.pressEventCamera, out var local))
                return;

            float radius = _wheelRt.rect.width * 0.5f;
            float dist = local.magnitude;
            float clamped = Mathf.Min(dist, radius);
            float hue = (Mathf.Atan2(local.y, local.x) / (Mathf.PI * 2f) + 1f) % 1f;
            float sat = clamped / radius;

            Color = Color.HSVToRGB(hue, sat, 1f);

            float angle = Mathf.Atan2(local.y, local.x);
            _cursorRt.anchoredPosition = new Vector2(
                Mathf.Cos(angle) * sat * radius,
                Mathf.Sin(angle) * sat * radius);
        }

        private void UpdateCursor()
        {
            if (_wheelRt == null || _cursorRt == null) return;
            Color.RGBToHSV(_color, out float h, out float s, out _);
            float radius = _wheelRt.rect.width * 0.5f;
            float angle = h * Mathf.PI * 2f;
            _cursorRt.anchoredPosition = new Vector2(
                Mathf.Cos(angle) * s * radius,
                Mathf.Sin(angle) * s * radius);
            preview.color = _color;
        }

        #endregion


    }
}