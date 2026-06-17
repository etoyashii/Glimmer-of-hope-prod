using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Core;

/// <summary>
/// Custom editor for BrushManager, handling asset placement/deletion.
/// </summary>
[CustomEditor(typeof(BrushManager))]
public class BrushManagerEditor : Editor
{
    private AssetTemplate GetTempltateByWeight(BrushManager drawer, int totalWeight)
    {
        // Randomly select an asset template based on their weights
        int reste = Random.Range(0, totalWeight);
        int i = 0;
        List<AssetTemplate> listTemplate = drawer.Assets;
        while (reste > 0)
        {
            reste -= drawer.Assets[i]._weight;
            if (reste > 0)
            {
                i++;
            }
        }
        return drawer.Assets[i];
    }

    private void OnSceneGUI()
    {
        BrushManager drawer = (BrushManager)target;
        List<AssetTemplate> assets = drawer.Assets;
        bool deleteMod = drawer.DeleteMode;
        bool lastActionWasAdd = drawer._lastActionWasAdd;
        int totalWeight = 0;

        // Calculate total weight for all asset templates
        foreach (AssetTemplate template in assets)
        {
            totalWeight += template._weight;
        }
        float invertDensity = 1f;
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
                    // Undo: move last placed asset to inactive storage
                    if (drawer.StokageAssets.transform.childCount > 0)
                    {
                        GameObject toDelete = drawer.StokageAssets.transform.GetChild(drawer.StokageAssets.transform.childCount - 1).gameObject;
                        toDelete.transform.parent = drawer.StokageAssetsUseless.transform;
                        toDelete.SetActive(false);
                    }
                    // Limit inactive storage size
                    if (drawer.StokageAssetsUseless.transform.childCount > drawer.RevertNumber)
                    {
                        DestroyImmediate(drawer.StokageAssetsUseless.transform.GetChild(0).gameObject);
                    }
                    e.Use();
                }
                else
                {
                    // Redo: move last deleted asset back to active storage
                    if (drawer.StokageAssetsUseless.transform.childCount > 0)
                    {
                        GameObject toDelete = drawer.StokageAssetsUseless.transform.GetChild(drawer.StokageAssetsUseless.transform.childCount - 1).gameObject;
                        toDelete.transform.parent = drawer.StokageAssets.transform;
                        toDelete.SetActive(true);
                    }
                    e.Use();
                }
            }
            else if (e.keyCode == KeyCode.Y)
            {
                if (lastActionWasAdd)
                {
                    // Redo: move last deleted asset back to active storage
                    if (drawer.StokageAssetsUseless.transform.childCount > 0)
                    {
                        GameObject toAdd = drawer.StokageAssetsUseless.transform.GetChild(drawer.StokageAssetsUseless.transform.childCount - 1).gameObject;
                        toAdd.transform.parent = drawer.StokageAssets.transform;
                        toAdd.SetActive(true);
                    }
                    e.Use();
                }
                else
                {
                    // Undo: move last placed asset to inactive storage
                    if (drawer.StokageAssets.transform.childCount > 0)
                    {
                        GameObject toAdd = drawer.StokageAssets.transform.GetChild(drawer.StokageAssets.transform.childCount - 1).gameObject;
                        toAdd.transform.parent = drawer.StokageAssetsUseless.transform;
                        toAdd.SetActive(false);
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
            if (!deleteMod)
            {
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
                    DestroyImmediate(drawer.StokageAssetsUseless.transform.GetChild(0).gameObject);
                }
                e.Use();
            }
        }

        // Clean up empty temporary parents on mouse up
        if (e.type == EventType.MouseUp)
        {
            if (!deleteMod && drawer.StokageAssets.transform.GetChild(drawer.StokageAssets.transform.childCount - 1).childCount == 0)
            {
                drawer._lastActionWasAdd = !drawer._lastActionWasAdd;
                lastActionWasAdd = drawer._lastActionWasAdd;
            }
            if (deleteMod && drawer.StokageAssetsUseless.transform.GetChild(drawer.StokageAssetsUseless.transform.childCount - 1).childCount == 0)
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
        }

        // Place assets on left mouse drag
        if (!deleteMod && e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
        {
            lastActionWasAdd = true;

            float area = Mathf.PI * drawer._circleRadius * drawer._circleRadius;
            int assetsToSpawn = Mathf.Max(1, Mathf.RoundToInt(area * drawer.Density * drawer.MultDensity));
            float overlapRadius = 1f / Mathf.Max(drawer.MultDensity, drawer.Density);

            for (int i = 0; i < assetsToSpawn; i++)
            {
                // Random position within brush radius
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(0f, drawer._circleRadius);
                Vector3 spawnPos = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0,
                    Mathf.Sin(angle) * distance
                ) + drawer.Pos;

                // Raycast to ground
                Ray spawnRay = new Ray(spawnPos + Vector3.up * drawer.RaycastDistance, Vector3.down);
                if (Physics.Raycast(spawnRay, out RaycastHit spawnHit, drawer.RaycastDistance * 2f, drawer.GroundLayer))
                {
                    spawnPos = spawnHit.point;
                }

                // Select random asset template based on weight
                AssetTemplate newTemplate = GetTempltateByWeight(drawer, totalWeight);

                // Check for collisions before placing
                int layerMaskWithoutGround = ~drawer.GroundLayerMask;
                Collider[] hitColliders = Physics.OverlapSphere(spawnPos, overlapRadius, layerMaskWithoutGround);

                if (hitColliders.Length == 0 && drawer.StokageAssets.transform.childCount > 0)
                {
                    // Instantiate asset
                    GameObject newnewGO = Instantiate(
                        newTemplate._asset,
                        spawnPos,
                        Quaternion.FromToRotation(Vector3.up, spawnHit.normal),
                        drawer.StokageAssets.transform.GetChild(drawer.StokageAssets.transform.childCount - 1).transform
                    );
                    // Random rotation and scale
                    newnewGO.transform.Rotate(Vector3.right, 90f);
                    newnewGO.transform.Rotate(Vector3.forward, Random.Range(0, 360f));
                    float newScale = Random.Range(newTemplate._limiteSize.x, newTemplate._limiteSize.y);
                    newnewGO.transform.localScale = Vector3.one * newScale * drawer.SizeMult;
                }
            }
        }
        // Delete assets on left mouse drag in delete mode
        else if (deleteMod && e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
        {
            lastActionWasAdd = false;
            int layerMaskWithoutGround = ~drawer.GroundLayerMask;
            Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Physics.Raycast(worldRay, out hit);
            Collider[] hitColliders = Physics.OverlapSphere(hit.point, drawer._circleRadius, layerMaskWithoutGround);
            foreach (Collider collider in hitColliders)
            {
                collider.gameObject.transform.parent = drawer.StokageAssetsUseless.transform.GetChild(drawer.StokageAssetsUseless.transform.childCount - 1).transform;
            }
        }
    }
}