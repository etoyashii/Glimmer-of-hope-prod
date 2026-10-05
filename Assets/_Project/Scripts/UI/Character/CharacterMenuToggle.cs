using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.UI.Widgets
{
    [RequireComponent(typeof(Button))]
    public class CharacterMenuToggle : MonoBehaviour
    {
        [SerializeField] private GameObject _subMenu;
        [SerializeField] private GameObject[] _otherSubMenus;
        [SerializeField] private bool _openOnStart;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Toggle);
        }

        private void Start()
        {
            if (_openOnStart)
                SetOpen(true);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Toggle);
        }

        private void Toggle()
        {
            if (_subMenu == null)
                return;

            SetOpen(!_subMenu.activeSelf);
        }

        private void SetOpen(bool open)
        {
            foreach (var menu in _otherSubMenus)
            {
                if (menu != null)
                    menu.SetActive(false);
            }

            if (_subMenu != null)
                _subMenu.SetActive(open);
        }
    }
}
