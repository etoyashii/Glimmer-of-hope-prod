using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GlimmerOfHope.Gameplay.Dialogue;
using System.Collections.Generic;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class NodeInspectorPanel : VisualElement
    {
        #region Private Fields

        private DialogueLineNode _selectedNode;
        private SerializedObject _serializedLine;
        private DialogueGraphView _graphView;

        private Label _lineIdLabel;
        private ObjectField _speakerField;
        private EnumField _emotionField;
        private Slider _speedSlider;
        private Toggle _waitToggle;
        private FloatField _autoDelayField;
        private LocalizationFieldGroup _textFields;
        private ActionListDrawer _startActions;
        private ActionListDrawer _endActions;
        private VisualElement _choicesLocContainer;
        private VisualElement _contentContainer;
        private Label _emptyLabel;

        #endregion

        #region Constructor

        public NodeInspectorPanel(DialogueGraphView graphView)
        {
            _graphView = graphView;

            AddToClassList("node-inspector");
            style.width = 320;
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0.15f, 0.15f, 0.15f);
            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            BuildHeader();
            BuildEmptyState();
            BuildContent();
            ShowEmpty();
        }

        #endregion

        #region Public Methods

        public void InspectNode(DialogueLineNode node)
        {
            _selectedNode = node;

            if (node == null || node.LineSO == null)
            {
                ShowEmpty();
                return;
            }

            _serializedLine = new SerializedObject(node.LineSO);
            ShowContent();
            PopulateFields();
        }

        public void ClearInspector()
        {
            _selectedNode = null;
            _serializedLine = null;
            _startActions.Unbind();
            _endActions.Unbind();
            ShowEmpty();
        }

        #endregion

        #region Private Methods — Build

        private void BuildHeader()
        {
            var header = new Label("Inspector");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 14;
            header.style.paddingLeft = 8;
            header.style.paddingTop = 8;
            Add(header);
        }

        private void BuildEmptyState()
        {
            _emptyLabel = new Label("Selectionnez un noeud");
            _emptyLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            _emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _emptyLabel.style.marginTop = 40;
            Add(_emptyLabel);
        }

        private void BuildContent()
        {
            _contentContainer = new VisualElement();
            _contentContainer.style.paddingLeft = 8;
            _contentContainer.style.paddingRight = 8;
            _contentContainer.style.paddingTop = 8;

            var scroll = new ScrollView(ScrollViewMode.Vertical);

            _lineIdLabel = new Label();
            _lineIdLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            _lineIdLabel.style.marginBottom = 8;
            scroll.Add(_lineIdLabel);

            scroll.Add(CreateSection("Personnage"));
            _speakerField = new ObjectField("Speaker") { objectType = typeof(CharacterSO) };
            _speakerField.RegisterValueChangedCallback(OnSpeakerChanged);
            scroll.Add(_speakerField);

            _emotionField = new EnumField("Emotion", EmotionType.Neutral);
            _emotionField.RegisterValueChangedCallback(OnEmotionChanged);
            scroll.Add(_emotionField);

            scroll.Add(CreateSeparator());

            _textFields = new LocalizationFieldGroup("Texte Dialogue", OnTextChanged);
            scroll.Add(_textFields);

            scroll.Add(CreateSeparator());
            scroll.Add(CreateSection("Timing"));

            _speedSlider = new Slider("Vitesse", 0.01f, 0.1f);
            _speedSlider.RegisterValueChangedCallback(OnSpeedChanged);
            scroll.Add(_speedSlider);

            _waitToggle = new Toggle("Attendre Input");
            _waitToggle.RegisterValueChangedCallback(OnWaitChanged);
            scroll.Add(_waitToggle);

            _autoDelayField = new FloatField("Auto Delay (s)");
            _autoDelayField.RegisterValueChangedCallback(OnAutoDelayChanged);
            scroll.Add(_autoDelayField);

            scroll.Add(CreateSeparator());

            _startActions = new ActionListDrawer("Actions Debut", "_onStartActions", OnActionsChanged);
            scroll.Add(_startActions);

            _endActions = new ActionListDrawer("Actions Fin", "_onEndActions", OnActionsChanged);
            scroll.Add(_endActions);

            scroll.Add(CreateSeparator());
            scroll.Add(CreateSection("Choix — Localisation"));
            _choicesLocContainer = new VisualElement();
            scroll.Add(_choicesLocContainer);

            _contentContainer.Add(scroll);
            Add(_contentContainer);
        }

        #endregion

        #region Private Methods — Populate

        private void PopulateFields()
        {
            if (_selectedNode == null || _serializedLine == null) return;

            var lineSO = _selectedNode.LineSO;
            _lineIdLabel.text = $"ID: {lineSO.LineId}";
            _speakerField.SetValueWithoutNotify(lineSO.Speaker);
            _emotionField.SetValueWithoutNotify(lineSO.Emotion);
            _speedSlider.SetValueWithoutNotify(lineSO.TypewriterSpeed);
            _waitToggle.SetValueWithoutNotify(lineSO.WaitForInput);
            _autoDelayField.SetValueWithoutNotify(lineSO.AutoAdvanceDelay);
            _autoDelayField.SetEnabled(!lineSO.WaitForInput);

            _textFields.SetTexts(_selectedNode.LocalizedTexts);
            _startActions.Bind(_serializedLine);
            _endActions.Bind(_serializedLine);
            PopulateChoicesLoc();
        }

        private void PopulateChoicesLoc()
        {
            _choicesLocContainer.Clear();

            if (_selectedNode?.Choices == null || _selectedNode.LineSO == null)
                return;

            var choices = _selectedNode.LineSO.Choices;
            if (choices == null || choices.Length == 0)
            {
                _choicesLocContainer.Add(new Label("Aucun choix") { style = { color = new Color(0.5f, 0.5f, 0.5f) } });
                return;
            }

            for (int i = 0; i < choices.Length; i++)
            {
                int idx = i;
                var group = new LocalizationFieldGroup($"Choix {i + 1}", (lang, text) =>
                {
                    _selectedNode.Choices.SetChoiceText(idx, lang, text);
                    _selectedNode.UpdateVisuals(_graphView.ActiveLanguage);
                });

                var texts = new Dictionary<string, string>();
                foreach (var lang in DialogueCSVFormat.LANGUAGES)
                    texts[lang] = _selectedNode.Choices.GetChoiceText(idx, lang);

                group.SetTexts(texts);
                _choicesLocContainer.Add(group);
            }
        }

        #endregion

        #region Private Methods — Callbacks

        private void OnSpeakerChanged(ChangeEvent<Object> evt)
        {
            if (_serializedLine == null) return;
            _serializedLine.FindProperty("_speaker").objectReferenceValue = evt.newValue;
            ApplyAndRefresh();
        }

        private void OnEmotionChanged(ChangeEvent<System.Enum> evt)
        {
            if (_serializedLine == null) return;
            _serializedLine.FindProperty("_emotion").enumValueIndex = (int)(EmotionType)evt.newValue;
            ApplyAndRefresh();
        }

        private void OnSpeedChanged(ChangeEvent<float> evt)
        {
            if (_serializedLine == null) return;
            _serializedLine.FindProperty("_typewriterSpeed").floatValue = evt.newValue;
            ApplyAndRefresh();
        }

        private void OnWaitChanged(ChangeEvent<bool> evt)
        {
            if (_serializedLine == null) return;
            _serializedLine.FindProperty("_waitForInput").boolValue = evt.newValue;
            _autoDelayField.SetEnabled(!evt.newValue);
            ApplyAndRefresh();
        }

        private void OnAutoDelayChanged(ChangeEvent<float> evt)
        {
            if (_serializedLine == null) return;
            _serializedLine.FindProperty("_autoAdvanceDelay").floatValue = evt.newValue;
            ApplyAndRefresh();
        }

        private void OnTextChanged(string language, string text)
        {
            _selectedNode?.SetLocalizedText(language, text);
            _selectedNode?.UpdateVisuals(_graphView.ActiveLanguage);
        }

        private void OnActionsChanged()
        {
            _selectedNode?.UpdateVisuals(_graphView.ActiveLanguage);
        }

        #endregion

        #region Private Methods — Helpers

        private void ApplyAndRefresh()
        {
            _serializedLine?.ApplyModifiedProperties();
            _selectedNode?.UpdateVisuals(_graphView.ActiveLanguage);
        }

        private void ShowEmpty() { _emptyLabel.style.display = DisplayStyle.Flex; _contentContainer.style.display = DisplayStyle.None; }
        private void ShowContent() { _emptyLabel.style.display = DisplayStyle.None; _contentContainer.style.display = DisplayStyle.Flex; }

        private Label CreateSection(string text)
        {
            return new Label(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8, marginBottom = 4 } };
        }

        private VisualElement CreateSeparator()
        {
            return new VisualElement { style = { height = 1, marginTop = 6, marginBottom = 6, backgroundColor = new Color(0.3f, 0.3f, 0.3f) } };
        }

        #endregion
    }

}
