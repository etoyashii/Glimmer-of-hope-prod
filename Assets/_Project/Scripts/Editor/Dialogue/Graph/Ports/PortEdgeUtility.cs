using UnityEditor.Experimental.GraphView;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public static class PortEdgeUtility
    {
        public static void Disconnect(Edge edge)
        {
            if (edge == null)
                return;

            edge.output?.Disconnect(edge);
            edge.input?.Disconnect(edge);
            edge.RemoveFromHierarchy();
        }
    }
}
