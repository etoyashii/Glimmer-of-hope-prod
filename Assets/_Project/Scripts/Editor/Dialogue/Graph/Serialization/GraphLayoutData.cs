using System;
using UnityEngine;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    [CreateAssetMenu(fileName = "New Layout", menuName = "Glimmer/Dialogue/Graph Layout")]
    public class GraphLayoutData : ScriptableObject
    {
        #region Serialized Fields

        [SerializeField] private string _conversationId;
        [SerializeField] private Vector3 _viewPosition;
        [SerializeField] private Vector3 _viewScale = Vector3.one;
        [SerializeField] private NodeLayoutEntry[] _nodeLayouts;

        #endregion

        #region Properties

        public string ConversationId => _conversationId;
        public Vector3 ViewPosition => _viewPosition;
        public Vector3 ViewScale => _viewScale;
        public NodeLayoutEntry[] NodeLayouts => _nodeLayouts;

        #endregion

        #region Public Methods

        public void Save(string conversationId, Vector3 viewPos, Vector3 viewScl, NodeLayoutEntry[] layouts)
        {
            _conversationId = conversationId;
            _viewPosition = viewPos;
            _viewScale = viewScl;
            _nodeLayouts = layouts;
        }

        public bool TryGetNodePosition(string lineId, out Vector2 position)
        {
            if (_nodeLayouts != null)
            {
                foreach (var entry in _nodeLayouts)
                {
                    if (entry.nodeId == lineId)
                    {
                        position = entry.position;
                        return true;
                    }
                }
            }

            position = Vector2.zero;
            return false;
        }

        #endregion
    }

    [Serializable]
    public class NodeLayoutEntry
    {
        public string nodeId;
        public Vector2 position;
        public bool isCollapsed;
    }
}
