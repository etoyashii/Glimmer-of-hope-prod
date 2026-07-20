using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    [CreateAssetMenu(
        fileName = "GrayZoneAsset",
        menuName = "Gray Zone/Zone Asset")]
    public class GrayZoneAsset : ScriptableObject
    {
        public Texture2D mask;

        public Vector2 size = Vector2.one;

        [Range(0, 1)]
        public float threshold = 0.5f;
    }
}