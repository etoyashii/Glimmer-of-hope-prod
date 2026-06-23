using UnityEngine;

[CreateAssetMenu(fileName = "Brush Asset", menuName = "Scriptable Objects/New Brush Asset", order = 1)]
public class AssetTemplate : ScriptableObject
{
    #region Serialized Fields
    [SerializeField] public GameObject _asset;      // Prefab to instantiate
    [SerializeField] public Vector2 _limiteSize;    // Min/Max scale range for the asset
    [SerializeField] public int _weight;            // Weight for random selection
    #endregion
}