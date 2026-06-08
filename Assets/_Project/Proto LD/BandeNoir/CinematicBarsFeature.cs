using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


/// <summary>
/// ScriptableRendererFeature to add the custom pass
/// Needed to add in pc-renderer and mobile-renderer
/// </summary>

public class CinematicBarsFeature : ScriptableRendererFeature
{
    #region Propreties
    [SerializeField] private Shader shader;
    private Material material;
    private CinematicBarsPass pass;
    #endregion

    #region Methods

    public override void Create()
    {
        if (shader == null) return;
        material = CoreUtils.CreateEngineMaterial(shader);
        pass = new CinematicBarsPass(material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var stack = VolumeManager.instance.stack;
        var effect = stack.GetComponent<CinematicBarsEffect>();
        if (effect != null && effect.IsActive())
            renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
    }
    #endregion
}