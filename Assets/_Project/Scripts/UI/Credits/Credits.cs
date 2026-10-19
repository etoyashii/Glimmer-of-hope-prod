using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

namespace GlimmerOfHope.UI
{

    public class Credits : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform creditsContent = null;
        [SerializeField] private Button exitButton;
        [Header("Animation")]
        [SerializeField] private float creditsAnimSpeed = 10f;
        [SerializeField] private float loadMainSceneWaitingTime = 3f;
        [Header("Settings")]
        [SerializeField] private UnityEvent onCreditsEndEvent;

        private void Start()
        {
            exitButton.onClick.AddListener(CreditsEnd);
            
            StartCoroutine(CreditsAnimationCoroutine());
        }

        private IEnumerator CreditsAnimationCoroutine()
        {
            while (creditsContent.anchoredPosition.y < 0)
            {
                creditsContent.anchoredPosition = new Vector2(creditsContent.anchoredPosition.x, creditsContent.anchoredPosition.y + creditsAnimSpeed * Time.deltaTime);
                yield return 0;
            }

            yield return new WaitForSeconds(loadMainSceneWaitingTime);
            CreditsEnd();
        }

        private void CreditsEnd()
        {
            onCreditsEndEvent?.Invoke();
        }


    }
}