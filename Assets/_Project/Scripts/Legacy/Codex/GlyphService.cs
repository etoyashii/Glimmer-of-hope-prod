using GlimmerOfHope.Gameplay.ScriptableObjects;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.UI
{
    public class GlyphService : MonoBehaviour
    {
        public static GlyphService instance;

        private GlyphGenerator _glyphStorage => GlyphGenerator.instance;


        private void Awake()
        {
            instance = this;
        }

        public void AddGlyph(SO_Glyphe glyphToAdd)
        {
            _glyphStorage.AddCodexGlyph(glyphToAdd);
        }

        public void RemoveGlyph(SO_Glyphe glyphToRemove)
        {
            _glyphStorage.RemoveCodexGlyph(glyphToRemove);
        }
    }
}
