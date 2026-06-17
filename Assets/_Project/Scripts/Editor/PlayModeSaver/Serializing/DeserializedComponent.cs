using static GlimmerOfHope.Editor.PlayModeSaver.PlayModeSaver;
using UnityEngine;
namespace GlimmerOfHope.Editor.PlayModeSaver
{
    class DeserializedComponent
    {
        #region Public Properties
        public SerializedComponent serializedComponent;
        public Component component;
        #endregion

        #region Public Methods
        public DeserializedComponent(SerializedComponent serializedComponent, Component component)
        {
            this.serializedComponent = serializedComponent;
            this.component = component;
        }
        #endregion
    }
}