using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.UI.MainMenu
{
    public class LogoBreathAnimation : MonoBehaviour
    {
        [SerializeField] private Image breathTarget;
        [SerializeField] private AnimationCurve breathCurve;
        [SerializeField] private float breathTime;
        
        private void Awake()
        {
            StartCoroutine(Breath());
        }

        private IEnumerator Breath()
        {
            float t = 0f;
            Color color = Color.white;

            while(enabled)
            {
                yield return 0;
                t += Time.deltaTime;
                if (t > breathTime)
                    t = 0f;
                color.a = breathCurve.Evaluate(t / breathTime);
                breathTarget.color = color;
            }
        }
    }
}
