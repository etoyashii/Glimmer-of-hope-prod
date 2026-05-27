using System.Collections.Generic;
using UnityEngine;
using System;

using static GlimmerOfHope.Editor.PlayModeSaver.PlayModeSaver;

namespace GlimmerOfHope.Editor.PlayModeSaver
{
    [System.Serializable]
    public class SerializedComponent
    {
        #region Public Properties
        public string assemblyName;
        public string typeName;
        [TextArea]
        public string serializedData;
        public List<InstanceReference> savedInstanceIDs = new List<InstanceReference>();
        #endregion

        #region Public Methods

        public SerializedComponent(Type type, string serializedData)
        {
            this.assemblyName = type.Assembly.GetName().Name;
            this.typeName = type.FullName;
            this.serializedData = serializedData;
        }
        #endregion
    }
}