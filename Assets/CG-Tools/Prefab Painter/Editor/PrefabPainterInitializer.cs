using UnityEditor;

[InitializeOnLoad]
public class PrefabPainterInitializer
{
    static PrefabPainterInitializer()
    {
        PrefabPainterReadme.ShowWindow();
    }
}
