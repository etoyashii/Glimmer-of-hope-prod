using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kebab.SceneFlow.LoadingScreen.Editors
{
    [CustomEditor(typeof(ALoadingScreen), true)]
    public class LoadingScreenCustomEditor : Editor
    {
        private const string PREFAB_SAVE_PATH = "Assets/Prefabs/SceneFlow/LoadingScreens";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (IsInPrefabEditor()) return;

            EditorGUILayout.Space(10);
            if (IsPrefab())
            {
                UnityEngine.Object target = null;

                if (IsPrefabInstance())
                    target = PrefabUtility.GetCorrespondingObjectFromSource(serializedObject.targetObject);
                else
                    target = serializedObject.targetObject;


                GUI.enabled = !IsSetAsLoadingScreen(target as ALoadingScreen);
                if (GUILayout.Button("Set as Loading Screen"))
                    SetAsLoadingScreen(target as ALoadingScreen);
                GUI.enabled = true;
            }
            else if (IsPrefabInstance())
            {
                if (GUILayout.Button("Open Prefab"))
                    OpenPrefab();
            }
            else
            {
                if (GUILayout.Button("Save as Prefab"))
                    SaveAsPrefab();
            }
        }

        private bool IsInPrefabEditor()
        {
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            return prefabStage != null;
        }


        private void OpenPrefab()
        {
            var target = serializedObject.targetObject;
            GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(target) as GameObject;

            if (prefabAsset != null)
                AssetDatabase.OpenAsset(prefabAsset);
        }


        private void SaveAsPrefab()
        {
            var targetComponent = serializedObject.targetObject as MonoBehaviour;
            if (targetComponent == null) return;

            var gameObject = targetComponent.gameObject;
            string name = gameObject.name;

            if (!System.IO.Directory.Exists(PREFAB_SAVE_PATH))
            {
                System.IO.Directory.CreateDirectory(PREFAB_SAVE_PATH);
            }

            string prefabPath = $"{PREFAB_SAVE_PATH}/{name}.prefab";
            try
            {
                GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, prefabPath, InteractionMode.AutomatedAction);
                if (prefab != null)
                {
                    Debug.Log($"Prefab saved successfully at: {prefabPath}");
                    EditorGUIUtility.PingObject(prefab); // Highlight the prefab in Project window
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save prefab: {e.Message}");
            }
        }

        private bool IsSetAsLoadingScreen(ALoadingScreen target)
        {
            return SceneFlowManager.Settings.LoadingScreenPrefab == target;
        }

        private bool IsPrefabAsset()
        {
            var target = serializedObject.targetObject;
            return PrefabUtility.IsPartOfPrefabAsset(target);
        }

        private bool IsPrefab()
        {
            var target = serializedObject.targetObject;
            return PrefabUtility.IsPartOfAnyPrefab(target);
        }


        private bool IsPrefabInstance()
        {
            var target = serializedObject.targetObject;
            return PrefabUtility.IsPartOfPrefabInstance(target);
        }

        private void SetAsLoadingScreen(ALoadingScreen target)
        {
            SceneFlowManager.Settings.LoadingScreenPrefab = target;
        }
    }
}

