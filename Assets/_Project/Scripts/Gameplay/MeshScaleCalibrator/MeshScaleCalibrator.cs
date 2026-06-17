using UnityEngine;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// Component added automatically to any GameObject having a
    /// MeshFilter as soon as it is placed in a scene.
    ///
    /// Workflow:
    /// 1) The asset is placed in a test scene, alongside other models
    ///    already at the correct scale.
    /// 2) Manually adjust the Transform.localScale until the size is
    ///    visually consistent with the rest.
    /// 3) Click on "Save calibration" in the Inspector
    ///    (see MeshScaleCalibratorEditor): the found scale is directly multiplied
    ///    into the mesh vertices, and a calibrated Prefab is
    ///    created/overwritten in the project. The Transform of the instance in the scene
    ///    returns to (1,1,1).
    /// 4) From now on, whenever this calibrated Prefab is used in a
    ///    scene, its Transform naturally displays Scale = 1/1/1.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MeshScaleCalibrator : MonoBehaviour
    {
        #region Public Properties
        // Path of the calibrated Prefab already generated for this asset, if it exists.
        // Empty if this asset has never been calibrated.
        public string CalibratedPrefabPath => _calibratedPrefabPath;

        // True if a calibration has already been saved for this object.
        public bool IsCalibrated => !string.IsNullOrEmpty(_calibratedPrefabPath);
        #endregion

        #region Serialized Fields
        [SerializeField, HideInInspector]
        string _calibratedPrefabPath = "";
        #endregion

        #region Public Methods
        public void SetCalibratedPrefabPath(string path)
        {
            _calibratedPrefabPath = path;
        }
        #endregion
    }
}