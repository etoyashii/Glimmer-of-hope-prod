using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.Core.Events;

namespace GlimmerOfHope.UI.Widgets
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CharacterColorPopup : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onColorRequested;
        [SerializeField] private Button _closeButton;

        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            SetOpen(false);
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
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            _group.alpha = open ? 1f : 0f;
            _group.blocksRaycasts = open;
            _group.interactable = open;
        }
    }
}
