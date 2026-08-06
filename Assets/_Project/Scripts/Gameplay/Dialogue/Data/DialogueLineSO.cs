using UnityEngine;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    [CreateAssetMenu(fileName = "New Line", menuName = "Glimmer/Dialogue/Line")]
    public class DialogueLineSO : ScriptableObject
    {
        #region Constants

        public const float DEFAULT_TYPEWRITER_SPEED = 0.03f;
        public const float DEFAULT_AUTO_ADVANCE = 2f;

        #endregion

        #region Serialized Fields

        [Header("Identification")]
        [SerializeField] private string _lineId;
        [SerializeField] private string _conversationId;
        [SerializeField] private int _sequenceOrder;

        [Header("Content")]
        [SerializeField] private CharacterSO _speaker;
        [SerializeField] private EmotionType _emotion = EmotionType.Neutral;

        [Header("Timing")]
        [SerializeField] private float _typewriterSpeed = DEFAULT_TYPEWRITER_SPEED;
        [SerializeField] private bool _waitForInput = true;
        [SerializeField] private float _autoAdvanceDelay = DEFAULT_AUTO_ADVANCE;

        [Header("Branching")]
        [SerializeField] private DialogueChoice[] _choices;
        [SerializeField] private DialogueLineSO _nextLine;
        [SerializeField] private ConditionalNext[] _conditionalNexts;

        [Header("Actions")]
        [SerializeField] private DialogueAction[] _onStartActions;
        [SerializeField] private DialogueAction[] _onEndActions;

        #endregion

        #region Properties

        public string LineId => _lineId;
        public string ConversationId => _conversationId;
        public int SequenceOrder => _sequenceOrder;
        public CharacterSO Speaker => _speaker;
        public EmotionType Emotion => _emotion;
        public float TypewriterSpeed => _typewriterSpeed;
        public bool WaitForInput => _waitForInput;
        public float AutoAdvanceDelay => _autoAdvanceDelay;
        public DialogueChoice[] Choices => _choices;
        public DialogueLineSO NextLine => _nextLine;
        public ConditionalNext[] ConditionalNexts => _conditionalNexts;
        public DialogueAction[] OnStartActions => _onStartActions;
        public DialogueAction[] OnEndActions => _onEndActions;

        public bool HasChoices => _choices != null && _choices.Length > 0;
        public bool HasConditionals => _conditionalNexts != null && _conditionalNexts.Length > 0;

        #endregion

        #region Unity Lifecycle

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_lineId))
                _lineId = name;
        }
#endif

        #endregion
    }
}
