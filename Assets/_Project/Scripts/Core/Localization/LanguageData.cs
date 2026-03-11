using UnityEngine;

namespace GlimmerOfHope.Core.Localization
{
    [CreateAssetMenu(fileName = "New Language", menuName = "Glimmer/Localization/Language Data")]
    public class LanguageData : ScriptableObject
    {
        [SerializeField] private string _languageCode;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _isRightToLeft;

        public string LanguageCode => _languageCode;
        public string DisplayName => _displayName;
        public bool IsRightToLeft => _isRightToLeft;
    }
}
