using UnityEngine;

[System.Serializable]
public class LayerHeightRule
{
    public TerrainLayer layer; // Le TerrainLayer à appliquer
    [Range(0f, 600f), Tooltip("Hauteur minimale NORMALISÉE (0 = bas, 1 = haut)")]
    public float minHeight;
    [Range(0f, 600f), Tooltip("Hauteur maximale NORMALISÉE (0 = bas, 1 = haut)")]
    public float maxHeight;
}