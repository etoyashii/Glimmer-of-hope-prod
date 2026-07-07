using UnityEngine;

[CreateAssetMenu(fileName = "Brush Asset", menuName = "Scriptable Objects/New Brush Asset", order = 1)]
public class AssetTemplate : ScriptableObject
{
    #region Serialized Fields
    [SerializeField] public GameObject _asset;      // Prefab to instantiate
    [SerializeField] public Vector2 _limiteSize;    // Min/Max scale range for the asset
    [SerializeField] public int _weight;            // Weight for random selection
    [SerializeField, Tooltip("leave 0 if there is no problem")] public Vector3 _rotation;            // Rotation in case the assets is in the ground
    [SerializeField] public bool _fullRotation;            // Rotation in case the assets is in the ground
    [SerializeField] public bool _noRandomRotation;            // active or not rotation
    [SerializeField] public rotationType _rotationType;            // j'ai pas envie d'explique c'est obvious
    #endregion
}

public enum rotationType
{
    xRotation,
    yRotation,
    zRotation
}