using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tooltip singleton.
/// Écoute OnSlotHovered via SO channel, lit l'ItemModel via ServiceLocator.
/// </summary>
public class TooltipView : MonoBehaviour
{
    #region Singleton

    public static TooltipView Instance { get; private set; }

    #endregion

    #region Configuration

    [Header("UI")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Vector2 _offset = new(16f, -16f);

    [Header("Channels entrants")]
    [SerializeField] private IntEventChannel _onSlotHovered;
    [SerializeField] private VoidEventChannel _onSlotUnhovered;

    #endregion

    #region State

    private RectTransform _rectTransform;
    private Canvas _canvas;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        _onSlotHovered?.Subscribe(Show);
        _onSlotUnhovered?.Subscribe(Hide);
    }

    private void OnDisable()
    {
        _onSlotHovered?.Unsubscribe(Show);
        _onSlotUnhovered?.Unsubscribe(Hide);
    }

    private void Update()
    {
        if (gameObject.activeSelf)
            FollowMouse();
    }

    #endregion

    #region Display

    private void Show(int slotIndex)
    {
        InventoryController ctrl = ServiceLocator.Get<InventoryController>();
        ItemModel item = ctrl?.Get(slotIndex);

        if (item == null) { gameObject.SetActive(false); return; }

        Populate(item);
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Populate(ItemModel item)
    {
        if (_iconImage)
        {
            _iconImage.sprite = item.Icon;
            _iconImage.enabled = item.Icon != null;
        }

        if (_nameText) _nameText.text = item.Name;
        if (_descriptionText) _descriptionText.text = item.Description;
    }

    #endregion

    #region Mouse Follow

    private void FollowMouse()
    {
        if (_canvas == null || _rectTransform == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            Input.mousePosition,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out Vector2 localPoint
        );

        _rectTransform.anchoredPosition = localPoint + _offset;
    }

    #endregion
}