using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class DialogueSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        #region Private Fields

        private DialogueGraphView _graphView;
        private EditorWindow _editorWindow;

        #endregion

        #region Public Methods

        public void Initialize(DialogueGraphView graphView, EditorWindow window)
        {
            _graphView = graphView;
            _editorWindow = window;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Ajouter Noeud"), 0),
                CreateEntry("Nouvelle Ligne", 1, "line"),
                CreateEntry("Noeud Fin", 1, "end"),
            };

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            var worldPos = _editorWindow.position.position;
            var localPos = context.screenMousePosition - worldPos;
            var graphPos = _graphView.viewTransform.matrix.inverse.MultiplyPoint(localPos);

            switch (entry.userData as string)
            {
                case "line":
                    _graphView.AddNewLineNode(graphPos);
                    return true;

                case "end":
                    _graphView.AddEndNode(graphPos);
                    return true;

                default:
                    return false;
            }
        }

        #endregion

        #region Private Methods

        private SearchTreeEntry CreateEntry(string label, int level, string userData)
        {
            return new SearchTreeEntry(new GUIContent(label))
            {
                level = level,
                userData = userData
            };
        }

        #endregion
    }
}
