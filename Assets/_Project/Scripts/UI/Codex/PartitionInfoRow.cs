using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlimmerOfHope.UI.BookMenu.Data;

namespace GlimmerOfHope.UI.BookMenu.Panels
{
    public class PartitionInfoRow : MonoBehaviour
    {
        #region Private Fields

        [SerializeField] private Image _thumbnail;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;

        #endregion

        #region Public Methods

        public void Setup(PartitionData data)
        {
            if (_thumbnail != null) _thumbnail.sprite = data.Thumbnail;
            if (_titleText != null) _titleText.text = data.Title;
            if (_descriptionText != null) _descriptionText.text = data.Description;
        }

        #endregion
    }
}