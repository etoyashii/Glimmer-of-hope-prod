using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Modèle de l'inventaire.
/// List&lt;ItemModel&gt; : null = slot vide, index = identité du slot.
/// Publie des events C# purs — le Controller les relaie vers les SO channels.
/// </summary>
public class InventoryModel
{
    #region Events

    public event Action<int, ItemModel> OnItemPlaced;
    public event Action<int> OnItemRemoved;
    public event Action<int, int> OnItemMoved;
    public event Action<int, int> OnItemSwapped;

    #endregion

    #region State

    private readonly List<ItemModel> _slots;

    public int Size => _slots.Count;
    public ReadOnlyCollection<ItemModel> Slots => _slots.AsReadOnly();

    #endregion

    #region Constructor

    public InventoryModel(int size)
    {
        _slots = new List<ItemModel>(size);
        for (int i = 0; i < size; i++)
            _slots.Add(null);
    }

    #endregion

    #region Read

    public ItemModel Get(int index) => IsValid(index) ? _slots[index] : null;
    public bool IsEmpty(int index) => Get(index) == null;

    #endregion

    #region Write

    /// <summary>Place dans le premier slot vide. Retourne l'index ou -1 si plein.</summary>
    public int Add(ItemModel item)
    {
        int index = _slots.IndexOf(null);
        if (index == -1) { Debug.LogWarning("[InventoryModel] Inventaire plein."); return -1; }

        SetAt(index, item);
        return index;
    }

    public bool Remove(int index)
    {
        if (!IsValid(index) || IsEmpty(index)) return false;

        _slots[index] = null;
        OnItemRemoved?.Invoke(index);
        return true;
    }

    /// <summary>Déplace ou échange deux slots. fromIndex doit être non-vide.</summary>
    public bool Move(int from, int to)
    {
        if (!IsValid(from) || !IsValid(to) || from == to) return false;
        if (IsEmpty(from)) return false;

        if (IsEmpty(to))
            ExecuteMove(from, to);
        else
            ExecuteSwap(from, to);

        return true;
    }

    #endregion

    #region Private

    private void SetAt(int index, ItemModel item)
    {
        _slots[index] = item;
        OnItemPlaced?.Invoke(index, item);
    }

    private void ExecuteMove(int from, int to)
    {
        ItemModel item = _slots[from];
        _slots[from] = null;
        OnItemRemoved?.Invoke(from);

        _slots[to] = item;
        OnItemPlaced?.Invoke(to, item);
        OnItemMoved?.Invoke(from, to);
    }

    private void ExecuteSwap(int from, int to)
    {
        (_slots[from], _slots[to]) = (_slots[to], _slots[from]);
        OnItemPlaced?.Invoke(from, _slots[from]);
        OnItemPlaced?.Invoke(to, _slots[to]);
        OnItemSwapped?.Invoke(from, to);
    }

    private bool IsValid(int i) => i >= 0 && i < _slots.Count;

    #endregion
}