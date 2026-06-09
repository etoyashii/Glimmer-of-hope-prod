using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WakeUpEffect : MonoBehaviour
{

    public Volume volume;
    public float wakeUpDuration = 2f;
    public CinemachineCamera camera;

    private DepthOfField dof;
    private CinematicBarsEffect cineBars;

    void Start()
    {
        volume.profile.TryGet(out dof);
        volume.profile.TryGet<CinematicBarsEffect>(out cineBars);
        cineBars.barSize.value =0.5f;
        StartCoroutine(WakeUp());
        camera.Priority = 0;
    }


    IEnumerator WakeUp()
    {
        float t = 0f;

        while (t < wakeUpDuration)
        {
            t += Time.deltaTime;
            float progress = t / wakeUpDuration;

            Debug.Log(dof.gaussianMaxRadius.value);

            dof.gaussianMaxRadius.value = Mathf.Lerp(7f, 0f, progress);
            cineBars.barSize.value = Mathf.Lerp(0.5f, 0.1f, progress);
            yield return null;
        }
    }
}