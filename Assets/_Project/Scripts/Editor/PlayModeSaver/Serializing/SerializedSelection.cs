using System.Collections.Generic;
using static GlimmerOfHope.Editor.PlayModeSaver.PlayModeSaver;

namespace GlimmerOfHope.Editor.PlayModeSaver
{
    [System.Serializable]
    public class SerializedSelection
    {
        #region Public Properties
        public List<int> indexOfRootGOs = new List<int>();
        public List<int> idOfRootGOs = new List<int>();
        public List<SerializedGameObject> serializedGameObjects = new List<SerializedGameObject>();
        public bool foundStatic;
        #endregion
    }
}

