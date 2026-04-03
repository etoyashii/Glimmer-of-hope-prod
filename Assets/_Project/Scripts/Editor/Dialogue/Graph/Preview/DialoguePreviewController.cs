using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class DialoguePreviewController : VisualElement
    {
        #region Constants

        private const float TYPEWRITER_INTERVAL = 0.03f;

        #endregion

        #region Private Fields

        private readonly DialogueGraphView _graphView;
        private readonly PreviewFlagManager _flagManager;
        private readonly PreviewPlayback _playback;

        private DialogueLineNode _currentNode;
        private bool _isPlaying;
        private string _currentText;
        private int _visibleChars;
        private double _lastCharTime;

        private Label _speakerLabel;
        private Label _textLabel;
        private VisualElement _choicesContainer;
        private Label _flagsLabel;
        private Button _nextButton;
        private VisualElement _panelRoot;

        #endregion

        #region Properties

        public bool IsPlaying => _isPlaying;

        #endregion

        #region Constructor

        public DialoguePreviewController(DialogueGraphView graphView)
        {
            _graphView = graphView;
            _flagManager = new PreviewFlagManager();
            _playback = new PreviewPlayback(_flagManager);

            BuildUI();
            Hide();
        }

        #endregion

        #region Public Methods

        public void StartPreview()
        {
            _flagManager.ClearAll();
            _isPlaying = true;
            Show();

            var startNode = _graphView.GetStartNode();
            if (startNode == null || !startNode.OutputPort.connected)
            {
                ShowMessage("Erreur: pas de noeud START ou pas connecte");
                return;
            }

            var firstEdge = startNode.OutputPort.connections.FirstOrDefault();
            var firstLine = firstEdge?.input?.node as DialogueLineNode;

            if (firstLine != null)
                PlayNode(firstLine);
            else
                ShowMessage("Erreur: START non connecte a une ligne");
        }

        public void StopPreview()
        {
            _isPlaying = false;
            _currentNode = null;
            ClearHighlight();
            Hide();
        }

        public void Update()
        {
            if (!_isPlaying || _currentText == null)
                return;

            if (_visibleChars < _currentText.Length)
                UpdateTypewriter();
        }

        #endregion

        #region Private Methods — UI

        private void BuildUI()
        {
            _panelRoot = new VisualElement();
            _panelRoot.style.height = 160;
            _panelRoot.style.borderTopWidth = 2;
            _panelRoot.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            _panelRoot.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            _panelRoot.style.paddingLeft = 12;
            _panelRoot.style.paddingRight = 12;
            _panelRoot.style.paddingTop = 6;

            _flagsLabel = CreateLabel("Flags: (aucun)", 10, new Color(0.6f, 0.6f, 0.6f));
            _speakerLabel = CreateLabel("", 12, new Color(0.5f, 0.8f, 1f));
            _speakerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _textLabel = CreateLabel("", 14, Color.white);
            _textLabel.style.whiteSpace = WhiteSpace.Normal;
            _textLabel.style.minHeight = 36;

            _choicesContainer = new VisualElement();
            _choicesContainer.style.flexDirection = FlexDirection.Row;
            _choicesContainer.style.flexWrap = Wrap.Wrap;
            _choicesContainer.style.marginTop = 4;

            _nextButton = new Button(OnNextClicked) { text = "Suivant" };
            _nextButton.style.width = 100;
            var stopBtn = new Button(StopPreview) { text = "Arreter" };
            stopBtn.style.marginLeft = 8;
            stopBtn.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
            var controls = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
            controls.Add(_nextButton);
            controls.Add(stopBtn);

            _panelRoot.Add(_flagsLabel);
            _panelRoot.Add(_speakerLabel);
            _panelRoot.Add(_textLabel);
            _panelRoot.Add(_choicesContainer);
            _panelRoot.Add(controls);
            Add(_panelRoot);
        }

        private Label CreateLabel(string text, int fontSize, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = color;
            return label;
        }

        #endregion

        #region Private Methods — Playback

        private void PlayNode(DialogueLineNode node)
        {
            ClearHighlight();
            _currentNode = node;
            node.AddToClassList("preview-active");

            var lang = _graphView.ActiveLanguage;
            var speaker = node.LineSO?.Speaker?.DisplayName ?? "(narrateur)";

            _speakerLabel.text = speaker;
            _currentText = node.GetLocalizedText(lang);
            _visibleChars = 0;
            _lastCharTime = EditorApplication.timeSinceStartup;
            _textLabel.text = "";
            _choicesContainer.Clear();
            _nextButton.SetEnabled(true);

            _playback.ExecuteActions(node.LineSO?.OnStartActions);
            UpdateFlagsLabel();
        }

        private void UpdateTypewriter()
        {
            var now = EditorApplication.timeSinceStartup;
            var speed = _currentNode?.LineSO?.TypewriterSpeed ?? TYPEWRITER_INTERVAL;

            if (now - _lastCharTime < speed) return;

            _visibleChars++;
            _lastCharTime = now;
            _textLabel.text = _currentText[.._visibleChars];

            if (_visibleChars >= _currentText.Length)
                OnTypewriterComplete();
        }

        private void OnTypewriterComplete()
        {
            _textLabel.text = _currentText;

            if (_currentNode != null && _currentNode.ChoicePorts.Count > 0)
            {
                ShowChoices();
                _nextButton.SetEnabled(false);
            }
        }

        private void ShowChoices()
        {
            _choicesContainer.Clear();
            if (_currentNode?.LineSO?.Choices == null) return;

            for (int i = 0; i < _currentNode.LineSO.Choices.Length; i++)
            {
                int index = i;
                var choice = _currentNode.LineSO.Choices[i];
                var btn = new Button(() => OnChoiceSelected(index)) { text = choice.choiceText };
                btn.style.marginRight = 8;
                btn.style.paddingLeft = 12;
                btn.style.paddingRight = 12;
                btn.style.backgroundColor = new Color(0.2f, 0.35f, 0.5f);
                _choicesContainer.Add(btn);
            }
        }

        private void OnNextClicked()
        {
            if (_currentNode == null) return;

            if (_visibleChars < _currentText.Length)
            {
                _visibleChars = _currentText.Length;
                _textLabel.text = _currentText;
                OnTypewriterComplete();
                return;
            }

            _playback.ExecuteActions(_currentNode.LineSO?.OnEndActions);
            var nextNode = _playback.FindNextNode(_currentNode);
            if (nextNode != null) PlayNode(nextNode);
            else ShowMessage("Fin du dialogue");
        }

        private void OnChoiceSelected(int index)
        {
            if (_currentNode?.LineSO?.Choices == null) return;

            var choice = _currentNode.LineSO.Choices[index];
            if (!string.IsNullOrEmpty(choice.setFlag))
                _flagManager.SetFlag(choice.setFlag);

            _playback.ExecuteActions(_currentNode.LineSO?.OnEndActions);

            if (index < _currentNode.ChoicePorts.Count && _currentNode.ChoicePorts[index].connected)
            {
                var edge = _currentNode.ChoicePorts[index].connections.FirstOrDefault();
                if (edge?.input?.node is DialogueLineNode nextNode)
                {
                    PlayNode(nextNode);
                    return;
                }
            }

            ShowMessage("Fin du dialogue (choix sans cible)");
        }

        #endregion

        #region Private Methods — Helpers

        private void ClearHighlight()
        {
            foreach (var node in _graphView.GetLineNodes())
                node.RemoveFromClassList("preview-active");
        }

        private void UpdateFlagsLabel()
        {
            var flags = _flagManager.ActiveFlags;
            _flagsLabel.text = flags.Count > 0
                ? $"Flags: {string.Join(", ", flags)}"
                : "Flags: (aucun)";
        }

        private void ShowMessage(string message)
        {
            _speakerLabel.text = "";
            _textLabel.text = message;
            _textLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _nextButton.SetEnabled(false);
        }

        private void Show() => _panelRoot.style.display = DisplayStyle.Flex;
        private void Hide() => _panelRoot.style.display = DisplayStyle.None;

        #endregion
    }
}
