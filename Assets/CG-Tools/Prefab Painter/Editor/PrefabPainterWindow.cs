using UnityEditor;
using UnityEngine;

public class PrefabPainterWindow : EditorWindow
{
    private GameObject[] prefabs = new GameObject[3];
    private float brushSize = 1.0f;
    private int quantity = 1;
    private Vector3 rotation = Vector3.zero;
    private Vector3 scale = Vector3.one;

    private enum Mode { Paint, Erase }
    private Mode currentMode = Mode.Paint;

    private const string PrefabsKey = "PrefabPainterWindow_Prefabs";
    private const string BrushSizeKey = "PrefabPainterWindow_BrushSize";
    private const string QuantityKey = "PrefabPainterWindow_Quantity";
    private const string RotationKey = "PrefabPainterWindow_Rotation";
    private const string ScaleKey = "PrefabPainterWindow_Scale";

    private string[] prefabTags = new string[3];
    private Texture2D windowIcon;

    [MenuItem("Tools/CG-Tools/Prefab Painter-Free")]
    public static void ShowWindow()
    {
        PrefabPainterWindow window = GetWindow<PrefabPainterWindow>("Prefab Painter");
        window.SetIcon();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        LoadSettings();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SaveSettings();
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Painter", EditorStyles.boldLabel);

        for (int i = 0; i < prefabs.Length; i++)
        {
            EditorGUI.BeginChangeCheck();
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField($"Prefab {i + 1}", prefabs[i], typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                // Generar una nueva etiqueta única cuando el prefab cambia
                prefabTags[i] = prefabs[i] != null ? System.Guid.NewGuid().ToString() : null;
            }
        }

        EditorGUILayout.Space();

        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.1f, 100f);
        quantity = EditorGUILayout.IntSlider("Quantity", quantity, 1, 100);

        EditorGUILayout.Space();

        GUILayout.Label("Rotation", EditorStyles.boldLabel);
        rotation = EditorGUILayout.Vector3Field("", rotation);

        EditorGUILayout.Space();

