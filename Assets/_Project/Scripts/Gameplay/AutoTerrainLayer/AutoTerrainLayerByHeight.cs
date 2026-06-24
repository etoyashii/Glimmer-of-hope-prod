using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Terrain))]
public class AutoTerrainLayerByHeight : MonoBehaviour
{
    public Terrain terrain;
    public LayerHeightRule[] rules; // Tableau de règles (layer + plage de hauteurs)

    private float[,,] originalAlphamaps; // Pour l'Undo
    private bool hasApplied = false;

    // Sauvegarde les alphamaps actuels
    void SaveOriginalAlphamaps()
    {
        TerrainData terrainData = terrain.terrainData;
        originalAlphamaps = terrainData.GetAlphamaps(
            0, 0,
            terrainData.alphamapWidth,
            terrainData.alphamapHeight
        );
    }

    // Applique les layers en fonction des hauteurs
    public void ApplyLayersByHeight()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>();

        TerrainData terrainData = terrain.terrainData;

        // Ajoute les layers manquants au terrain
        foreach (var rule in rules)
        {
            if (rule.layer == null)
            {
                Debug.LogError("Un TerrainLayer est manquant dans les règles !");
                return;
            }
            if (!terrainData.terrainLayers.Contains(rule.layer))
            {
                terrainData.terrainLayers = terrainData.terrainLayers.Append(rule.layer).ToArray();
            }
        }

        // Sauvegarde pour l'Undo
        SaveOriginalAlphamaps();
        hasApplied = true;

        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;
        int heightmapWidth = terrainData.heightmapResolution;
        int heightmapHeight = terrainData.heightmapResolution;

        float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);
        float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

        // Récupère les index des layers des règles
        int[] layerIndices = new int[rules.Length];
        for (int i = 0; i < rules.Length; i++)
        {
            layerIndices[i] = System.Array.IndexOf(terrainData.terrainLayers, rules[i].layer);
            if (layerIndices[i] == -1)
            {
                Debug.LogError($"Le TerrainLayer {rules[i].layer.name} est introuvable !");
                return;
            }
        }

        for (int y = 0; y < alphamapHeight; y++)
        {
            for (int x = 0; x < alphamapWidth; x++)
            {
                int heightX = (int)(x * (float)heightmapWidth / alphamapWidth);
                int heightY = (int)(y * (float)heightmapHeight / alphamapHeight);
                float height = heights[heightY, heightX];

                // Vérifie si la hauteur est dans une des plages
                bool isInRuleRange = false;
                for (int i = 0; i < rules.Length; i++)
                {
                    if (height >= rules[i].minHeight/600 && height <= rules[i].maxHeight/600)
                    {
                        // Réinitialise TOUS les layers pour ce point
                        for (int layer = 0; layer < terrainData.alphamapLayers; layer++)
                        {
                            alphamaps[y, x, layer] = 0;
                        }
                        // Active UNIQUEMENT le layer correspondant
                        alphamaps[y, x, layerIndices[i]] = 1f;
                        isInRuleRange = true;
                        break;
                    }
                }
                // Si la hauteur n'est dans AUCUNE plage, on ne touche PAS aux alphamaps
            }
        }

        terrainData.SetAlphamaps(0, 0, alphamaps);
    }

    // Annule les modifications
    public void UndoLayers()
    {
        if (originalAlphamaps != null && terrain != null && hasApplied)
        {
            terrain.terrainData.SetAlphamaps(0, 0, originalAlphamaps);
            hasApplied = false;
        }
        else
        {
            Debug.LogWarning("Rien à annuler !");
        }
    }
}