using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.Core.Events;

namespace GlimmerOfHope.UI.Widgets
{
    [RequireComponent(typeof(Button))]
    public class PartColorButton : MonoBehaviour
    {
        [SerializeField] private Button _partButton;
        [SerializeField] private VoidEventChannel _onColorRequested;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (_partButton != null)
                _partButton.onClick.Invoke();

            if (_onColorRequested != null)
                _onColorRequested.Raise();
        }
    }
}
