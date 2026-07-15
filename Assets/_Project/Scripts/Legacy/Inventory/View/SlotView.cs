using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Vue d'un slot.
/// Publie les drops via SO channels.
/// </summary>
[RequireComponent(typeof(Image))]
public class SlotView : MonoBehaviour, IDropHandler
{
    #region Configuration

    [Header("Visuels")]
    [SerializeField] private Image _backgroundImage;

    [Header("Couleurs")]
    [SerializeField] private Color _colorEmpty = new Color(0.15f, 0.15f, 0.20f, 0.90f);
    [SerializeField] private Color _colorOccupied = new Color(0.12f, 0.12f, 0.16f, 0.90f);

    [Header("Channels sortants")]
    [SerializeField] private IntPairEventChannel _onDropAttempted;

    #endregion

    #region State

    public int SlotIndex { get; private set; }

    #endregion

    #region Init

    public void Initialize(int index)
    {
        SlotIndex = index;
        SetOccupied(false);
    }

    public void SetOccupied(bool occupied)
    {
        if (_backgroundImage)
            _backgroundImage.color = occupied ? _colorOccupied : _colorEmpty;
    }

    #endregion

    #region IDropHandler

    public void OnDrop(PointerEventData eventData)
    {
        ItemView dragged = eventData.pointerDrag?.GetComponent<ItemView>();
        if (dragged == null) return;

        dragged.SetDropAccepted();

        _onDropAttempted?.Raise(new IntPairPayload(dragged.SlotIndex, SlotIndex));
    }

    #endregion
}