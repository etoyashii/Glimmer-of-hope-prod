using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Glimmer/Dialogue/Character")]
    public class CharacterSO : ScriptableObject
    {
        #region Serialized Fields

        [Header("Identity")]
        [SerializeField] private string _characterId;
        [SerializeField] private string _displayName;
        [SerializeField] private Color _nameColor = Color.white;

        [Header("Visuals")]
        [SerializeField] private Sprite _defaultPortrait;
        [SerializeField] private EmotionPortrait[] _emotionPortraits;

        #endregion

        #region Properties

        public string CharacterId => _characterId;
        public string DisplayName => _displayName;
        public Color NameColor => _nameColor;
        public Sprite DefaultPortrait => _defaultPortrait;

        #endregion

        #region Public Methods

        public Sprite GetPortrait(EmotionType emotion)
        {
            if (_emotionPortraits == null || _emotionPortraits.Length == 0)
                return _defaultPortrait;

            foreach (var ep in _emotionPortraits)
            {
                if (ep.emotion == emotion)
                    return ep.portrait != null ? ep.portrait : _defaultPortrait;
            }

            return _defaultPortrait;
        }

        #endregion
    }

    [Serializable]
    public struct EmotionPortrait
    {
        public EmotionType emotion;
        public Sprite portrait;
    }
}
