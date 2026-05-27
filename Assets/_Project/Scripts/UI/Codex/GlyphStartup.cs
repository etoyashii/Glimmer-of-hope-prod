using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlimmerOfHope.UI
{
    /// <summary>
    /// Le script qui gere les informations de chaque "slot" dans le codex et les fonctions pertinente a un glyphe dans le codex.
    /// </summary>
    public class GlyphStartup : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Données Glyphe")]
        //[SerializeField] private SO_Glyphe _myGlyph;
        [SerializeField] private Image _mySprite;
        [SerializeField] private TextMeshProUGUI _myTitle;
        [SerializeField] private TextMeshProUGUI _myContext;

        #endregion

        #region Public Methods

        public void UpdateGlyphSprite()
        {
            //_mySprite.sprite = _myGlyph.Sprite;
        }

        public void UpdateGlyphTitle()
        {
            //_myTitle.text = _myGlyph.GlypheName;
        }
        public void UpdateGlyphContext()
        {
            //_myContext.text = _myGlyph.DiscoverContext;
        }

        /*
        public void ObtainGlyphSO(SO_Glyphe GlyphSO)
        {
            _myGlyph = GlyphSO;
            UpdateGlyphSprite();
            UpdateGlyphTitle();
            UpdateGlyphContext();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (ExchangeGlyphManager.Instance.SelectedGlyph == null)
                Debug.Log("null selectedGlyph");
            else
                Debug.Log(ExchangeGlyphManager.Instance.SelectedGlyph.GlypheName);

            ExchangeGlyphManager.Instance.SelectedGlyph = _myGlyph;

            Debug.Log(ExchangeGlyphManager.Instance.SelectedGlyph.GlypheName);
        }
        */
        #endregion
    }
}
