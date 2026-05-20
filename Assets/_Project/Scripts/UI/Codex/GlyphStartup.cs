using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlimmerOfHope.UI
{
    public class GlyphStartup : MonoBehaviour
    {
        //[SerializeField] private SO_Glyphe _myGlyph;
        [SerializeField] private Image _mySprite;
        [SerializeField] private TextMeshProUGUI _myTitle;
        [SerializeField] private TextMeshProUGUI _myContext;

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
    }
}
