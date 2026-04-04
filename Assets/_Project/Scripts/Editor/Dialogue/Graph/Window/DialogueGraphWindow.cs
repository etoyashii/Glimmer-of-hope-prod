using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class DialogueGraphWindow : EditorWindow
    {
        #region Private Fields

        private DialogueGraphView _graphView;
        private DialogueGraphToolbar _toolbar;
        private NodeInspectorPanel _inspectorPanel;
        private DialogueSearchWindow _searchWindow;
        private DialoguePreviewController _previewController;
        private ConversationSO _targetConversation;
        private Label _statusLabel;

        #endregion

        #region Menu Item

        [MenuItem("Glimmer/Dialogue/Graph Editor", priority = 50)]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.minSize = new Vector2(1000, 600);
            window.Show();
        }

        public static void OpenConversation(ConversationSO conversation)
        {
            var window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.minSize = new Vector2(1000, 600);
            window._targetConversation = conversation;
            window.Show();
            window.LoadConversation(conversation);
        }

        #endregion

        #region Double-Click Handler

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);

            if (obj is ConversationSO conversation)
            {
                OpenConversation(conversation);
                return true;
            }

            return false;
        }

        #endregion

        #region Unity Lifecycle

        private void CreateGUI()
        {
            BuildLayout();
            SetupSearchWindow();
            RegisterCallbacks();

            if (_targetConversation != null)
                LoadConversation(_targetConversation);
        }

        private void OnDestroy()
        {
            EditorApplication.update -= OnEditorUpdate;

            if (_searchWindow != null)
                DestroyImmediate(_searchWindow);
        }

        private void OnEditorUpdate()
        {
            _previewController?.Update();

            if (_previewController != null && _previewController.IsPlaying)
                Repaint();
        }

        #endregion

        #region Private Methods — Layout

        private void BuildLayout()
        {
            _graphView = new DialogueGraphView();
            _inspectorPanel = new NodeInspectorPanel(_graphView);

            _toolbar = new DialogueGraphToolbar(
                _graphView,
                OnConversationSelected,
                OnSaveClicked,
                OnPreviewClicked);

            EditorApplication.update += OnEditorUpdate;

            rootVisualElement.Add(_toolbar);

            var mainContainer = new VisualElement();
            mainContainer.style.flexDirection = FlexDirection.Row;
            mainContainer.style.flexGrow = 1;

            var graphContainer = new VisualElement();
            graphContainer.style.flexGrow = 1;
            _graphView.StretchToParentSize();
            graphContainer.Add(_graphView);
            mainContainer.Add(graphContainer);

            mainContainer.Add(_inspectorPanel);
            rootVisualElement.Add(mainContainer);

            _previewController = new DialoguePreviewController(_graphView);
            rootVisualElement.Add(_previewController);

            BuildStatusBar();
        }

        private void BuildStatusBar()
        {
            var statusBar = new VisualElement();
            statusBar.style.flexDirection = FlexDirection.Row;
            statusBar.style.height = 24;
            statusBar.style.paddingLeft = 8;
            statusBar.style.alignItems = Align.Center;
            statusBar.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

            _statusLabel = new Label("Aucune conversation chargee");
            _statusLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            statusBar.Add(_statusLabel);

            rootVisualElement.Add(statusBar);
        }

        private void SetupSearchWindow()
        {
            _searchWindow = CreateInstance<DialogueSearchWindow>();
            _searchWindow.Initialize(_graphView, this);

            _graphView.nodeCreationRequest = ctx =>
            {
                SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _searchWindow);
            };
        }

        private void RegisterCallbacks()
        {
            _graphView.graphViewChanged += change =>
            {
                UpdateStatusBar();
                return change;
            };

            RegisterSelectionCallback();
        }

        private void RegisterSelectionCallback()
        {
            _graphView.RegisterCallback<MouseUpEvent>(_ =>
            {
                var selection = _graphView.selection;

                if (selection.Count == 1 && selection[0] is DialogueLineNode lineNode)
                {
                    _inspectorPanel.InspectNode(lineNode);
                }
                else
                {
                    _inspectorPanel.ClearInspector();
                }
            });
        }

        #endregion

        #region Private Methods — Actions

        private void LoadConversation(ConversationSO conversation)
        {
            _targetConversation = conversation;
            _graphView.LoadConversation(conversation);
            _toolbar.UpdateConversationLabel(conversation);
            _inspectorPanel.ClearInspector();
            UpdateStatusBar();
        }

        private void OnConversationSelected(ConversationSO conversation)
        {
            if (conversation != null)
                LoadConversation(conversation);
        }

        private void OnPreviewClicked()
        {
            if (_targetConversation == null)
            {
                EditorUtility.DisplayDialog("Erreur", "Aucune conversation chargee.", "OK");
                return;
            }

            if (_previewController.IsPlaying)
                _previewController.StopPreview();
            else
                _previewController.StartPreview();
        }

        private void OnSaveClicked()
        {
            if (_targetConversation == null)
            {
                EditorUtility.DisplayDialog("Erreur", "Aucune conversation chargee.", "OK");
                return;
            }

            var success = _graphView.SaveConversation();
            _toolbar.UpdateConversationLabel(_targetConversation);
            UpdateStatusBar();

            _statusLabel.text += success ? " | Sauvegarde OK" : " | ERREUR sauvegarde";
        }

        private void UpdateStatusBar()
        {
            if (_targetConversation == null)
            {
                _statusLabel.text = "Aucune conversation chargee";
                return;
            }

            var lineCount = _graphView.GetLineNodes().Count;
            _statusLabel.text = $"{_targetConversation.ConversationId} | {lineCount} lignes | Langue: {_graphView.ActiveLanguage}";
        }

        #endregion
    }
}
