using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlimmerOfHope.UI.BookMenu;

namespace GlimmerOfHope.UI.BookMenu.Panels
{
    public class ProfilePanel : MonoBehaviour, IBookPage
    {
        #region Private Fields

        [Header("Left Page")]
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private Image _playerPortrait;

        [Header("Right Page")]
        [SerializeField] private Image _rightImage;

        #endregion

        #region Public Methods

        public void SetPlayerData(string playerName, Sprite portrait, Sprite rightImageSprite)
        {
            if (_nameInput != null) _nameInput.text = playerName;
            if (_playerPortrait != null) _playerPortrait.sprite = portrait;
            if (_rightImage != null) _rightImage.sprite = rightImageSprite;
        }

        public void OnPageShown()
        {
        }

        #endregion
    }
}