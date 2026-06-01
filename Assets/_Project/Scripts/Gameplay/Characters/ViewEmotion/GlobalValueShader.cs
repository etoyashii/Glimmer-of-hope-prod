using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Use once in a scene to send the variable to the shader graph
/// </summary>
public class GlobalValueShader : MonoBehaviour
{
    #region Public Properties

    public Transform playerTransform;
    public float radiusBigCircle = 20;
    public Color bigCircleColor = Color.aliceBlue;
    public float radiusSmallCircle = 5;
    public Color smallCircleColor = Color.red;
    public bool viewEmotionIsActive = true;
    public float currentPropRadius = 0;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        //Set the unchanging variable of the shader
        Shader.SetGlobalFloat("_RadiusBigCircle", radiusBigCircle);
        Shader.SetGlobalFloat("_RadiusSmallCircle", radiusSmallCircle);
        Shader.SetGlobalColor("_BigCircleColor", bigCircleColor);
        Shader.SetGlobalColor("_SmallCircleColor", smallCircleColor);

        SwitchViewEmotionMode();
    }

    private void Update()
    {
        //set the update variable of the shader
        Shader.SetGlobalVector("_CenterCircle", playerTransform.position);

        if (Keyboard.current.tabKey.wasPressedThisFrame) //need to be change for mobile, with a button or something else
        {
            SwitchViewEmotionMode();
        }

        if (viewEmotionIsActive)
        {
            if (currentPropRadius != 1f) //if the circle isnt full size
            {
                currentPropRadius += Time.deltaTime;

                if (currentPropRadius > 1f)
                    currentPropRadius = 1f;

                Shader.SetGlobalFloat("_Pourcentage", currentPropRadius);
            }
        }
        else
        {
            if (currentPropRadius != 0f) //if the circle is still visible
            {
                currentPropRadius -= Time.deltaTime;

                if (currentPropRadius < 0f)
                {
                    currentPropRadius = 0f;
                    Shader.SetGlobalFloat("_IsActive", 0f);
                }                    

                Shader.SetGlobalFloat("_Pourcentage", currentPropRadius);
            }
        }
    }

    #endregion

    #region Public Methods

    public void SwitchViewEmotionMode()
    {
        viewEmotionIsActive = !viewEmotionIsActive;

        if (viewEmotionIsActive)
            Shader.SetGlobalFloat("_IsActive", 1f);
    }

    #endregion
}
