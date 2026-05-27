using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    [CreateAssetMenu(fileName = "SO_Glyphe", menuName = "Scriptable Objects/SO_Glyphe")]
    public class SO_Glyphe : ScriptableObject
    {
        #region State

        public enum Family
        {
            State,
            Action,
            Link,
            Elements
        }

        #endregion

        #region SerializeFields

        [SerializeField] private int _id;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private string _name;
        [SerializeField] private string _discoverContext;
        [SerializeField] private Family _family;

        #endregion

        #region Public Properties

        public int Id => _id;
        public Sprite Sprite => _sprite;
        public string GlypheName => _name;
        public string DiscoverContext => _discoverContext;
        public Family FamilyType => _family;

        #endregion

    }
}
