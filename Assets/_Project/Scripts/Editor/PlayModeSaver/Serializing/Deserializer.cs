using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

using static GlimmerOfHope.Editor.PlayModeSaver.PlayModeSaver;

namespace GlimmerOfHope.Editor.PlayModeSaver
{

    class Deserializer
    {
        #region Private Fields

        SerializedSelection serializedSelection;
        bool destroyOriginals;

        List<UnityEngine.Object> deserializedObjects = new List<UnityEngine.Object>();
        List<DeserializedGameObject> deserializedGameObjects = new List<DeserializedGameObject>();
        List<DeserializedComponent> deserializedComponents = new List<DeserializedComponent>();
        Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();
        #endregion

        #region Public Methods
        public Deserializer(SerializedSelection serializedSelection, bool destroyOriginals)
        {
            this.serializedSelection = serializedSelection;
            this.destroyOriginals = destroyOriginals;
        }

       

        public GameObject[] Deserialize()
        {
            Reset();

            int undoIndex = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Restore Play Mode Changes");

            // Do this first, since otherwise it can interfere with restoring the sibling indices.
            if (destroyOriginals)
                DestroyOriginals();

            GameObject go = null;
            foreach (int index in serializedSelection.indexOfRootGOs)
            {
                var serializedGameObject = serializedSelection.serializedGameObjects[index];
                Scene scene = EditorSceneManager.GetSceneByPath(serializedGameObject.scenePath);
                if (!scene.isLoaded) continue;

                ReadNodeFromSerializedNodes(index, out go);
            }
            RestoreInternalObjectReferences();

            var deserializedRootGameObjects = deserializedGameObjects.Where(x => serializedSelection.indexOfRootGOs.Contains(x.serializedGameObject.indexOfFirstChild - 1)).Select(x => x.gameObject).ToArray();

            // Enforces child index when redoing
            foreach (var g in deserializedRootGameObjects)
                Undo.SetTransformParent(g.transform, g.transform.parent, "Creat");

            Undo.CollapseUndoOperations(undoIndex);
            return deserializedRootGameObjects;
        }
        #endregion

        #region Private Methods
        void Reset()
        {
            deserializedObjects = new List<UnityEngine.Object>();
            deserializedGameObjects = new List<DeserializedGameObject>();
            deserializedComponents = new List<DeserializedComponent>();
            loadedAssemblies = new Dictionary<string, Assembly>();
        }
        void DestroyOriginals()
        {
            foreach (var id in serializedSelection.idOfRootGOs)
            {
                GameObject originalRootGO = EditorUtility.EntityIdToObject(id) as GameObject;
                if (originalRootGO == null) continue;
                Undo.DestroyObjectImmediate(originalRootGO);
            }
        }

        int ReadNodeFromSerializedNodes(int index, out GameObject go)
        {
            var serializedGameObject = serializedSelection.serializedGameObjects[index];
            var newGameObject = RestoreGameObject(serializedGameObject);

            Scene scene = EditorSceneManager.GetSceneByPath(serializedGameObject.scenePath);
            if (!scene.isDirty) EditorSceneManager.MarkSceneDirty(scene);
            Undo.MoveGameObjectToScene(newGameObject, scene, "Move GameObject to scene");
            // The tree needs to be read in depth-first, since that's how we wrote it out.
            for (int i = 0; i != serializedGameObject.childCount; i++)
            {
                GameObject childGO;
                index = ReadNodeFromSerializedNodes(++index, out childGO);
                childGO.transform.SetParent(newGameObject.transform, false);
            }
            go = newGameObject;
            return index;
        }

        GameObject RestoreGameObject(SerializedGameObject serializedGameObject)
        {
            GameObject gameObject = new GameObject();
            Undo.RegisterCreatedObjectUndo(gameObject, "Create");

            deserializedObjects.Add(gameObject);
            deserializedGameObjects.Add(new DeserializedGameObject(serializedGameObject, gameObject));
            EditorJsonUtility.FromJsonOverwrite(serializedGameObject.serializedData, gameObject);
            RestoreObjectReference(serializedGameObject.savedInstanceIDs, gameObject);

            RestoreComponents(gameObject, serializedGameObject.serializedComponents);
            return gameObject;
        }

        void RestoreComponents(GameObject go, List<SerializedComponent> serializedComponents)
        {
            foreach (var serializedComponent in serializedComponents)
            {
                RestoreComponent(go, serializedComponent);
            }
        }

        void RestoreComponent(GameObject go, SerializedComponent serializedComponent)
        {
            Component component = null;

            if (!loadedAssemblies.ContainsKey(serializedComponent.assemblyName))
                loadedAssemblies.Add(serializedComponent.assemblyName, Assembly.Load(serializedComponent.assemblyName));
            Type type = loadedAssemblies[serializedComponent.assemblyName].GetType(serializedComponent.typeName);
            Debug.Assert(type != null, "Type '" + serializedComponent.typeName + "' not found in assembly '" + serializedComponent.assemblyName + "'");

            if (type == typeof(Transform)) component = go.transform;
            else component = Undo.AddComponent(go, type);


            EditorJsonUtility.FromJsonOverwrite(serializedComponent.serializedData, component);
            RestoreObjectReference(serializedComponent.savedInstanceIDs, component);
            deserializedObjects.Add(component);
            deserializedComponents.Add(new DeserializedComponent(serializedComponent, component));
        }

        void RestoreObjectReference(List<InstanceReference> savedInstanceIDs, UnityEngine.Object obj)
        {
            SerializedObject so = new SerializedObject(obj);
            var prop = so.GetIterator();
            int i = 0;
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (!savedInstanceIDs[i].isNull && !savedInstanceIDs[i].isInternal)
                    {
                        var refObj = EditorUtility.EntityIdToObject(savedInstanceIDs[i].id);
                        if (refObj == null) Debug.LogWarning("Object reference with saved id " + savedInstanceIDs[i] + " on " + obj + " could not be found. This is likely a bug.");
                        prop.objectReferenceValue = refObj;
                    }
                    i++;
                }
            }
            so.ApplyModifiedProperties();
        }

        // Some things can't be restored until all the gameobjects and components have been created. Do them now.
        void RestoreInternalObjectReferences()
        {
            foreach (var deserializedGameObject in deserializedGameObjects)
            {
                // The root gameobjects need their parents restored
                if (deserializedGameObject.gameObject.transform.parent == null && deserializedGameObject.serializedGameObject.hasParent)
                {
                    UnityEngine.Object o = EditorUtility.EntityIdToObject(deserializedGameObject.serializedGameObject.parentID);
                    if (o == null || (Transform)o == null) return;
                    // Note that this ought to use Undo.SetTransformParent, but you can't currently set worldPositionStays using it.
                    deserializedGameObject.gameObject.transform.SetParent((Transform)o, false);
                }
                deserializedGameObject.gameObject.transform.SetSiblingIndex(deserializedGameObject.serializedGameObject.siblingIndex);
            }


            foreach (var deserializedComponent in deserializedComponents)
            {
                SerializedObject so = new SerializedObject(deserializedComponent.component);
                var prop = so.GetIterator();
                int i = 0;
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (!deserializedComponent.serializedComponent.savedInstanceIDs[i].isNull && deserializedComponent.serializedComponent.savedInstanceIDs[i].isInternal)
                        {
                            prop.objectReferenceValue = deserializedObjects[deserializedComponent.serializedComponent.savedInstanceIDs[i].id];
                        }
                        i++;
                    }
                }
                so.ApplyModifiedProperties();
            }
        }
    }
    #endregion
}