        GUILayout.Label("Scale", EditorStyles.boldLabel);
        scale = EditorGUILayout.Vector3Field("", scale);

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Paint Mode"))
        {
            currentMode = Mode.Paint;
        }
        if (GUILayout.Button("Erase Mode"))
        {
            currentMode = Mode.Erase;
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("Execute Action"))
        {
            ExecuteCurrentAction();
        }
    }

    private void ExecuteCurrentAction()
    {
        if (currentMode == Mode.Paint)
        {
            PaintPrefabs();
        }
        else if (currentMode == Mode.Erase)
        {
            ErasePrefabs();
        }
    }

    private void PaintPrefabs()
    {
        if (prefabs.Length == 0 || prefabs[0] == null)
        {
            Debug.LogError("Please assign at least one prefab.");
            return;
        }

        for (int i = 0; i < quantity; i++)
        {
            Vector3? position = GetValidRandomPosition();
            if (position.HasValue)
            {
                GameObject selectedPrefab = GetRandomPrefab(out string prefabTag);
                if (selectedPrefab != null)
                {
                    if (!IsPositionOccupied(position.Value))
                    {
                        GameObject instantiatedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                        instantiatedPrefab.transform.position = position.Value;
                        instantiatedPrefab.transform.rotation = GetSurfaceRotation(position.Value);
                        instantiatedPrefab.transform.localScale = scale;
                        PrefabEraserTag eraserTag = instantiatedPrefab.AddComponent<PrefabEraserTag>(); // Añadir el componente PrefabEraserTag
                        eraserTag.prefabTag = prefabTag;

                        // Asegurarse de que el prefab tenga un collider
                        if (instantiatedPrefab.GetComponent<Collider>() == null)
                        {
                            instantiatedPrefab.AddComponent<BoxCollider>();
                        }

                        // Registrar la acción de creación en la Undo History
                        Undo.RegisterCreatedObjectUndo(instantiatedPrefab, "Paint Prefab");

                        Debug.Log($"Instantiated prefab at {position} with tag added."); // Mensaje de depuración
                    }
                }
            }
        }
    }

    private bool IsPositionOccupied(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, brushSize * 0.5f); // Reducido a la mitad del tamaño del pincel
        foreach (var collider in colliders)
        {
            if (collider.gameObject.GetComponent<PrefabEraserTag>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private void ErasePrefabs()
    {
        Vector3 mousePosition = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log($"Hit point: {hit.point}"); // Mensaje de depuración
            Collider[] hitColliders = Physics.OverlapSphere(hit.point, brushSize);
            Debug.Log($"Number of colliders detected: {hitColliders.Length}"); // Mensaje de depuración

            foreach (var hitCollider in hitColliders)
            {
                Debug.Log($"Checking collider: {hitCollider.gameObject.name}"); // Mensaje de depuración
                PrefabEraserTag tag = hitCollider.gameObject.GetComponent<PrefabEraserTag>();
                if (tag != null && IsPrefabTagAssigned(tag.prefabTag))
                {
                    Debug.Log($"Erasing prefab at {hitCollider.gameObject.transform.position}"); // Mensaje de depuración
                    Undo.DestroyObjectImmediate(hitCollider.gameObject);
                }
            }
        }
    }

    private GameObject GetRandomPrefab(out string prefabTag)
    {
        int nonNullPrefabCount = 0;
        foreach (var prefab in prefabs)
        {
            if (prefab != null)
                nonNullPrefabCount++;
        }

        if (nonNullPrefabCount == 0)
        {
            prefabTag = null;
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, nonNullPrefabCount);
        int currentIndex = 0;

        foreach (var prefab in prefabs)
        {
            if (prefab != null)
            {
                if (currentIndex == randomIndex)
                {
                    prefabTag = prefabTags[currentIndex];
                    return prefab;
                }
                currentIndex++;
            }
        }

        prefabTag = null;
        return null;
    }

    private Vector3? GetValidRandomPosition()
    {
        Vector3 mousePosition = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            for (int i = 0; i < 10; i++) // Intentar hasta 10 veces para encontrar una posición válida
            {
                Vector3 randomOffset = GetRandomShapeOffset();
                Vector3 potentialPosition = hit.point + randomOffset;
                if (Physics.Raycast(potentialPosition + Vector3.up * 10, Vector3.down, out RaycastHit surfaceHit))
                {
                    if (!IsPositionOccupied(surfaceHit.point))
                    {
                        return surfaceHit.point;
                    }
                }
            }
        }

        return null;
    }

    private Vector3 GetRandomShapeOffset()
    {
        float angle = Random.Range(0f, Mathf.PI * 2);
        float radius = Random.Range(0f, brushSize);
        return new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
    }

    private Quaternion GetSurfaceRotation(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 10, Vector3.down, out RaycastHit hit))
        {
            return Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
        return Quaternion.identity;
    }

    private bool IsPrefabTagAssigned(string tag)
    {
        foreach (var prefabTag in prefabTags)
        {
            if (prefabTag == tag)
            {
                return true;
            }
        }
        return false;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseMove)
        {
            if (e.button == 0)
            {
                if (currentMode == Mode.Paint && e.type != EventType.MouseMove)
                {
                    PaintPrefabs();
                }
                else if (currentMode == Mode.Erase && e.type != EventType.MouseMove)
                {
                    ErasePrefabs();
                }

                e.Use();
            }
        }

        Handles.color = currentMode == Mode.Erase ? new Color(1f, 0f, 0f, 0.4f) : new Color(0f, 1f, 0f, 0.4f);
        Vector3 mousePosition = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Handles.DrawSolidDisc(hit.point, Vector3.up, brushSize);
        }
    }

    private void SaveSettings()
    {
        // Guardar prefabs
        for (int i = 0; i < prefabs.Length; i++)
        {
            string prefabPath = prefabs[i] != null ? AssetDatabase.GetAssetPath(prefabs[i]) : null;
            EditorPrefs.SetString($"{PrefabsKey}_{i}", prefabPath);
            EditorPrefs.SetString($"{PrefabsKey}_Tag_{i}", prefabTags[i] ?? string.Empty);
        }

        // Guardar otros parámetros
        EditorPrefs.SetFloat(BrushSizeKey, brushSize);
        EditorPrefs.SetInt(QuantityKey, quantity);
        EditorPrefs.SetString(RotationKey, $"{rotation.x},{rotation.y},{rotation.z}");
        EditorPrefs.SetString(ScaleKey, $"{scale.x},{scale.y},{scale.z}");
    }

    private void LoadSettings()
    {
        // Cargar prefabs
        for (int i = 0; i < prefabs.Length; i++)
        {
            string prefabPath = EditorPrefs.GetString($"{PrefabsKey}_{i}", null);
            if (!string.IsNullOrEmpty(prefabPath))
            {
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            prefabTags[i] = EditorPrefs.GetString($"{PrefabsKey}_Tag_{i}", null);
        }

        // Cargar otros parámetros
        brushSize = EditorPrefs.GetFloat(BrushSizeKey, 1.0f);
        quantity = EditorPrefs.GetInt(QuantityKey, 1);

        string[] rotationValues = EditorPrefs.GetString(RotationKey, "0,0,0").Split(',');
        if (rotationValues.Length == 3)
        {
            rotation = new Vector3(
                float.Parse(rotationValues[0]),
                float.Parse(rotationValues[1]),
                float.Parse(rotationValues[2]));
        }

        string[] scaleValues = EditorPrefs.GetString(ScaleKey, "1,1,1").Split(',');
        if (scaleValues.Length == 3)
        {
            scale = new Vector3(
                float.Parse(scaleValues[0]),
                float.Parse(scaleValues[1]),
                float.Parse(scaleValues[2]));
        }
    }

    private void SetIcon()
    {
        string iconPath = "Assets/CG-Tools/Prefab Painter/Scripts/Images/ICO.jpeg";
        windowIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (windowIcon != null)
        {
            titleContent = new GUIContent("Prefab Painter", windowIcon);
        }
        else
        {
            Debug.LogWarning("Icon not found at " + iconPath);
        }
    }
}
