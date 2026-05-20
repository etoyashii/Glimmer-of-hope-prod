using UnityEngine;

/// <summary>Canal portant un entier (index de slot).</summary>
[CreateAssetMenu(menuName = "Inventory/Events/IntEventChannel")]
public class IntEventChannel : EventChannel<int> { }

/// <summary>Canal portant deux entiers (from / to).</summary>
[CreateAssetMenu(menuName = "Inventory/Events/IntPairEventChannel")]
public class IntPairEventChannel : EventChannel<IntPairPayload> { }

/// <summary>Canal portant index + ItemModel (item placé dans un slot).</summary>
[CreateAssetMenu(menuName = "Inventory/Events/ItemPlacedEventChannel")]
public class ItemPlacedEventChannel : EventChannel<ItemPlacedPayload> { }