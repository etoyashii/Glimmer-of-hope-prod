using System;
using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.UI
{

    /// <summary>
    /// 
    /// </summary>
    public class CharacterColorPresetButton : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private Color _color;
        [SerializeField] private Button _button;
        #endregion

        public static event Action<Color> OnSelectColor;

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }


        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            OnSelectColor?.Invoke(_color);
        }
    }
}