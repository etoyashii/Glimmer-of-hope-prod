using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace GlimmerOfHope.Editor.PlayModeSaver
{

    /// <summary>
    /// Play mode saver.
    /// Allows saving and restoring of gameobject hierarchies.
    /// </summary>
    public static class PlayModeSaver {


        #region Public Methods

        /// <summary>
        /// Serialize the specified gameObjects and all their children.
        /// </summary>
        /// <param name="gameObjects">Game objects.</param>
        public static SerializedSelection Serialize (IList<GameObject> gameObjects) {
			Serializer serializer = new Serializer(gameObjects);
			return serializer.Serialize();
		}

		// Checks if this data can be deserialized
		public static bool CanDeserialize (SerializedSelection serializedSelection) {
			if(serializedSelection.foundStatic) return false;
			foreach(int index in serializedSelection.indexOfRootGOs) {
				var serializedGameObject = serializedSelection.serializedGameObjects [index];
				Scene scene = EditorSceneManager.GetSceneByPath(serializedGameObject.scenePath);
				if(scene.isLoaded) return true;
			}
			return false;
		}
			
		/// <summary>
		/// Deserialize the specified serializedSelection and optionally destroy the originals.
		/// </summary>
		/// <param name="serializedSelection">Serialized selection.</param>
		/// <param name="destroyOriginals">If set to <c>true</c> destroy originals.</param>
		/// <returns>Returns the root level restored GameObjects</returns>
		public static GameObject[] Deserialize (SerializedSelection serializedSelection, bool destroyOriginals) {
			Deserializer deserializer = new Deserializer(serializedSelection, destroyOriginals);
			var clonedGameObjects = deserializer.Deserialize();
			return clonedGameObjects;
		}
        #endregion

    }
}
