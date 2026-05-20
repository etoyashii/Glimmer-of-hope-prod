using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.UI
{
    public class CodexModel : MonoBehaviour
    {
        //public List<SO_Glyphe> glyphInv;
        //private SO_Glyphe.Family _currentFamily;
        [SerializeField] private GameObject _glyphPrefab;
        [SerializeField] private GameObject _codexLayoutGroup;
        [SerializeField] private int _invSize;



        public void ChangeCodexFamily(int GlyphToSwitch)
        {
            //_currentFamily = (SO_Glyphe.Family)GlyphToSwitch;
        }

        /*
        public void BuildCodex()
        {
            foreach (var glyph in glyphInv)
            {
                if (glyph.FamilyType != _currentFamily)
                    continue;

                GlyphEntrySetup generatedGlyph = Instantiate(_glyphPrefab, _codexLayoutGroup.transform, false).GetComponent<GlyphEntrySetup>();

                generatedGlyph.ObtainGlyphSO(glyph);

            }
        }
        */

        public void DestroyCodex()
        {
            for (int i = _codexLayoutGroup.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_codexLayoutGroup.transform.GetChild(i).gameObject);
            }
        }

        /*
        public void AddCodexGlyph(SO_Glyphe glyphToAdd)
        {
            if (glyphInv.Count < _invSize)
                glyphInv.Add(glyphToAdd);
            else
                Debug.Log("Inventory full!");
        }

        public void RemoveCodexGlyph(SO_Glyphe glyphToRemove)
        {
            if (glyphInv.Contains(glyphToRemove))
                glyphInv.Remove(glyphToRemove);
            else
                Debug.Log("No glyph in the inventory!");
        }
        */
    }
}
