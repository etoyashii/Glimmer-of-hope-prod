using System.Collections;
using UnityEngine;

public class BlendShapeController : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer skinnedMesh;
    [SerializeField] private int blendShapeIndex = 0;
    [SerializeField] private float duration = 1f;

    private Coroutine blendShapeCoroutine;

    public void PlayBlendShape()
    {
        if (blendShapeCoroutine != null)
            StopCoroutine(blendShapeCoroutine);

        blendShapeCoroutine = StartCoroutine(BlendShapeRoutine());
    }

    private IEnumerator BlendShapeRoutine()
    {
        float startValue = skinnedMesh.GetBlendShapeWeight(blendShapeIndex);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            t = Mathf.SmoothStep(0f, 1f, t);

            float value = Mathf.Lerp(startValue, 100f, t);

            skinnedMesh.SetBlendShapeWeight(blendShapeIndex, value);

            yield return null;
        }

        skinnedMesh.SetBlendShapeWeight(blendShapeIndex, 100f);
        blendShapeCoroutine = null;
    }
}