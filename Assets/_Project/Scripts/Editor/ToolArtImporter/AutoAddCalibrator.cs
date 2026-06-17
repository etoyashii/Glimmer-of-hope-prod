using UnityEngine;
using UnityEditor;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// Automatically adds the MeshScaleCalibrator component to any
    /// GameObject having a MeshFilter as soon as it appears in a scene
    /// opened in the Editor (drag&drop from the Project, instantiation
    /// of a Prefab, etc.)
    ///
    /// WARNING: this behavior is intentionally broad (every object with a
    /// mesh, in all scenes) following an explicit request. This
    /// can modify a large number of GameObjects and scene files
    /// A menu allows disabling this behavior easily if needed
    /// (Tools > Blender Fix > Auto-Add Calibrator [ON/OFF])
    /// </summary>
    [InitializeOnLoad]
    public static class AutoAddCalibrator
    {
        #region Private Fields
        const string kPrefsKey = "GlimmerOfHope.BlenderFix.AutoAddCalibrator.Enabled";
        #endregion

        #region Public Properties
        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(kPrefsKey, true);
            set => EditorPrefs.SetBool(kPrefsKey, value);
        }
        #endregion


        #region Private Methods
        static void OnHierarchyChanged()
        {
            if (!IsEnabled) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // We only iterate over the MeshFilters currently loaded in the
            // scene. For large projects, this cost remains negligible because
            // hierarchyChanged is only raised when actual modifications occur
            // in the hierarchy (not every frame)
            MeshFilter[] meshFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

            foreach (var mf in meshFilters)
            {
                if (mf == null) continue;
                GameObject go = mf.gameObject;

                if (go.GetComponent<MeshScaleCalibrator>() == null)
                {
                    go.AddComponent<MeshScaleCalibrator>();
                }
            }
        }
        #endregion
        
        #region Helper
        static AutoAddCalibrator()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        [MenuItem("Tools/Blender Fix/Auto-Add Calibrator (Toggle)")]
        static void ToggleEnabled()
        {
            IsEnabled = !IsEnabled;
            Debug.Log($"[AutoAddCalibrator] {(IsEnabled ? "Activé" : "Désactivé")}.");
        }

        [MenuItem("Tools/Blender Fix/Auto-Add Calibrator (Toggle)", true)]
        static bool ToggleEnabledValidate()
        {
            Menu.SetChecked("Tools/Blender Fix/Auto-Add Calibrator (Toggle)", IsEnabled);
            return true;
        }
        #endregion
    }
}