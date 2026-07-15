/// <summary>Payload d'un déplacement ou échange (from, to).</summary>
public readonly struct IntPairPayload
{
    public readonly int A;
    public readonly int B;

    public IntPairPayload(int a, int b) { A = a; B = b; }

    public override string ToString() => $"({A}, {B})";
}

/// <summary>Payload d'un placement d'item dans un slot.</summary>
public readonly struct ItemPlacedPayload
{
    public readonly int SlotIndex;
    public readonly ItemModel Item;

    public ItemPlacedPayload(int slotIndex, ItemModel item)
    {
        SlotIndex = slotIndex;
        Item = item;
    }
}