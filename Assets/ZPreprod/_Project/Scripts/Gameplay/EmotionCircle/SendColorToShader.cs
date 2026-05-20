using UnityEngine;

namespace GlimmerOfHope.Gameplay.Emotion
{
    /// <summary>
    /// Use to send the default color of a mesh for the emotion shaders
    /// </summary>
    public class SendColorToShader : MonoBehaviour
    {
        #region Public Properties

        public Color color = Color.white;
        public bool isTransparent = false;

        #endregion

        #region Unity Lifecycle
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Material mat = GetComponent<Renderer>().material;
            mat.SetColor("_ClassiqueColor",color);

            if (isTransparent)
                mat.EnableKeyword("_CANBETRANSPARENT");
        }

        #endregion
    }
}
