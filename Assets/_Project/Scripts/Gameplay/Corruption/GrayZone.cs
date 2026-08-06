using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Project a mask (from and array in the manager) and return the data of positions etc
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Collider))]
    public class GrayZone : MonoBehaviour
    {
        public Vector2 size = new Vector2(10, 10);
        [Range(0, 1)]
        public float threshold = 0.5f;
        [Range(0.1f, 2f)]
        public float contrast = 0.5f;
        public int maskIndex;

        [Header("Repeinte")]
        public Vector3 worldPoint;
        private Vector2 paintOrigin;
        private float paintRadius;

        public event Action<Vector3, float> OnPaintStep;
        public event Action<Vector3> OnPaintComplete;

        [SerializeField]
        private Material sharedMat;
        private float defaultFade = 50;

        private void OnEnable()
        {
            GrayZoneManager.Register(this);
        }

        private void OnDisable()
        {
            GrayZoneManager.Unregister(this);
        }

        private void OnValidate()
        {
            GrayZoneManager.MarkDirty();
        }

#if UNITY_EDITOR
        // update editor
        private void Update()
        {
            if (!Application.isPlaying && transform.hasChanged)
            {
                GrayZoneManager.MarkDirty();
                transform.hasChanged = false;
            }
        }

#endif

        public GrayZoneData GetData()
        {
            GrayZoneData data = new GrayZoneData();
            data.worldToLocal = transform.worldToLocalMatrix;
            data.size = size;
            data.threshold = threshold;
            data.contrast = contrast;
            data.maskIndex = maskIndex;
            data.paintOrigin = paintOrigin;
            data.paintRadius = paintRadius;
            return data;
        }

        public void Repaint()
        {
            PaintFromPoint(worldPoint, 4f, 5f);
        }

        //animation repaint
        public async Task PaintFromPoint(Vector3 worldPoint, float targetRadius, float duration = 1f)
        {

            Destroy(this.transform.GetChild(0).gameObject);
            StartCoroutine(PaintRoutine(worldPoint, targetRadius, duration));
        }

        private IEnumerator PaintRoutine(Vector3 worldPoint, float targetRadius, float duration)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            paintOrigin = new Vector2(local.x, local.z);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                paintRadius = Mathf.Lerp(0f, targetRadius, progress);

                GrayZoneManager.MarkDirty();
                OnPaintStep?.Invoke(worldPoint, paintRadius);
                sharedMat.SetFloat("_TopFadeAmount", Mathf.Lerp(defaultFade, 0f, progress));

                yield return null;
            }

            paintRadius = targetRadius;
            GrayZoneManager.MarkDirty();
            OnPaintComplete?.Invoke(worldPoint);
            sharedMat.SetFloat("_TopFadeAmount", 50);
            Destroy(this.gameObject);
        }
    }
}