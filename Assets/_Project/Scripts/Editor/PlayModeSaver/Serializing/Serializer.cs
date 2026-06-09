using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using static GlimmerOfHope.Editor.PlayModeSaver.PlayModeSaver;

namespace GlimmerOfHope.Editor.PlayModeSaver
{
    /// <summary>
    /// Serializes a selection of GameObjects and their components into a format that can be restored later.
    /// </summary>
    class Serializer
    {
        #region Private Fields

        IList<GameObject> rawGameObjects;
        SerializedSelection serializedSelection;
        List<GameObject> rootGameObjectsToCopy;
        List<GameObject> allGameObjectsToCopy;
        List<UnityEngine.Object> allComponentsInGameObjectsToCopyHierarchy;
        #endregion

        #region Public Methods

        public Serializer(IList<GameObject> rawGameObjects)
        {
            this.rawGameObjects = rawGameObjects;
        }

        /// <summary>
        /// Serializes the provided GameObjects and their hierarchy into a SerializedSelection.
        /// </summary>
        public SerializedSelection Serialize()
        {
            // Identify root GameObjects (those not parented by another selected GameObject)
            rootGameObjectsToCopy = GetRootGameObjects(rawGameObjects);

            allGameObjectsToCopy = new List<GameObject>();
            rootGameObjectsToCopy.ForEach(x =>
            {
                List<GameObject> tree = new List<GameObject>();
                GetTree(x, ref tree);
                allGameObjectsToCopy.AddRange(tree);
            });

            // Collect all components in the hierarchy for reference resolution
            allComponentsInGameObjectsToCopyHierarchy = GetAllObjects(rootGameObjectsToCopy);

            serializedSelection = new SerializedSelection();
            Serialize(rootGameObjectsToCopy);
            return serializedSelection;
        }
        #endregion

        #region Private Methods

        // Gets all selected gameobjects that aren't parented by another in the selected list
        List<GameObject> GetRootGameObjects(IList<GameObject> gameObjects)
        {
            List<GameObject> rootGameObjects = new List<GameObject>();
            if (gameObjects.Count == 1)
            {
                rootGameObjects.Add(gameObjects.First());
            }
            else
            {
                foreach (GameObject gameObject in gameObjects)
                {
                    // A GameObject is a root if it is not a child of any other selected GameObject
                    if (gameObjects.Any(x => x != gameObject && !gameObject.transform.IsChildOf(x.transform)))
                    {
                        rootGameObjects.Add(gameObject);
                    }
                }
            }
            return rootGameObjects;
        }

        List<UnityEngine.Object> GetAllObjects(List<GameObject> gameObjects)
        {
            List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
            allGameObjectsToCopy.ForEach(x =>
            {
                objects.Add(x.gameObject);
                List<Component> components = x.GetComponents<Component>().ToList();
                components.ForEach(y =>
                {
                    objects.Add(y);
                });
            });
            return objects;
        }

        void Serialize(List<GameObject> gameObjectsToSerialize)
        {
            foreach (GameObject gameObject in gameObjectsToSerialize)
            {
                serializedSelection.indexOfRootGOs.Add(serializedSelection.serializedGameObjects.Count);
                serializedSelection.idOfRootGOs.Add(gameObject.GetInstanceID());
                SerializeGameObject(gameObject);
            }
        }

        void SerializeGameObject(GameObject gameObject)
        {
            SerializedGameObject sgo = new SerializedGameObject();
            sgo.serializedData = EditorJsonUtility.ToJson(gameObject, false);
            sgo.savedInstanceIDs = GetInstanceReferenceIDs(gameObject);

            sgo.scenePath = gameObject.scene.path;
            sgo.hasParent = gameObject.transform.parent != null;
            sgo.parentID = sgo.hasParent ? gameObject.transform.parent.GetInstanceID() : 0;
            sgo.siblingIndex = gameObject.transform.GetSiblingIndex();

            sgo.childCount = gameObject.transform.childCount;
            sgo.indexOfFirstChild = serializedSelection.serializedGameObjects.Count + 1;

            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component == null) continue;
                var serializedComponent = SerializeComponent(component);
                sgo.serializedComponents.Add(serializedComponent);
            }

            serializedSelection.serializedGameObjects.Add(sgo);

            if (gameObject.isStatic)
            {
                serializedSelection.foundStatic = true;
                Debug.LogWarning("PlayModeSaver tried to serialize static GameObject " + gameObject + ". This is not allowed.");
            }

            foreach (Transform child in gameObject.transform)
                SerializeGameObject(child.gameObject);
        }

        SerializedComponent SerializeComponent(Component component)
        {
            SerializedComponent serializedComponent = new SerializedComponent(component.GetType(), EditorJsonUtility.ToJson(component, false));
            serializedComponent.savedInstanceIDs = GetInstanceReferenceIDs(component);
            return serializedComponent;
        }

        List<InstanceReference> GetInstanceReferenceIDs(UnityEngine.Object obj)
        {
            List<InstanceReference> ids = new List<InstanceReference>();
            SerializedObject so = new SerializedObject(obj);
            var prop = so.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (prop.objectReferenceValue == null)
                    {
                        ids.Add(new InstanceReference());
                    }
                    else if (allComponentsInGameObjectsToCopyHierarchy.Contains(prop.objectReferenceValue))
                    {
                        // If the reference is within the hierarchy, store its index for internal resolution
                        int index = allComponentsInGameObjectsToCopyHierarchy.IndexOf(prop.objectReferenceValue);
                        ids.Add(new InstanceReference(index, true));
                    }
                    else
                    {
                        // If the reference is external, store its global instance ID
                        ids.Add(new InstanceReference(prop.objectReferenceInstanceIDValue, false));
                    }
                }
            }
            return ids;
        }

        static void GetTree(GameObject go, ref List<GameObject> gameObjects)
        {
            gameObjects.Add(go);
            foreach (Transform child in go.transform)
                GetTree(child.gameObject, ref gameObjects);
        }
        #endregion
    }
}