using UnityEngine;

/// <summary>
/// Template d'un item.
/// Données partagées, read-only en runtime.
/// Ne jamais manipuler directement — toujours instancier via new ItemModel(itemSO).
/// Create via : clic droit > create > inventory > item
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    #region Data

    public string itemName = "Nouvel Item";

    [TextArea(3, 6)]
    public string description = "Description de l'item.";

    public Sprite icon;

    #endregion
}