using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
namespace GlimmerOfHope.Editor.NewDialogue
{
    public class DialogueGraphWindow : EditorWindow
    {
        #region Private Fields
        private DialogueGraph _graph;
        #endregion

        #region Public Methods
        [MenuItem("Window/Dialogue System/Graph Editor")]
        public static void Open()
        {
            var window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
        }

        // Called by Unity when a DialogueGraph asset is double-clicked in the Project window
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceId) as DialogueGraph;
            if (asset == null) return false;

            var window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window._graph = asset;
            window.BuildView();
            return true;
        }
        #endregion

        #region private Methods
        private void CreateGUI()
        {
            BuildView();
        }

        private void BuildView()
        {
            rootVisualElement.Clear();

            var graphView = new DialogueGraphView(_graph)
            {
                name = "Dialogue Graph View"
            };
            graphView.StretchToParentSize();
            graphView.nodeCreationRequest = null; // disables the default shortcut (spacebar)

            // Right-click context menu: one entry per creatable node type
            graphView.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                var localMousePosition = graphView.ChangeCoordinatesTo(graphView.contentViewContainer, evt.localMousePosition);
                evt.menu.AppendAction("Create Dialogue Node (simple)", _ => graphView.CreateNewNode(localMousePosition, DialogueNodeType.Dialogue, hasChoices: false));
                evt.menu.AppendAction("Create Dialogue Node (with choices)", _ => graphView.CreateNewNode(localMousePosition, DialogueNodeType.Dialogue, hasChoices: true));
                evt.menu.AppendAction("Create Gate Node", _ => graphView.CreateNewNode(localMousePosition, DialogueNodeType.Gate));
                evt.menu.AppendAction("Create Condition Node", _ => graphView.CreateNewNode(localMousePosition, DialogueNodeType.Condition));
                evt.menu.AppendAction("Create Action Node", _ => graphView.CreateNewNode(localMousePosition, DialogueNodeType.Action));
                evt.menu.AppendAction("Create End Node", _ => graphView.CreateNewNode(localMousePosition, DialogueNodeType.End));
            });

            rootVisualElement.Add(graphView);
        }
        #endregion
    }
}