using GlimmerOfHope.Gameplay;
using UnityEngine;

namespace GlimmerOfHope.UI
{
    /// <summary>
    /// Un script service, rend les demandes de modification du CodexModel plus clean.
    /// </summary>
    public class GlyphInvManager : MonoBehaviour
    {
        
        #region Private Fields
        
        [SerializeField] private CodexModel _glyphStorage;

        #endregion

        #region Public Methods
        
        public void AddGlyph(SO_Glyphe glyphToAdd)
        {
            _glyphStorage.AddCodexGlyph(glyphToAdd);
        }

        public void RemoveGlyph(SO_Glyphe glyphToRemove)
        {
            _glyphStorage.RemoveCodexGlyph(glyphToRemove);
        }
        
        #endregion
        
    }
}
