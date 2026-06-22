using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

/// <summary>
/// Component in the volume, this must be modify in runtime
/// </summary>
[Serializable, VolumeComponentMenu("Custom/Cinematic Bars")]
public class CinematicBarsEffect : VolumeComponent, IPostProcessComponent
{
    #region Inspector propreties
    [Tooltip("Size (0 = none, 0.15 = cine)")]
    public ClampedFloatParameter barSize = new ClampedFloatParameter(0f, 0f, 0.5f);
    [Tooltip("Degrade bande noir")]
    public ClampedFloatParameter offset = new ClampedFloatParameter(0f, 0f, 0.2f);

    public bool IsActive() => barSize.value > 0f;
    public bool IsTileCompatible() => false;
    #endregion
}