using GlimmerOfHope.Editor;
using UnityEngine;

public class TestComponent : MonoBehaviour
{
    // Slider
    [Header("--Slider--")]

    [Slider(0f, 100f, SliderColor.Red)]
    public int health = 75;

    [Slider(-1f, 1f, SliderColor.Yellow)]
    public float balance = 0f;

    [Slider(0, 10)]
    public int lives = 3;

    // Texture Preview
    [Header("--Texture Preview--")]

    [TexturePreview]
    public Sprite testSprite;

    [TexturePreview]
    public Texture2D testTexture;

    [TexturePreview]
    public Material testMaterial;

    // Audio Player
    [Header("--Audio Player--")]

    [AudioPlayer]
    public AudioClip testClip;
}