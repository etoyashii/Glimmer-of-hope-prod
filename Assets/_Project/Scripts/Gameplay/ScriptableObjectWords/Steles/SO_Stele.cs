using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SO_Stele", menuName = "Scriptable Objects/SO_Stele")]
    public class SO_Stele : ScriptableObject
    {
        #region SerializeFields

        [SerializeField] private int _id;
        [SerializeField] private List<SO_Glyphe> _currentGlyphes;
        [SerializeField] private List<SO_Glyphe> _expectedGlyphes;

        #endregion

        #region Public Properties

        public List<SO_Glyphe> CurrentGlyphes => _currentGlyphes;
        public List<SO_Glyphe> ExpectedGlyphes => _expectedGlyphes;

        #endregion
    }
}
