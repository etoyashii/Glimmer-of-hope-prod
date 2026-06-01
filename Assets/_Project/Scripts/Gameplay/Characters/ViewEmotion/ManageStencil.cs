using UnityEngine;

/// <summary>
/// Use to write the stencil if an object inside is visible with the emotion view.
/// </summary>
public class ManageStencil : MonoBehaviour
{
    #region Public Properties

    public Transform player;
    public GlobalValueShader globalShader;

    #endregion

    #region Private Properties

    private Material _mat;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        _mat = GetComponent<Renderer>().materials[1];
        
    }

    private void Update()
    {
        //Set the update variable for the shader
        _mat.SetVector("_PlayerPos", player.position);
        _mat.SetFloat("_IsActive", globalShader.viewEmotionIsActive ? 1f : 0f);
        _mat.SetFloat("_Radius", globalShader.radiusSmallCircle * globalShader.currentPropRadius);
    }

    #endregion
}
