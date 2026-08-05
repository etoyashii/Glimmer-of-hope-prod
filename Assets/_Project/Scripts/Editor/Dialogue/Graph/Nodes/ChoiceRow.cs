using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class ChoiceRow
    {
        public int Index;
        public VisualElement Root;
        public TextField Field;
        public Port Port;

        public void SetIndex(int index)
        {
            Index = index;
            Port.userData = index;
        }
    }
}
