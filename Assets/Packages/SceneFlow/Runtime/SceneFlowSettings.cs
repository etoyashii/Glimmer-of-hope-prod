using System;
using System.IO;
using UnityEngine;

namespace Kebab.SceneFlow.Settings
{
    /// <summary>
    /// The settings for the SceneFlow system.
    /// </summary>
    public class SceneFlowSettings : ScriptableObject
    {
        [Flags]
        public enum EGamepadAction
        {
            A = 1 << 0,
            B = 1 << 1,
            X = 1 << 2,
            Y = 1 << 3,
            Start = 1 << 4,
        }


        [Flags]
        public enum EKeyboardAction
        {
            Space = 1 << 0,
            Enter = 1 << 1,
            Escape = 1 << 2,
        }
        
        [Flags]
        public enum EMouseAction
        {
            LeftClick = 1 << 0,
            RightClick = 1 << 1,
        }

        public const string SETTINGS_DIRECTORY = "SceneFlow";
        public const string SETTINGS_FILENAME = "SceneFlowSettings";


        /// <summary>
        /// The prefab to use for the loading screen.
        /// </summary>
        public ALoadingScreen LoadingScreenPrefab;

        /// <summary>
        /// The fake loading time in seconds.
        /// </summary>
        [Min(0)] public float FakeLoadingTime = 0f;

        /// <summary>
        /// The percentage where the fake loading starts.
        /// </summary>
        [Range(0f, 1f)] public float FakeLoadingPercent = 0.2f;

        /// <summary>
        /// If false, the new scene will be displayed immediately after the loading (fake or not) is finished.
        /// </summary>
        public bool ActionToExitLoadingScreen = false;

        public EKeyboardAction ExitLoadingKeyboardAction = (EKeyboardAction)(-1);

        public EGamepadAction ExitLoadingGamepadAction = (EGamepadAction)(-1);

        public EMouseAction ExitLoadingMouseAction = (EMouseAction)(-1);

#if UNITY_EDITOR
        /// <summary>
        /// /// Get the settings asset or create it if it doesn't exist.
        /// </summary>
        /// <returns>The settings asset.</returns>
        public static SceneFlowSettings GetOrCreate()
        {
            SceneFlowSettings settings = Resources.Load<SceneFlowSettings>($"{SETTINGS_DIRECTORY}/{SETTINGS_FILENAME}");

            if (settings == null)
                settings = CreateAsset();

            return settings;
        }

        public static SceneFlowSettings CreateAsset()
        {
            SceneFlowSettings settings;
            string directoryParentName = "Assets/Resources";
            string directoryPath = Path.Join(directoryParentName, SETTINGS_DIRECTORY).Replace('\\', '/');
            string filePath = Path.Join(directoryPath, SETTINGS_FILENAME + ".asset").Replace('\\', '/');
            settings = CreateInstance<SceneFlowSettings>();
            if (!UnityEditor.AssetDatabase.IsValidFolder(directoryParentName))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            if (!UnityEditor.AssetDatabase.IsValidFolder(directoryPath))
                UnityEditor.AssetDatabase.CreateFolder(directoryParentName, SETTINGS_DIRECTORY);

            UnityEditor.AssetDatabase.CreateAsset(settings, filePath);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"Created ui settings asset at path: {filePath}");
            return settings;
        }
#endif
    }
}



