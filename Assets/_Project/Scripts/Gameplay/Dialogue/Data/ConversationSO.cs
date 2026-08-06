using UnityEngine;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    [CreateAssetMenu(fileName = "New Conversation", menuName = "Glimmer/Dialogue/Conversation")]
    public class ConversationSO : ScriptableObject
    {
        #region Serialized Fields

        [Header("Metadata")]
        [SerializeField] private string _conversationId;
        [SerializeField] private string _displayName;
        [SerializeField] private ConversationType _type = ConversationType.Standard;

        [Header("Content")]
        [SerializeField] private DialogueLineSO _startLine;
        [SerializeField] private DialogueLineSO[] _allLines;

        [Header("Conditions")]
        [SerializeField] private string[] _requiredFlags;
        [SerializeField] private string[] _setFlagsOnComplete;

        #endregion

        #region Properties

        public string ConversationId => _conversationId;
        public string DisplayName => _displayName;
        public ConversationType Type => _type;
        public DialogueLineSO StartLine => _startLine;
        public DialogueLineSO[] AllLines => _allLines;
        public string[] RequiredFlags => _requiredFlags;
        public string[] SetFlagsOnComplete => _setFlagsOnComplete;

        public bool HasRequiredFlags => _requiredFlags != null && _requiredFlags.Length > 0;

        #endregion

        #region Unity Lifecycle

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_conversationId))
                _conversationId = name;

            if (string.IsNullOrWhiteSpace(_displayName))
                _displayName = name;
        }
#endif

        #endregion
    }
}
