using UnityEngine;

/// <summary>
/// Instance runtime d'un item.
/// Créée à partir d'un ItemSO (lecture seule).
/// </summary>
public class ItemModel
{
    #region Properties

    public string Name { get; }
    public string Description { get; }
    public Sprite Icon { get; }

    public ItemSO Source { get; }

    #endregion

    #region Constructor

    public ItemModel(ItemSO so)
    {
        Source = so;
        Name = so.itemName;
        Description = so.description;
        Icon = so.icon;
    }

    #endregion

    #region Utils

    public override string ToString() => $"ItemModel({Name})";

    #endregion
}