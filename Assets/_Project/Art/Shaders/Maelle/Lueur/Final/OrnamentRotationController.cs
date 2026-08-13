using UnityEngine;

public class OrnamentRotationController : MonoBehaviour
{
    [Header("Vitesses (deg/s)")]
    public float speedX = 0f;
    public float speedY = 30f;
    public float speedZ = 0f;

    [Header("Phase aleatoire (comme le toggle _RandomizePerOrnament)")]
    public bool randomizePhase = true;

    [Header("Cible")]
    public Material[] targetMaterials;

    public bool includeSelf = true;

    static readonly int RotationSpeedX = Shader.PropertyToID("_RotationSpeedX");
    static readonly int RotationSpeedY = Shader.PropertyToID("_RotationSpeedY");
    static readonly int RotationSpeedZ = Shader.PropertyToID("_RotationSpeedZ");
    static readonly int RandomizePerOrnament = Shader.PropertyToID("_RandomizePerOrnament");

    struct Target
    {
        public Renderer renderer;
        public int materialIndex;
    }

    Target[] _targets;
    MaterialPropertyBlock _block;

    void Awake()
    {
        CacheRenderers();
        _block = new MaterialPropertyBlock();
    }

    void CacheRenderers()
    {
        var allRenderers = GetComponentsInChildren<Renderer>(true);
        var found = new System.Collections.Generic.List<Target>();

        foreach (var r in allRenderers)
        {
            if (!includeSelf && r.gameObject == gameObject) continue;

            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                bool matches = targetMaterials == null || targetMaterials.Length == 0;
                if (!matches)
                {
                    for (int j = 0; j < targetMaterials.Length; j++)
                    {
                        if (mats[i] == targetMaterials[j]) { matches = true; break; }
                    }
                }

                if (matches)
                {
                    found.Add(new Target { renderer = r, materialIndex = i });
                }
            }
        }

        _targets = found.ToArray();
    }

    void OnEnable()
    {
        if (_targets == null) CacheRenderers();
        if (_block == null) _block = new MaterialPropertyBlock();
        Apply();
    }

    public void Apply()
    {
        if (_targets == null) return;

        foreach (var t in _targets)
        {
            if (t.renderer == null) continue;

            t.renderer.GetPropertyBlock(_block, t.materialIndex);
            _block.SetFloat(RotationSpeedX, speedX);
            _block.SetFloat(RotationSpeedY, speedY);
            _block.SetFloat(RotationSpeedZ, speedZ);
            _block.SetFloat(RandomizePerOrnament, randomizePhase ? 1f : 0f);
            t.renderer.SetPropertyBlock(_block, t.materialIndex);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        CacheRenderers();
        if (_block == null) _block = new MaterialPropertyBlock();
        if (isActiveAndEnabled) Apply();
    }
#endif
}