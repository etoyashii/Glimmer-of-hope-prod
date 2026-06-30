using GlimmerOfHope.Core;
using NaughtyAttributes.Test;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom editor for BrushManager, handling asset placement/deletion.
/// </summary>
[CustomEditor(typeof(BrushManager))]
public class BrushManagerEditor : Editor
{
    private Vector3 _lastClearPosition = Vector3.positiveInfinity;

    public Vector2 WorldToSplatAlpha(BrushManager drawer, Vector3 worldPos)
    {
        Vector3 terrainPos = drawer.UsedTerrain.transform.position;
        Vector3 terrainSize = drawer.UsedTerrain.terrainData.size;

        float u = (worldPos.x - terrainPos.x) / terrainSize.x;
        float v = (worldPos.z - terrainPos.z) / terrainSize.z;

        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        return new Vector2(u, v);
    }

    private AssetTemplate GetTemplateByWeight(BrushManager drawer)
    {
        // Randomly select an asset template based on their weights
        // Only consider active (non-disabled) asset structs
        List<AssetTemplate> activeAssets = new();
        foreach (AssetsStruct aStruct in drawer.Assets)
        {
            if (!aStruct._isDisable)
            {
                foreach (AssetTemplate aTemplate in aStruct._template)
                {
                    activeAssets.Add(aTemplate);
                }
            }
        }

        if (activeAssets.Count <= 0) return null;

        // Calculate total weight only on active assets
        int totalWeight = 0;
        foreach (AssetTemplate template in activeAssets)
        {
            totalWeight += template._weight;
        }

        int reste = Random.Range(0, totalWeight);
        int i = 0;
        while (reste > 0)
        {
            reste -= activeAssets[i]._weight;
            if (reste > 0) i++;
        }
        return activeAssets[i];
    }

    /// <summary>
    /// Moves every direct child of a TempParent between placedAssets and unplacedAssets lists.
    /// Used when an entire TempParent (a brush stroke) is moved between active/inactive storage.
    /// </summary>
    private void MoveTempParentChildren(Transform tempParent, List<Transform> from, List<Transform> to)
    {
        foreach (Transform child in tempParent)
        {
            from.Remove(child);
            if (!to.Contains(child))
                to.Add(child);
        }
    }

    /// <summary>
    /// Walks up from a hit collider to find the placed asset root:
    /// the direct child of a TempParent (itself a direct child of StokageAssets).
    /// Returns null if no such root is found.
    /// </summary>
    private Transform GetPlacedAssetRoot(BrushManager drawer, Transform t)
    {
        while (t != null)
        {
            if (t.parent != null && t.parent.parent == drawer.StokageAssets.transform)
                return t; // t is a direct child of a TempParent under StokageAssets
            t = t.parent;
        }
        return null;
    }

    /// <summary>
    /// Removes destroyed (null) references from both tracking lists.
    /// Call after any DestroyImmediate that could affect placed/unplaced assets.
    /// </summary>
    private void PurgeDestroyedRefs(BrushManager drawer)
    {
        drawer.placedAssets.RemoveAll(t => t == null);
        drawer.unplacedAssets.RemoveAll(t => t == null);
    }

