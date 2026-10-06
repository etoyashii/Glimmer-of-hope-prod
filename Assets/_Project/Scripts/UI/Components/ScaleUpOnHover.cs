using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace GlimmerOfHope.UI
{
    public class ScaleUpOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields
        [SerializeField] private float _targetScale = 1.05f;
        [SerializeField] private float _scaleUpTime = 0.2f;
        [SerializeField] private Ease _scaleUpEase = Ease.InCirc;
        [SerializeField] private float _scaleDownTime = 0.2f;
        [SerializeField] private Ease _scaleDownEase = Ease.InCirc;

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(_targetScale, _scaleUpTime).SetEase(_scaleUpEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(1f, _scaleDownTime).SetEase(_scaleDownEase);
        }
        #endregion
    }
}