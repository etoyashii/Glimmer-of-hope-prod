using UnityEngine;
using static GlimmerOfHope.Editor.PlayModeSaver.PlayModeSaver;

namespace GlimmerOfHope.Editor.PlayModeSaver
{
    class DeserializedGameObject
    {
        #region Public Properties

        public SerializedGameObject serializedGameObject;
        public GameObject gameObject;
        #endregion

        #region Public Methods
        public DeserializedGameObject(SerializedGameObject serializedGameObject, GameObject gameObject)
        {
            this.serializedGameObject = serializedGameObject;
            this.gameObject = gameObject;
        }
        #endregion
    }
}
