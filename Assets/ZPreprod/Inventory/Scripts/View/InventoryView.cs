using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vue principale de l'inventaire.
/// Génère la grille, écoute les SO channels du Model pour synchroniser l'UI.
/// Utilise le ServiceLocator pour accéder au Controller.
/// </summary>
public class InventoryView : MonoBehaviour
{
    #region Configuration

    [Header("Prefabs")]
    [SerializeField] private GameObject _slotPrefab;
    [SerializeField] private GameObject _itemPrefab;

    [Header("Conteneur (GridLayoutGroup)")]
    [SerializeField] private Transform _gridContainer;

    [Header("Channels entrants (Model → View)")]
    [SerializeField] private ItemPlacedEventChannel _onItemPlaced;
    [SerializeField] private IntEventChannel _onItemRemoved;
    [SerializeField] private IntEventChannel _onDropCancelled;

    #endregion

    #region State

    private SlotView[] _slots;
    private Dictionary<int, ItemView> _items = new();

    private bool _isInitialised;

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        _onItemPlaced?.Subscribe(OnPlaced);
        _onItemRemoved?.Subscribe(OnRemoved);
        _onDropCancelled?.Subscribe(OnDropCancelled);
    }

    private void OnDisable()
    {
        _onItemPlaced?.Unsubscribe(OnPlaced);
        _onItemRemoved?.Unsubscribe(OnRemoved);
        _onDropCancelled?.Unsubscribe(OnDropCancelled);
    }

    private void Start()
    {
        InventoryController ctrl = ServiceLocator.Get<InventoryController>();
        BuildGrid(ctrl.Model.Size);
    }

    #endregion

    #region Grid Construction

    private void BuildGrid(int count)
    {
        _isInitialised = false;

        foreach (Transform child in _gridContainer)
            Destroy(child.gameObject);

        _slots = new SlotView[count];
        _items.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(_slotPrefab, _gridContainer);
            SlotView slot = go.GetComponent<SlotView>();
            slot.Initialize(i);
            _slots[i] = slot;
        }

        _isInitialised = true;
    }

    #endregion

    #region Channel Handlers

    private void OnPlaced(ItemPlacedPayload payload)
    {
        if (!_isInitialised) return;

        int index = payload.SlotIndex;
        if (!IsValidIndex(index)) return;

        ItemModel item = payload.Item;


        if (_items.TryGetValue(index, out ItemView existing)
            && existing != null
            && existing.IsDragging)
        {
            Destroy(existing.gameObject);
            _items.Remove(index);
            existing = null;
        }

        if (!_items.TryGetValue(index, out ItemView view))
        {
            if (_slots[index] == null) return;

            GameObject go = Instantiate(_itemPrefab, _slots[index].transform);
            view = go.GetComponent<ItemView>();
            _items[index] = view;
        }

        if (_slots[index] == null) return;

        view.Initialize(item, index);
        view.AttachToSlot(_slots[index].transform, index);
        _slots[index].SetOccupied(true);
    }

    private void OnRemoved(int index)
    {
        if (!_isInitialised) return;
        if (!IsValidIndex(index)) return;

        if (_items.TryGetValue(index, out ItemView view))
        {
            if (view != null)
                Destroy(view.gameObject);

            _items.Remove(index);
        }

        if (_slots[index] != null)
            _slots[index].SetOccupied(false);
    }

    private void OnDropCancelled(int fromIndex)
    {
        if (!_isInitialised) return;

        if (_items.TryGetValue(fromIndex, out ItemView view) && view != null)
            view.ReturnToOrigin();
    }

    #endregion

    #region Helpers

    private bool IsValidIndex(int index) =>
        _slots != null && index >= 0 && index < _slots.Length;

    #endregion
}