using GlimmerOfHope.Gameplay.ScriptableObjects;
using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class GlyphInteraction : MonoBehaviour
    {
        #region Private Fields

        private SO_Glyphe _glyph;

        #endregion

        #region Public Properties

        public SO_Glyphe Glyph
        {
            get => _glyph;
            set => _glyph = value;
        }

        #endregion

        #region Action

        public event Action<SO_Glyphe> OnGlyphClicked;

        #endregion

        void OnMouseDown()
        {
            OnGlyphClicked?.Invoke(Glyph);

            Debug.Log($"Clicked on : {Glyph.GlypheName}");
        }
    }
}
