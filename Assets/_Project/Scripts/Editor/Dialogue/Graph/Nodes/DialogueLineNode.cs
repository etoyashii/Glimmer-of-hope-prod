using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class DialogueLineNode : Node
    {
        #region Private Fields

        private DialogueLineSO _lineSO;
        private Port _inputPort;
        private Port _defaultOutputPort;
        private readonly List<Port> _choicePorts = new();
        private readonly List<Port> _conditionPorts = new();

        private Label _speakerLabel;
        private Label _emotionLabel;
        private TextField _textField;
        private Label _timingLabel;
        private Label _actionsLabel;
        private VisualElement _choiceContainer;
        private VisualElement _conditionContainer;

        private string _lineId;
        private string _activeLanguage = "fr";
        private Dictionary<string, string> _localizedTexts = new();
        private ChoiceManager _choiceManager;

        #endregion

        #region Properties

        public DialogueLineSO LineSO => _lineSO;
        public string LineId => _lineId;
        public Port InputPort => _inputPort;
        public Port DefaultOutputPort => _defaultOutputPort;
        public IReadOnlyList<Port> ChoicePorts => _choicePorts;
        public IReadOnlyList<Port> ConditionPorts => _conditionPorts;
        public Dictionary<string, string> LocalizedTexts => _localizedTexts;
        public ChoiceManager Choices => _choiceManager;

        #endregion

        #region Public Methods

        public void Initialize(
            DialogueLineSO lineSO,
            Dictionary<string, string> texts,
            Dictionary<string, Dictionary<string, string>> allLangData = null)
        {
            _lineSO = lineSO;
            _lineId = lineSO != null ? lineSO.LineId : System.Guid.NewGuid().ToString("N")[..8];
            _localizedTexts = texts ?? new Dictionary<string, string>();

            AddToClassList("dialogue-line-node");
            SetupPorts();
            BuildBody();

            _choiceManager.LoadChoiceTexts(lineSO, allLangData);
            _choiceManager.RebuildUI("fr");

            RebuildConditionPorts();
            UpdateVisuals("fr");
            RefreshExpandedState();
            RefreshPorts();
        }

        public void InitializeEmpty(string lineId)
        {
            _lineSO = null;
            _lineId = lineId;
            _localizedTexts = new Dictionary<string, string>();

            AddToClassList("dialogue-line-node");
            SetupPorts();
            BuildBody();
            UpdateVisuals("fr");
            RefreshExpandedState();
            RefreshPorts();
        }

        public void UpdateVisuals(string language)
        {
            _activeLanguage = language;
            UpdateTitle();
            UpdateSpeakerLabel();
            UpdateEmotionLabel();
            UpdateTextField(language);
            UpdateTimingLabel();
            UpdateActionsLabel();
            _choiceManager?.RefreshLanguage(language);
        }

        public void SetLocalizedText(string language, string text)
        {
            _localizedTexts[language] = text;
        }

        public string GetLocalizedText(string language)
        {
            return _localizedTexts.TryGetValue(language, out var text) ? text : "";
        }

        public void RebuildConditionPorts()
        {
            ClearDynamicPorts(_conditionPorts, _conditionContainer);

            if (_lineSO == null || !_lineSO.HasConditionals)
                return;

            for (int i = 0; i < _lineSO.ConditionalNexts.Length; i++)
            {
                var cond = _lineSO.ConditionalNexts[i];
                var port = DialoguePortFactory.CreateConditionOutput(this, cond.condition, i);
                _conditionPorts.Add(port);
                _conditionContainer.Add(port);
            }

            RefreshPorts();
        }

        public void LinkSO(DialogueLineSO so)
        {
            _lineSO = so;
            if (so != null)
                _lineId = so.LineId;
        }

        public void CollectChoiceLocData(Dictionary<string, Dictionary<string, string>> dataPerLang)
        {
            _choiceManager?.CollectLocalizationData(_lineId, dataPerLang);
        }

        #endregion

        #region Private Methods — Setup

        private void SetupPorts()
        {
            _inputPort = DialoguePortFactory.CreateFlowInput(this);
            inputContainer.Add(_inputPort);

            _defaultOutputPort = DialoguePortFactory.CreateFlowOutput(this);
            outputContainer.Add(_defaultOutputPort);
        }

        private void BuildBody()
        {
            var body = new VisualElement();
            body.AddToClassList("dialogue-line-node__body");

            _speakerLabel = new Label();
            _speakerLabel.AddToClassList("dialogue-line-node__speaker");
            body.Add(_speakerLabel);

            _emotionLabel = new Label();
            _emotionLabel.AddToClassList("dialogue-line-node__emotion");
            body.Add(_emotionLabel);

            _textField = new TextField();
            _textField.multiline = true;
            _textField.AddToClassList("dialogue-line-node__text");
            _textField.style.whiteSpace = WhiteSpace.Normal;
            _textField.style.minHeight = 36;
            _textField.RegisterValueChangedCallback(OnTextFieldChanged);
            body.Add(_textField);

            _timingLabel = new Label();
            _timingLabel.AddToClassList("dialogue-line-node__timing");
            body.Add(_timingLabel);

            _actionsLabel = new Label();
            _actionsLabel.AddToClassList("dialogue-line-node__actions");
            body.Add(_actionsLabel);

            extensionContainer.Add(body);

            _choiceContainer = new VisualElement();
            _choiceContainer.AddToClassList("dialogue-line-node__choices");
            extensionContainer.Add(_choiceContainer);

            _choiceManager = new ChoiceManager(this, _choicePorts, _choiceContainer, lang =>
            {
            });

            _conditionContainer = new VisualElement();
            _conditionContainer.AddToClassList("dialogue-line-node__conditions");
            extensionContainer.Add(_conditionContainer);
        }

        #endregion

        #region Private Methods — Updates

        private void UpdateTitle()
        {
            title = _lineId ?? "(sans id)";
        }

        private void UpdateSpeakerLabel()
        {
            var name = (_lineSO != null && _lineSO.Speaker != null) ? _lineSO.Speaker.DisplayName : "(aucun)";
            _speakerLabel.text = $"Speaker: {name}";
        }

        private void UpdateEmotionLabel()
        {
            var emotion = _lineSO != null ? _lineSO.Emotion : EmotionType.Neutral;
            _emotionLabel.text = $"Emotion: {emotion}";
        }

        private void UpdateTextField(string language)
        {
            _textField.SetValueWithoutNotify(GetLocalizedText(language));
        }

        private void OnTextFieldChanged(ChangeEvent<string> evt)
        {
            SetLocalizedText(_activeLanguage, evt.newValue);
        }

        private void UpdateTimingLabel()
        {
            if (_lineSO == null) { _timingLabel.text = ""; return; }
            var wait = _lineSO.WaitForInput ? "Wait" : $"Auto {_lineSO.AutoAdvanceDelay:F1}s";
            _timingLabel.text = $"Speed: {_lineSO.TypewriterSpeed:F3} | {wait}";
        }

        private void UpdateActionsLabel()
        {
            if (_lineSO == null) { _actionsLabel.text = ""; return; }
            int s = _lineSO.OnStartActions?.Length ?? 0;
            int e = _lineSO.OnEndActions?.Length ?? 0;
            _actionsLabel.style.display = (s + e == 0) ? DisplayStyle.None : DisplayStyle.Flex;
            _actionsLabel.text = $"Actions: {s} start, {e} end";
        }

        private void ClearDynamicPorts(List<Port> ports, VisualElement container)
        {
            foreach (var port in ports)
            {
                foreach (var edge in port.connections)
                    edge.RemoveFromHierarchy();
                container.Remove(port);
            }
            ports.Clear();
        }

        #endregion
    }
}
