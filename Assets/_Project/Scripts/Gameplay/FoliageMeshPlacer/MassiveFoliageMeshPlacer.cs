using System.Collections.Generic;
using UnityEngine;

public class MassiveFoliageMeshPlacer : MonoBehaviour
{
    [Header("Step 1: Attach The Terrain")]
    public Terrain terrain;
    [Header("Step 2: Select Target Texture and Source")]
    public List<LayerTerrain> TerrainLayers;
    private int targetTextureLayer = 1;
    private int targetDetailsLayer = 0;
    [Header("Step 3: Adjust Parameters")]
    public bool EraseModification = true;
    public FillType fillType = FillType.full;
    public enum FillType { full, sides };

    const int MAX_AMOUNT = 500;
    private int amount = 1;
    private float fallOff = 0.8f;

    [System.Serializable]
    public class DetailsID
    {
        [HideInInspector]
        public string name;
        public int DetailsLayer;
        [Range(1, MAX_AMOUNT)]
        public int amount = 50;
        [Range(0, 1)]
        public float fallOff = 0.8f;
    }

    [System.Serializable]
    public class LayerTerrain
    {
        [HideInInspector]
        public string name;
        public List<DetailsID> Details;
    }

    public void GenerateDetails()
    {

        for (targetTextureLayer = 0; targetTextureLayer < TerrainLayers.Count; targetTextureLayer++)
        {
            foreach (var d in TerrainLayers[targetTextureLayer].Details)
            {
                targetDetailsLayer = d.DetailsLayer;
                amount = d.amount;
                fallOff = d.fallOff;

                AddOneDetails();
            }
        }
    }


    public void AddOneDetails()//Add details by the selected texture and selected presents
    {
        #region Get Terrain Data
        TerrainData terrainData = terrain.terrainData;
        float[,,] alphaMapData = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);//The terrain texture maps
        int[,] detailsMap = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, targetDetailsLayer);//The terrain detail maps(Where everything is placed)
        #endregion
        #region Convert texture map data length to details map length
        Texture2D temp = new Texture2D(terrainData.alphamapWidth, terrainData.alphamapHeight);
        for (int x = 0; x < terrainData.alphamapWidth; x++)
        {
            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                temp.SetPixel(x, y, new Color(0, 0, 0, alphaMapData[x, y, targetTextureLayer]));
            }
        }
        temp.Apply();
        int targetLength = detailsMap.GetLength(0);
        TextureScale.Bilinear(temp, targetLength, targetLength);
        temp.Apply();
        #endregion
        #region Apply detail data by user presents and selected texture
        //detailsMap = new int[targetLength, targetLength];
        for (int x = 0; x < targetLength; x += 1)
        {
            for (int y = 0; y < targetLength; y += 1)
            {
                if (fillType == FillType.full)
                {
                    detailsMap[x, y] = (temp.GetPixel(x, y).a > fallOff ? amount : detailsMap[x, y]);
                }
                else if (temp.GetPixel(x, y).a == 1)
                {
                    int totalPoints = 0;
                    totalPoints += (int)(temp.GetPixel(x - 1, y).a < 1 ? 1 : 0);
                    totalPoints += (int)(temp.GetPixel(x + 1, y).a < 1 ? 1 : 0);
                    totalPoints += (int)(temp.GetPixel(x, y - 1).a < 1 ? 1 : 0);
                    totalPoints += (int)(temp.GetPixel(x, y + 1).a < 1 ? 1 : 0);
                    if (totalPoints > 0)
                    {
                        detailsMap[x, y] = amount;
                    }
                }
            }
        }
        #endregion
        terrainData.SetDetailLayer(0, 0, targetDetailsLayer, detailsMap);
    }
    public void CleanDetails(int layer)//Clear the details of selected layer.
    {
        TerrainData terrainData = terrain.terrainData;
        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            int[,] map = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, layer == -1 ? i : layer);

            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    map[x, y] = 0;
                }
            }
            terrainData.SetDetailLayer(0, 0, layer == -1 ? i : layer, map);
            if (layer != -1)
            {
                return;
            }
        }
    }

    private void OnValidate()
    {
        if (!terrain)
        {
            terrain = GetComponentInChildren<Terrain>();
            if (!terrain)
            {
                terrain = GetComponentInParent<Terrain>();
                if (!terrain) terrain = FindAnyObjectByType<Terrain>();
            }
        }
        while (TerrainLayers.Count < terrain.terrainData.terrainLayers.Length)
            TerrainLayers.Add(new LayerTerrain());
        while (TerrainLayers.Count > terrain.terrainData.terrainLayers.Length)
            TerrainLayers.RemoveAt(TerrainLayers.Count - 1);

        int i = 0;
        foreach (var terrainL in TerrainLayers)
        {
            terrainL.name = terrain.terrainData.terrainLayers[i].name;
            foreach (var detailsL in terrainL.Details)
            {
                if (detailsL.DetailsLayer >= terrain.terrainData.detailPrototypes.Length)
                    detailsL.DetailsLayer = terrain.terrainData.detailPrototypes.Length - 1;
                if (detailsL.DetailsLayer < 0)
                    detailsL.DetailsLayer = 0;
                detailsL.name = terrain.terrainData.detailPrototypes[detailsL.DetailsLayer].prototype.name;
            }

            i++;
        }
    }
}
