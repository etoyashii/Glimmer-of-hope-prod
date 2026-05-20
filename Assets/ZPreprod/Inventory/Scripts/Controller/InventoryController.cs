using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contrôleur de l'inventaire.
///
/// - Crée et possède le Model
/// - S'enregistre dans le ServiceLocator (ADR-005)
/// - Relaie les events C# du Model vers les SO channels (ADR-002)
/// - Écoute les SO channels View pour les inputs utilisateur
/// </summary>
public class InventoryController : MonoBehaviour
{
    #region Configuration

    [Header("Taille")]
    [SerializeField] private int _columns = 5;
    [SerializeField] private int _rows = 4;

    [Header("Items de départ (templates SO)")]
    [SerializeField] private List<ItemSO> _startingItems;

    #endregion

    #region Channels — Model → View

    [Header("Channels sortants (Model → View)")]
    [SerializeField] private ItemPlacedEventChannel _onItemPlaced;
    [SerializeField] private IntEventChannel _onItemRemoved;
    [SerializeField] private IntPairEventChannel _onItemMoved;
    [SerializeField] private IntPairEventChannel _onItemSwapped;

    #endregion

    #region Channels — View → Controller

    [Header("Channels entrants (View → Controller)")]
    [SerializeField] private IntPairEventChannel _onDropAttempted;
    [SerializeField] private IntEventChannel _onDropCancelled;

    #endregion

    #region State

    public InventoryModel Model { get; private set; }

    // Lambdas stockées pour pouvoir se désabonner avec -=
    private Action<int, ItemModel> _onItemPlacedHandler;
    private Action<int> _onItemRemovedHandler;
    private Action<int, int> _onItemMovedHandler;
    private Action<int, int> _onItemSwappedHandler;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        Model = new InventoryModel(_columns * _rows);
        ServiceLocator.Register(this);
        BindModelEvents();
    }

    private void Start()
    {
        foreach (ItemSO so in _startingItems)
            if (so != null) Model.Add(new ItemModel(so));
    }

    private void OnEnable() => _onDropAttempted?.Subscribe(HandleDropAttempted);
    private void OnDisable() => _onDropAttempted?.Unsubscribe(HandleDropAttempted);

    private void OnDestroy()
    {
        UnbindModelEvents();
        ServiceLocator.Unregister<InventoryController>();
    }

    #endregion

    #region Model Event Binding

    private void BindModelEvents()
    {
        _onItemPlacedHandler = (i, item) => _onItemPlaced?.Raise(new ItemPlacedPayload(i, item));
        _onItemRemovedHandler = i => _onItemRemoved?.Raise(i);
        _onItemMovedHandler = (f, t) => _onItemMoved?.Raise(new IntPairPayload(f, t));
        _onItemSwappedHandler = (a, b) => _onItemSwapped?.Raise(new IntPairPayload(a, b));

        Model.OnItemPlaced += _onItemPlacedHandler;
        Model.OnItemRemoved += _onItemRemovedHandler;
        Model.OnItemMoved += _onItemMovedHandler;
        Model.OnItemSwapped += _onItemSwappedHandler;
    }

    private void UnbindModelEvents()
    {
        Model.OnItemPlaced -= _onItemPlacedHandler;
        Model.OnItemRemoved -= _onItemRemovedHandler;
        Model.OnItemMoved -= _onItemMovedHandler;
        Model.OnItemSwapped -= _onItemSwappedHandler;
    }

    #endregion

    #region Handlers

    private void HandleDropAttempted(IntPairPayload payload)
    {
        if (!Model.Move(payload.A, payload.B))
            _onDropCancelled?.Raise(payload.A);
    }

    #endregion

    #region Public API

    public int Add(ItemSO so) => so != null ? Model.Add(new ItemModel(so)) : -1;

    public bool Remove(int index) => Model.Remove(index);

    public ItemModel Get(int index) => Model.Get(index);

    #endregion
}