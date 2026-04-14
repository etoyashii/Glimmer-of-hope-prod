using UnityEngine;
using UnityEngine.EventSystems;

namespace GlimmerOfHope.UI.Widgets
{
    /// <summary>
    /// Adds scale bounce on click and smooth hover to any UI element.
    /// Attach to buttons/cards — no configuration needed.
    /// </summary>
    public class CharacterUIAnimator : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        #region Settings

        [Header("Hover")]
        [SerializeField] private float _hoverScale  = 1.05f;
        [SerializeField] private float _hoverSpeed  = 12f;

        [Header("Click")]
        [SerializeField] private float _clickScale  = 0.92f;
        [SerializeField] private float _bounceScale = 1.08f;
        [SerializeField] private float _bounceSpeed = 16f;

        #endregion

        #region Private Fields

        private Vector3 _baseScale;
        private float _targetScale = 1f;
        private float _currentScale = 1f;
        private bool _isBouncing;
        private float _bounceTimer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            if (_isBouncing)
            {
                _bounceTimer += Time.unscaledDeltaTime * _bounceSpeed;

                if (_bounceTimer < 1f)
                    _currentScale = Mathf.Lerp(_clickScale, _bounceScale, _bounceTimer);
                else if (_bounceTimer < 2f)
                    _currentScale = Mathf.Lerp(_bounceScale, _targetScale, _bounceTimer - 1f);
                else
                {
                    _currentScale = _targetScale;
                    _isBouncing = false;
                }
            }
            else
            {
                _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.unscaledDeltaTime * _hoverSpeed);
            }

            transform.localScale = _baseScale * _currentScale;
        }

        #endregion

        #region Pointer Events

        public void OnPointerEnter(PointerEventData _)
        {
            _targetScale = _hoverScale;
        }

        public void OnPointerExit(PointerEventData _)
        {
            _targetScale = 1f;
        }

        public void OnPointerDown(PointerEventData _)
        {
            _isBouncing  = true;
            _bounceTimer = 0f;
        }

        public void OnPointerUp(PointerEventData _)
        {
            _targetScale = _hoverScale;
        }

        #endregion
    }
}
