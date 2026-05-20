using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panneau de description d'item 
/// </summary>
public class ItemDescriptionView : MonoBehaviour
{
    #region Singleton

    public static ItemDescriptionView Instance { get; private set; }

    #endregion

    #region Configuration

    [Header("UI")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Header("Bouton de fermeture")]
    [SerializeField] private UnityEngine.UI.Button _closeButton;

    [Header("Channels entrants")]
    [SerializeField] private IntEventChannel _onItemClicked;

    #endregion

    #region State

    private int _currentSlot = -1;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
            return;
        Instance = this;
        gameObject.SetActive(false);
        _onItemClicked?.Subscribe(OnItemClicked);


    }

    private void OnEnable()
    {
        _closeButton?.onClick.AddListener(Hide);

    }

    private void OnDisable()
    {
        _closeButton?.onClick.RemoveListener(Hide);
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        _onItemClicked?.Unsubscribe(OnItemClicked);

    }

    #endregion

    #region Handlers

    private void OnItemClicked(int slotIndex)
    {
        if (gameObject.activeSelf && _currentSlot == slotIndex)
            Hide();
        else
            Show(slotIndex);
        print($"ItemDescriptionView: OnItemClicked({slotIndex})");
    }

    #endregion

    #region Display

    private void Show(int slotIndex)
    {
        InventoryController ctrl = ServiceLocator.Get<InventoryController>();
        ItemModel item = ctrl?.Get(slotIndex);

        if (item == null)
        {
            Hide();
            return;
        }

        _currentSlot = slotIndex;
        Populate(item);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        _currentSlot = -1;
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
}