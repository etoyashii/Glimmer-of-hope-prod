using UnityEngine;

namespace GlimmerOfHope.Gameplay.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SO_Word", menuName = "Scriptable Objects/SO_Word")]
    public class SO_Word : ScriptableObject
    {
        #region SerializeFields

        [Header("Text")]
        [Tooltip("What text will be printed")]
        [SerializeField] string word = default;

        #endregion

        #region Public Methods

        public string GetWord()
        {
            return word;
        }

        #endregion
    }
}
