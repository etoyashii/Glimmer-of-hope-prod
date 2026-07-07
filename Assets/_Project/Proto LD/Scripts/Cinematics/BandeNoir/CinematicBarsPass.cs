using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// Create a material from CinematicBarShader
/// Render the bars using the volume
/// If optimisation is needed cut the blit
/// </summary>
public class CinematicBarsPass : ScriptableRenderPass
{
    #region Propreties
    private Material material;
    private static readonly int BarSizeID = Shader.PropertyToID("_BarSize");
    private static readonly int OffsetID = Shader.PropertyToID("_Offset");
    #endregion

    #region Methods
    public CinematicBarsPass(Material mat)
    {
        material = mat;
        renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    private class PassData
    {
        public Material material;
        public TextureHandle source;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var stack = VolumeManager.instance.stack;
        var effect = stack.GetComponent<CinematicBarsEffect>();
        if (effect == null || !effect.IsActive()) return;

        var resourceData = frameData.Get<UniversalResourceData>();
        var source = resourceData.activeColorTexture;

        var descriptor = renderGraph.GetTextureDesc(source);
        descriptor.name = "CinematicBarsTmp";
        descriptor.clearBuffer = false;
        var destination = renderGraph.CreateTexture(descriptor);

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("CinematicBars", out var passData))
        {
            passData.material = material;
            passData.source = source;

            material.SetFloat(BarSizeID, effect.barSize.value);
            material.SetFloat(OffsetID, effect.offset.value);

            builder.UseTexture(source);
            builder.SetRenderAttachment(destination, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("CinematicBars_Copy", out var passData))
        {
            passData.source = destination;

            builder.UseTexture(destination);
            builder.SetRenderAttachment(source, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
            });
        }
    }
    #endregion
}