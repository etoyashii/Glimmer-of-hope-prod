using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Fixes the two FBX import issues from Blender:
///   1) Default Scale 100 -> we enforce an actual Scale of 50 on the imported model's root Transform (visible as is, NOT hidden)
///   2) Z-up orientation (Blender) -> Y-up (Unity) -> baked into the mesh so
///      that the Transform keeps a neutral rotation (0,0,0)
/// </summary>

namespace GlimmerOfHope.Editor
{

    public class BlenderFbxImportFix : AssetPostprocessor
    {

        #region Public Properties
        static readonly uint k_Version = 2;
        public override uint GetVersion() => k_Version;

        // The actual scale to apply on the root Transform of the imported model
        public const float kTargetScale = 50f;
        #endregion

        #region Private Fields
        // Marker stored in the .meta (userData) to know if the fix has already
        // been validated for this asset, and therefore if it should be automatically
        // reapplied on each reimport
        const string kFixedMarker = "BLENDER_FIX_SCALE50_APPLIED";

        // List of FBXs that have just been imported in this batch, waiting for
        // user validation via the popup
        static List<string> s_PendingImports = new List<string>();
        #endregion

        #region Unity LifeCycle
        void OnPreprocessModel()
        {
            ModelImporter importer = assetImporter as ModelImporter;
            if (importer == null) return;

            // Scale Factor left at 1, according to the request: only the
            // root Transform will carry the actual scale of 50, not the ModelImporter
            importer.globalScale = 1f;

            // Bake of the Z-up -> Y-up axis conversion directly into the mesh,
            // so that the imported root Transform keeps a neutral rotation
            importer.bakeAxisConversion = true;
        }

        void OnPostprocessModel(GameObject root)
        {
            if (root == null) return;

            bool alreadyFixed = assetImporter.userData != null
                && assetImporter.userData.Contains(kFixedMarker);

            if (alreadyFixed)
            {
                // The fix has already been validated once for this asset: we
                // systematically reapply it on each reimport (modification of the source FBX,
                // change of settings, etc.) without going through the popup again
                root.transform.localScale = Vector3.one * kTargetScale;
            }
            else
            {
                // New asset (or not yet validated): we do not touch the scale
                // and we add it to the queue to offer the button
                if (!s_PendingImports.Contains(assetPath))
                    s_PendingImports.Add(assetPath);
            }
        }

        /// <summary>
        /// Called by Unity after a complete import batch We use this entry
        /// point to trigger the opening of the popup once everything is
        /// stable (we cannot open a window during the import itself)
        /// </summary>
        static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (s_PendingImports.Count == 0) return;

            // We copy and clear the queue before the delayed call, to avoid
            // duplicates if another import batch arrives in the meantime
            var toShow = s_PendingImports.Distinct().ToList();
            s_PendingImports.Clear();

            EditorApplication.delayCall += () =>
            {
                BlenderFixPopup.ShowForAssets(toShow);
            };
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Marks an asset as "fix validated" in its .meta, then reimports it
        /// so that OnPostprocessModel immediately applies the Scale 50
        /// Called by the popup when the user clicks on the button
        /// </summary>
        public static void ApplyFixToAsset(string assetPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[BlenderFbxImportFix] Impossible de retrouver l'importer pour : {assetPath}");
                return;
            }

            string existing = importer.userData ?? "";
            if (!existing.Contains(kFixedMarker))
            {
                importer.userData = string.IsNullOrEmpty(existing)
                    ? kFixedMarker
                    : existing + ";" + kFixedMarker;
            }

            importer.SaveAndReimport();

            Debug.Log($"[BlenderFbxImportFix] Scale {kTargetScale} appliqué sur le root de : {assetPath}");
        }
        #endregion
    }
}