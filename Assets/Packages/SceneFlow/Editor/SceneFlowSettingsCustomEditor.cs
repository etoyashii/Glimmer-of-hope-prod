using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Kebab.SceneFlow.Settings.Editors
{
    [CustomEditor(typeof(SceneFlowSettings))]
    public class SceneFlowSettingsCustomEditor : Editor
    {
        private const string TOOLTIP_FAKE_LOADING_TIME = "The fake loading time in seconds.";
        private const int LABEL_WIDTH = 250;
        private SerializedProperty loadingScreenPrefab;
        private SerializedProperty fakeLoadingTime;
        private SerializedProperty fakeLoadingPercent;
        private SerializedProperty actionToExitLoadingScreen;
        private SerializedProperty exitLoadingKeyboardMouseAction;
        private SerializedProperty exitLoadingGamepadAction;
        private SerializedProperty exitLoadingMouseAction;

        private GUIContent fakeLoadingTimeContent;
        private GUIContent fakeLoadingPercentContent;
        private GUIContent loadingScreenPrefabContent;
        private GUIContent exitLoadingKeyboardMouseActionContent;
        private GUIContent exitLoadingGamepadActionContent;
        private GUIContent exitLoadingMouseActionContent;
        private GUIContent actionToExitLoadingScreenContent;
        private GUIStyle labelStyle;



        private void OnEnable()
        {
            fakeLoadingTimeContent = new GUIContent("Fake Loading Time", TOOLTIP_FAKE_LOADING_TIME);
            exitLoadingKeyboardMouseActionContent = new GUIContent("Exit Loading Keyboard Mouse Action", "The action to exit the loading screen using the keyboard or mouse.");
            exitLoadingGamepadActionContent = new GUIContent("Exit Loading Gamepad Action", "The action to exit the loading screen using the gamepad.");
            exitLoadingMouseActionContent = new GUIContent("Exit Loading Mouse Action", "The action to exit the loading screen using the mouse.");
            fakeLoadingPercentContent = new GUIContent("Fake Loading Percent", "The percentage where the fake loading starts.");
            loadingScreenPrefabContent = new GUIContent("Loading Screen Prefab", "The prefab to use for the loading screen.");
            actionToExitLoadingScreenContent = new GUIContent("Action to Exit Loading Screen", "If false, the new scene will be displayed immediately after the loading (fake or not) is finished.");

            try
            {
                loadingScreenPrefab = serializedObject.FindProperty(nameof(SceneFlowSettings.LoadingScreenPrefab));
                fakeLoadingTime = serializedObject.FindProperty(nameof(SceneFlowSettings.FakeLoadingTime));
                fakeLoadingPercent = serializedObject.FindProperty(nameof(SceneFlowSettings.FakeLoadingPercent));

                exitLoadingMouseAction = serializedObject.FindProperty(nameof(SceneFlowSettings.ExitLoadingMouseAction));

                actionToExitLoadingScreen = serializedObject.FindProperty(nameof(SceneFlowSettings.ActionToExitLoadingScreen));
                exitLoadingKeyboardMouseAction = serializedObject.FindProperty(nameof(SceneFlowSettings.ExitLoadingKeyboardAction));
                exitLoadingGamepadAction = serializedObject.FindProperty(nameof(SceneFlowSettings.ExitLoadingGamepadAction));
            }
            catch (Exception e)
            {
                // Really weird... it throw an error (SerializedObjectNotCreatableException) when the asset is created.
            }
        }

        private void LargePropertyField(GUIContent label, SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(LABEL_WIDTH)); // Adjust width as needed
            EditorGUILayout.PropertyField(property, GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {

            DrawFakeLoadingTime();
            DrawExitLoadingScreen();

            Header("References");
            LargePropertyField(loadingScreenPrefabContent, loadingScreenPrefab);
            if (loadingScreenPrefab.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No loading screen prefab selected.", MessageType.Error);
                EditorGUILayout.Space(10);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFakeLoadingTime()
        {
            Header("Fake loading");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fakeLoadingTimeContent, GUILayout.Width(LABEL_WIDTH));
            float newFakeLoadingTime = EditorGUILayout.FloatField(fakeLoadingTime.floatValue);
            if (newFakeLoadingTime < 0f)
                newFakeLoadingTime = 0f;
            EditorGUILayout.EndHorizontal();

            fakeLoadingTime.floatValue = newFakeLoadingTime;
            if (fakeLoadingTime.floatValue > 0f)
                LargePropertyField(fakeLoadingPercentContent, fakeLoadingPercent);
        }

        private void DrawExitLoadingScreen()
        {
            Header("Exit loading screen");
            LargePropertyField(actionToExitLoadingScreenContent, actionToExitLoadingScreen);
            if (actionToExitLoadingScreen.boolValue)

            {
                SceneFlowSettings.EKeyboardAction exitLoadingKeyboardMouseActionValue = (SceneFlowSettings.EKeyboardAction)exitLoadingKeyboardMouseAction.enumValueFlag;
                SceneFlowSettings.EGamepadAction exitLoadingGamepadActionValue = (SceneFlowSettings.EGamepadAction)exitLoadingGamepadAction.enumValueFlag;
                SceneFlowSettings.EMouseAction exitLoadingMouseActionValue = (SceneFlowSettings.EMouseAction)exitLoadingMouseAction.enumValueFlag;

                DrawLargeEnumFlagsField(exitLoadingKeyboardMouseActionContent, exitLoadingKeyboardMouseAction, exitLoadingKeyboardMouseActionValue);
                DrawLargeEnumFlagsField(exitLoadingGamepadActionContent, exitLoadingGamepadAction, exitLoadingGamepadActionValue);
                DrawLargeEnumFlagsField(exitLoadingMouseActionContent, exitLoadingMouseAction, exitLoadingMouseActionValue);
            }
        }

        private void DrawLargeEnumFlagsField<T>(GUIContent label, SerializedProperty property, T value) where T : Enum
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(LABEL_WIDTH));
            T newValue = (T)EditorGUILayout.EnumFlagsField(value);
            property.enumValueFlag = Convert.ToInt32(newValue);
            EditorGUILayout.EndHorizontal();
        }


        private void Header(string title)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

    }
}

