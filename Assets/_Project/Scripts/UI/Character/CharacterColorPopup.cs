using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.Core.Events;
using DG.Tweening;
using System.Collections.Generic;

namespace GlimmerOfHope.UI.Widgets
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CharacterColorPopup : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float _showAnimTime = 0.5f;
        [SerializeField] private AnimationCurve _showAnimEase;
        [SerializeField] private float _hideAnimTime = 0.5f;
        [SerializeField] private AnimationCurve _hideAnimEase;
        [SerializeField] private Ease _animatedElementsEase = Ease.OutBack;
        [SerializeField] private float _animatedElementsShowTime = 0.2f;
        [SerializeField] private List<Transform> _animatedElements;
        [SerializeField] private float _timeBetweenAmimatedElements = 0.1f;
        [Header("References")]
        [SerializeField] private VoidEventChannel _onColorRequested;
        [SerializeField] private Button _closeButton;

        private bool _isOpen = false;

        private void Awake()
        {
            transform.localScale = Vector3.zero;
        }

        private void OnEnable()
        {
            if (_onColorRequested != null)
                _onColorRequested.Subscribe(Open);
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (_onColorRequested != null)
                _onColorRequested.Unsubscribe(Open);
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            if (_isOpen)
                return;
            _isOpen = true;
            HideAnimatedElements();
            transform.DOKill();
            Sequence sequence = DOTween.Sequence();

            sequence.Append(transform.DOScale(1f, _showAnimTime)
                .SetEase(_showAnimEase)
                .ChangeStartValue(Vector3.zero));

            foreach (Transform element in _animatedElements)
            {
                sequence.Append(element.DOScale(1f, _animatedElementsShowTime).SetEase(_animatedElementsEase));
                sequence.AppendInterval(_timeBetweenAmimatedElements);
            }
        }

        public void Close()
        {
            _isOpen = false;
            transform.DOKill();
            transform.DOScale(0f, _hideAnimTime)
                .SetEase(_hideAnimEase);
        }

        private void HideAnimatedElements()
        {
            foreach (Transform element in _animatedElements)
                element.transform.localScale = Vector3.zero;
        }
    }
}
