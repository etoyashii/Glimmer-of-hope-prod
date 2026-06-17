using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Editor.PlayModeSaver
{
    [System.Serializable]
    public class SerializedGameObject
    {
        [TextArea]
        #region Public Properties

        public string serializedData;
        public List<InstanceReference> savedInstanceIDs = new List<InstanceReference>();

        public string scenePath;

        public bool hasParent;
        public int parentID;
        public int siblingIndex;

        public int childCount;
        public int indexOfFirstChild;

        public List<SerializedComponent> serializedComponents = new List<SerializedComponent>();
        #endregion
    }
}