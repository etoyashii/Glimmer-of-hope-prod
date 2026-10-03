using UnityEditor;
using UnityEngine;

public class PrefabPainterReadme : EditorWindow
{
    private Vector2 scrollPos;
    private Texture2D bannerTexture;
    public static void ShowWindow()
    {
        GetWindow<PrefabPainterReadme>("Prefab Painter Readme");
    }

    private void OnEnable()
    {
        // Load the banner image
        bannerTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/CG-Tools/Prefab Painter/Scripts/Images/Banner.jpg", typeof(Texture2D));
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Display the banner image
        if (bannerTexture != null)
        {
            float aspectRatio = (float)bannerTexture.height / bannerTexture.width;
            Rect bannerRect = GUILayoutUtility.GetRect(position.width, position.width * aspectRatio);
            GUI.DrawTexture(bannerRect, bannerTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            GUILayout.Label("Banner image not found.", EditorStyles.boldLabel);
        }

        GUILayout.Space(10);

        GUILayout.Label("Prefab Painter Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("Overview / Resumen", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "English: \n" +
            "Prefab Painter Tool is a Unity editor extension that allows you to paint prefabs onto your scenes easily.\n" +
            "\n" +
            "Español: \n" +
            "La herramienta Prefab Painter es una extensión del editor de Unity que te permite pintar prefabs en tus escenas fácilmente.",
            MessageType.Info
        );

        GUILayout.Space(20);
        GUILayout.Label("Features / Características", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "English: \n" +
            "- Paint multiple prefabs simultaneously.\n" +
            "- Erase painted prefabs.\n" +
            "- Customizable brush size and quantity.\n" +
            "- Supports undo functionality.\n" +
            "- Prefab alignment with terrain and irregular surfaces.\n" +
            "- Save and load brush settings.\n" +
            "\n" +
            "Español: \n" +
            "- Pinta varios prefabs simultáneamente.\n" +
            "- Borra prefabs pintados.\n" +
            "- Tamaño y cantidad de pincel personalizables.\n" +
            "- Soporta la funcionalidad de deshacer.\n" +
            "- Alineación de prefabs con terreno y superficies irregulares.\n" +
            "- Guardar y cargar configuraciones de pincel.",
            MessageType.None
        );

        GUILayout.Space(20);
        GUILayout.Label("Usage / Uso", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "English: \n" +
            "1. Open the Prefab Painter window from `Tools > CG-Tools > Prefab Painter`.\n" +
            "2. Assign up to 3 prefabs in the Prefab Painter window.\n" +
            "3. Customize the brush settings: size, quantity, rotation, and scale.\n" +
            "4. Select `Paint Mode` or `Erase Mode`.\n" +
            "5. Click or drag in the scene view to paint or erase prefabs.\n" +
            "\n" +
            "Español: \n" +
            "1. Abre la ventana Prefab Painter desde `Tools > CG-Tools > Prefab Painter`.\n" +
            "2. Asigna hasta 3 prefabs en la ventana de Prefab Painter.\n" +
            "3. Personaliza la configuración del pincel: tamaño, cantidad, rotación y escala.\n" +
            "4. Selecciona `Modo Pintar` o `Modo Borrar`.\n" +
            "5. Haz clic o arrastra en la vista de la escena para pintar o borrar prefabs.",
            MessageType.None
        );

        GUILayout.Space(20);
        GUILayout.Label("Limitations / Limitaciones", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "English: \n" +
            "This free version of Prefab Painter is limited to 3 prefabs. Upgrade to the Pro version for unlimited prefabs, advanced features, and enhanced performance.\n" +
            "\n" +
            "Español: \n" +
            "Esta versión gratuita de Prefab Painter está limitada a 3 prefabs. Actualiza a la versión Pro para tener prefabs ilimitados, características avanzadas y mejor rendimiento.",
            MessageType.Warning
        );

        GUILayout.Space(20);
        GUILayout.Label("Contact / Contacto", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "English: \n" +
            "For support and feedback, contact us at support@cubasgames.com.\n" +
            "\n" +
            "Español: \n" +
            "Para soporte y comentarios, contáctanos en support@cubasgames.com.",
            MessageType.None
        );

        GUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }
}
