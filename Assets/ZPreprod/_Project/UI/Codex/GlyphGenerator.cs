using GlimmerOfHope.Gameplay.ScriptableObjects;
using UnityEngine;
using System.Collections.Generic;

namespace GlimmerOfHope.UI
{
    public class GlyphGenerator : MonoBehaviour
    {
              public static GlyphGenerator instance;

        public List<SO_Glyphe> glyphInv;
        private SO_Glyphe.Family _currentFamily;
        [SerializeField] private GameObject _glyphEntryPrefab;
        [SerializeField] private GameObject _GridLayoutGroup;
        [SerializeField] private int _invSize;

        private void Awake()
        {
            instance = this;
        }

        public void ChangeGlyphInv(int GlyphToSwitch)
        {
            _currentFamily = (SO_Glyphe.Family)GlyphToSwitch;
        }

        public void AssembleCodex()
        {
            foreach (var glyph in glyphInv)
            {
                if (glyph.FamilyType != _currentFamily)
                    continue;

                GlyphEntrySetup generatedGlyph = Instantiate(_glyphEntryPrefab, _GridLayoutGroup.transform, false).GetComponent<GlyphEntrySetup>();
                
                generatedGlyph.ObtainGlyphSO(glyph);
                
            }
        }

        public void ClearCodex()
        {
            for (int i = _GridLayoutGroup.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_GridLayoutGroup.transform.GetChild(i).gameObject);
            }
        }

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
    }
}
