using GlimmerOfHope.Gameplay.ScriptableObjects;
using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class ExchangeGlyphManager : MonoBehaviour
    {
         public static ExchangeGlyphManager Instance { get; private set; }

        private SO_Glyphe _selectedGlyph;

        public SO_Glyphe SelectedGlyph
        {
            get => _selectedGlyph;
            set => _selectedGlyph = value;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            SelectedGlyph = null;
        }
    }
}
