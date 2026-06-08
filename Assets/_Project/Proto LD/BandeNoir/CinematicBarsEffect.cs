using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

[Serializable, VolumeComponentMenu("Custom/Cinematic Bars")]
public class CinematicBarsEffect : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Taille des bandes (0 = aucune, 0.15 = cinéma 2.39:1)")]
    public ClampedFloatParameter barSize = new ClampedFloatParameter(0f, 0f, 0.5f);

    public bool IsActive() => barSize.value > 0f;
    public bool IsTileCompatible() => false;
}