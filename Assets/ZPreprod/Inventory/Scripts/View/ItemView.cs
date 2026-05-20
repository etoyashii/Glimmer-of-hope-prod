using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Vue d'un item draggable.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ItemView : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerClickHandler
{
    #region Configuration

    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;

    [Header("Channels sortants")]
    [SerializeField] private IntEventChannel _onDropCancelled;
    [SerializeField] private IntEventChannel _onItemClicked;

    #endregion

    #region State

    public int SlotIndex { get; private set; }

    public bool IsDragging { get; private set; }

    private bool _dropAccepted;

    private Canvas _canvas;
    private CanvasGroup _group;
    private Transform _originParent;
    private Vector3 _originLocalPos;
    private int _originSiblingIndex;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _group = GetComponent<CanvasGroup>();
    }

    #endregion

    #region Init

    public void Initialize(ItemModel item, int slotIndex)
    {
        SlotIndex = slotIndex;

        if (_iconImage)
        {
            _iconImage.sprite = item?.Icon;
            _iconImage.enabled = item?.Icon != null;
        }

        if (_nameText)
            _nameText.text = item?.Name ?? string.Empty;
    }

    public void AttachToSlot(Transform parent, int newIndex)
    {
        SlotIndex = newIndex;
        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
    }

    public void ReturnToOrigin()
    {
        transform.SetParent(_originParent);
        transform.SetSiblingIndex(_originSiblingIndex);
        transform.localPosition = _originLocalPos;
    }

    public void SetDropAccepted() => _dropAccepted = true;

    #endregion

    #region Drag Handlers

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dropAccepted = false;
        IsDragging = true;

        _originParent = transform.parent;
        _originLocalPos = transform.localPosition;
        _originSiblingIndex = transform.GetSiblingIndex();

        transform.SetParent(_canvas.transform);
        transform.SetAsLastSibling();

        _group.blocksRaycasts = false;
        _group.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position += (Vector3)(eventData.delta / _canvas.scaleFactor);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (this == null) return;

        IsDragging = false;
        _group.blocksRaycasts = true;
        _group.alpha = 1f;

        if (!_dropAccepted && transform.parent == _canvas.transform)
        {
            _onDropCancelled?.Raise(SlotIndex);
            ReturnToOrigin();
        }
    }

    #endregion

    #region Click Handler 

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;

        _onItemClicked?.Raise(SlotIndex);
        print($"Item clicked in slot {SlotIndex}");
    }

    #endregion
}