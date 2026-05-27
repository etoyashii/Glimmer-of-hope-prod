using GlimmerOfHope.Editor;
using UnityEngine;

public class TestComponent : MonoBehaviour
{
    // Slider
    [Header("--Slider--")]

    [Slider(0f, 100f)]
    public float health = 75f;

    [Slider(-1f, 1f)]
    public float balance = 0f;

    [Slider(0, 10)]
    public int lives = 3;

    // Texture Preview
    [Header("--Texture Preview--")]

    [TexturePreview]
    public Sprite testSprite;

    [TexturePreview]
    public Texture2D testTexture;

    // Audio Player
    [Header("--Audio Player--")]

    [AudioPlayer]
    public AudioClip testClip;
}