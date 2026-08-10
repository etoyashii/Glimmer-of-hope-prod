using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.UI.BookMenu.Data;

namespace GlimmerOfHope.UI.BookMenu.Panels
{
    public class PartitionSequenceRow : MonoBehaviour
    {
        #region Private Fields

        [Header("Dot Colors")]
        [SerializeField] private Color _red = new Color(0.75f, 0.22f, 0.17f);
        [SerializeField] private Color _blue = new Color(0.18f, 0.44f, 0.65f);
        [SerializeField] private Color _yellow = new Color(0.83f, 0.67f, 0.17f);
        [SerializeField] private Color _black = new Color(0.17f, 0.15f, 0.13f);

        [Header("References")]
        [Tooltip("Prefab of a single round dot (Image)")]
        [SerializeField] private GameObject _dotPrefab;
        [Tooltip("Horizontal Layout Group container")]
        [SerializeField] private Transform _dotsContainer;

        #endregion

        #region Public Methods

        public void Setup(NoteColor[] sequence)
        {
            for (int i = _dotsContainer.childCount - 1; i >= 0; i--)
                Destroy(_dotsContainer.GetChild(i).gameObject);

            foreach (var color in sequence)
            {
                var dotInstance = Instantiate(_dotPrefab, _dotsContainer);
                var image = dotInstance.GetComponent<Image>();
                if (image != null) image.color = ColorFor(color);
            }
        }

        #endregion

        #region Private Methods

        private Color ColorFor(NoteColor color)
        {
            return color switch
            {
                NoteColor.Red => _red,
                NoteColor.Blue => _blue,
                NoteColor.Yellow => _yellow,
                _ => _black,
            };
        }

        #endregion
    }
}