    private void OnSceneGUI()
    {
        BrushManager drawer = (BrushManager)target;
        List<AssetTemplate> assets = new();
        foreach (AssetsStruct aStruct in drawer.Assets)
        {
            foreach (AssetTemplate assetTemplate in aStruct._template)
            {
                assets.Add(assetTemplate);
            }
        }
        bool deleteMod = drawer.DeleteMode;
        bool clearMod = drawer.ClearMode;
        bool lastActionWasAdd = drawer._lastActionWasAdd;

        if (drawer == null || assets == null || assets.Count == 0) return;

        // Handle mouse and keyboard input for brush tool
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        // Update brush position to mouse position on ground
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, drawer.GroundLayerMask))
        {
            drawer.SetPos(hit.point);
            drawer.transform.position = hit.point;
            SceneView.RepaintAll();
        }

        // Undo/Redo with Ctrl+Alt+Z/Y
        if (e.control && e.alt && e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Z)
            {
                Debug.Log(lastActionWasAdd);
                if (lastActionWasAdd)
                {
                    // Undo: move last placed TempParent (and its children) to inactive storage
                    if (drawer.StokageAssets.transform.childCount > 0)
                    {
                        Transform toDelete = drawer.StokageAssets.transform.GetChild(drawer.StokageAssets.transform.childCount - 1);
                        MoveTempParentChildren(toDelete, drawer.placedAssets, drawer.unplacedAssets);
                        toDelete.gameObject.SetActive(false);
                        toDelete.SetParent(drawer.StokageAssetsUseless.transform);
                    }
                    // Limit inactive storage size
                    if (drawer.StokageAssetsUseless.transform.childCount > drawer.RevertNumber)
                    {
                        Transform oldest = drawer.StokageAssetsUseless.transform.GetChild(0);
                        // These children are about to be destroyed permanently: drop them from tracking
                        foreach (Transform child in oldest)
                            drawer.unplacedAssets.Remove(child);
                        DestroyImmediate(oldest.gameObject);
                    }
                    e.Use();
                }
                else
                {
                    // Redo: move last deleted TempParent (and its children) back to active storage
                    if (drawer.StokageAssetsUseless.transform.childCount > 0)
                    {
                        Transform toDelete = drawer.StokageAssetsUseless.transform.GetChild(drawer.StokageAssetsUseless.transform.childCount - 1);
                        toDelete.SetParent(drawer.StokageAssets.transform);
                        toDelete.gameObject.SetActive(true);
                        MoveTempParentChildren(toDelete, drawer.unplacedAssets, drawer.placedAssets);
                    }
                    e.Use();
                }
            }
            else if (e.keyCode == KeyCode.Y)
            {
                if (lastActionWasAdd)
                {
                    // Redo: move last deleted TempParent (and its children) back to active storage
                    if (drawer.StokageAssetsUseless.transform.childCount > 0)
                    {
                        Transform toAdd = drawer.StokageAssetsUseless.transform.GetChild(drawer.StokageAssetsUseless.transform.childCount - 1);
                        toAdd.SetParent(drawer.StokageAssets.transform);
                        toAdd.gameObject.SetActive(true);
                        MoveTempParentChildren(toAdd, drawer.unplacedAssets, drawer.placedAssets);
                    }
                    e.Use();
                }
                else
                {
                    // Undo: move last placed TempParent (and its children) to inactive storage
                    if (drawer.StokageAssets.transform.childCount > 0)
                    {
                        Transform toAdd = drawer.StokageAssets.transform.GetChild(drawer.StokageAssets.transform.childCount - 1);
                        MoveTempParentChildren(toAdd, drawer.placedAssets, drawer.unplacedAssets);
                        toAdd.gameObject.SetActive(false);
                        toAdd.SetParent(drawer.StokageAssetsUseless.transform);
                    }
                    e.Use();
                }
            }
        }

        // Adjust brush radius with mouse wheel
        if (e.control && e.type == EventType.ScrollWheel)
        {
            float scrollDelta = e.delta.y;
            if (scrollDelta > 0)
            {
                drawer._circleRadius += 0.5f;
                drawer._circleRadius = Mathf.Min(drawer._circleRadius, 10000f);
            }
            else if (scrollDelta < 0)
            {
                drawer._circleRadius -= 0.5f;
                drawer._circleRadius = Mathf.Max(0.1f, drawer._circleRadius);
            }
            e.Use();
        }

        // Create temporary parent for new assets on mouse down
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            deleteMod = drawer.DeleteMode;
            clearMod = drawer.ClearMode;
            if (!deleteMod && !clearMod)
            {
                drawer._actualColor = Color.green;
                drawer._lastActionWasAdd = true;
                lastActionWasAdd = drawer._lastActionWasAdd;
                GameObject newGO = new GameObject("TempParent");
                newGO.transform.parent = drawer.StokageAssets.transform;
                e.Use();
            }
            else
            {
                drawer._lastActionWasAdd = false;
                lastActionWasAdd = drawer._lastActionWasAdd;
                GameObject newGO = new GameObject("TempParent");
                newGO.transform.parent = drawer.StokageAssetsUseless.transform;
                newGO.SetActive(false);
                if (drawer.StokageAssetsUseless.transform.childCount > drawer.RevertNumber)
                {
                    Transform oldest = drawer.StokageAssetsUseless.transform.GetChild(0);
                    foreach (Transform child in oldest)
                        drawer.unplacedAssets.Remove(child);
                    DestroyImmediate(oldest.gameObject);
                }
                e.Use();
            }
        }

        // Clean up empty temporary parents on mouse up
        if (e.type == EventType.MouseUp)
        {
            _lastClearPosition = Vector3.positiveInfinity;

            if (!deleteMod && !clearMod && drawer.StokageAssets.transform.childCount > 0
                && drawer.StokageAssets.transform.GetChild(drawer.StokageAssets.transform.childCount - 1).childCount == 0)
            {
                drawer._actualColor = Color.green;
                drawer._lastActionWasAdd = !drawer._lastActionWasAdd;
                lastActionWasAdd = drawer._lastActionWasAdd;
            }
            if ((deleteMod || clearMod) && drawer.StokageAssetsUseless.transform.childCount > 0
                && drawer.StokageAssetsUseless.transform.GetChild(drawer.StokageAssetsUseless.transform.childCount - 1).childCount == 0)
            {
                drawer._lastActionWasAdd = !drawer._lastActionWasAdd;
                lastActionWasAdd = drawer._lastActionWasAdd;
            }
            foreach (Transform child in drawer.StokageAssets.transform)
            {
                if (child.childCount == 0)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            foreach (Transform child in drawer.StokageAssetsUseless.transform)
            {
                if (child.childCount == 0)
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            // Safety net: drop any references that got destroyed along the way
            PurgeDestroyedRefs(drawer);
        }

        // Place assets on left mouse drag
        if (!deleteMod && !clearMod && e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
        {
            drawer._actualColor = Color.green;
            lastActionWasAdd = true;

            float area = Mathf.PI * drawer._circleRadius * drawer._circleRadius;
            int assetsToSpawn = Mathf.Max(1, Mathf.RoundToInt(area * drawer.Density * drawer.MultDensity));

            // overlapRadius based only on Density: high density == smaller gap between objects
            float overlapRadius = 1f / Mathf.Max(1f, drawer.Density);

            for (int i = 0; i < assetsToSpawn; i++)
            {
                // Random position within brush radius
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = drawer._circleRadius * Mathf.Sqrt(Random.Range(0f, 1f));
                Vector3 spawnPos = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0,
                    Mathf.Sin(angle) * distance
                ) + drawer.Pos;

                // Raycast to ground
                Ray spawnRay = new Ray(spawnPos + Vector3.up * drawer.RaycastDistance, Vector3.down);
                if (!Physics.Raycast(spawnRay, out RaycastHit spawnHit, drawer.RaycastDistance * 2f, drawer.GroundLayer))
                    continue;

                spawnPos = spawnHit.point;

                // Skip if slope exceeds max angle
                if (Vector3.Angle(Vector3.up, spawnHit.normal) > drawer.MaxSlopeAngle)
                    continue;

                // Skip if terrain layer filter is enabled and position is not on the required layer
                if (!drawer.IsOnTerrainLayer(spawnPos))
                    continue;

                AssetTemplate newTemplate = GetTemplateByWeight(drawer);
                if (newTemplate != null)
                {
                    int layerMaskWithoutGround = ~drawer.GroundLayerMask;
                    Collider[] hitColliders = Physics.OverlapSphere(spawnPos, overlapRadius, layerMaskWithoutGround);

                    if (hitColliders.Length == 0 && drawer.StokageAssets.transform.childCount > 0)
                    {
                        GameObject newnewGO = Instantiate(
                            newTemplate._asset,
                            spawnPos,
                            Quaternion.FromToRotation(Vector3.up, spawnHit.normal),
                            drawer.StokageAssets.transform.GetChild(drawer.StokageAssets.transform.childCount - 1).transform
                        );
                        // Random rotation and scale
                        if (!newTemplate._fullRotation)
                        {
                            newnewGO.transform.Rotate(Vector3.up, Random.Range(0, 360f));
                        }
                        else
                        {
                            newnewGO.transform.Rotate(Vector3.right, Random.Range(0, 360f));
                            newnewGO.transform.Rotate(Vector3.forward, Random.Range(0, 360f));
                            newnewGO.transform.Rotate(Vector3.up, Random.Range(0, 360f));
                        }
                        float newScale = Random.Range(newTemplate._limiteSize.x, newTemplate._limiteSize.y);
                        newnewGO.transform.localScale = newnewGO.transform.localScale * newScale * drawer.SizeMult;

                        // Track the placed root (direct child of the current TempParent)
                        drawer.placedAssets.Add(newnewGO.transform);
                    }
                }
            }
        }
        // Delete assets on left mouse drag in delete mode
        else if (deleteMod && !clearMod && e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
        {
            drawer._actualColor = Color.red;
            lastActionWasAdd = false;
            int layerMaskWithoutGround = ~drawer.GroundLayerMask;
            Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Physics.Raycast(worldRay, out hit);
            Collider[] hitColliders = Physics.OverlapSphere(hit.point, drawer._circleRadius, layerMaskWithoutGround);

            if (drawer.StokageAssetsUseless.transform.childCount > 0)
            {
                Transform destination = drawer.StokageAssetsUseless.transform.GetChild(drawer.StokageAssetsUseless.transform.childCount - 1);
                foreach (Collider collider in hitColliders)
                {
                    // Walk up from the hit collider to find the actual tracked root
                    Transform root = GetPlacedAssetRoot(drawer, collider.transform);
                    if (root != null && drawer.placedAssets.Contains(root))
                    {
                        root.SetParent(destination);
                        drawer.placedAssets.Remove(root);
                        drawer.unplacedAssets.Add(root);
                    }
                }
            }
        }
        else if (clearMod && e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
        {
            drawer._actualColor = Color.orange;
            lastActionWasAdd = false;

            Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Physics.Raycast(worldRay, out hit);

            float minStep = drawer._circleRadius * 0.5f;
            if (e.type != EventType.MouseDown && Vector3.Distance(hit.point, _lastClearPosition) < minStep)
            {
                e.Use();
                return;
            }
            _lastClearPosition = hit.point;

            int layerMaskWithoutGround = ~drawer.GroundLayerMask;
            Collider[] hitColliders = Physics.OverlapSphere(hit.point, drawer._circleRadius, layerMaskWithoutGround);

            if (drawer.StokageAssetsUseless.transform.childCount > 0)
            {
                Transform destination = drawer.StokageAssetsUseless.transform.GetChild(drawer.StokageAssetsUseless.transform.childCount - 1);
                foreach (Collider collider in hitColliders)
                {
                    if (Random.Range(0, 100) < drawer.ProbClearAssets)
                    {
                        Transform root = GetPlacedAssetRoot(drawer, collider.transform);
                        if (root != null && drawer.placedAssets.Contains(root))
                        {
                            root.SetParent(destination);
                            drawer.placedAssets.Remove(root);
                            drawer.unplacedAssets.Add(root);
                        }
                    }
                }
            }
            e.Use();
        }
    }
}