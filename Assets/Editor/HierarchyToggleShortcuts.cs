#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
public static class HierarchyToggleShortcuts
{
    // Alt + H : masque/affiche dans la Scene
    [Shortcut("Hierarchy Toggle/Toggle Visibility (Eye)", KeyCode.H, ShortcutModifiers.Alt)]
    private static void ToggleVisibility()
    {
        GameObject[] selection = Selection.gameObjects;
        if (selection.Length == 0) return;

        SceneVisibilityManager vis = SceneVisibilityManager.instance;

        bool anyVisible = false;
        foreach (GameObject go in selection)
        {
            if (!vis.IsHidden(go))
            {
                anyVisible = true;
                break;
            }
        }

        if (anyVisible) vis.Hide(selection, true);
        else vis.Show(selection, true);
    }

    // Alt + G : active/désactive
    [Shortcut("Hierarchy Toggle/Toggle Active", KeyCode.G, ShortcutModifiers.Alt)]
    private static void ToggleActive()
    {
        GameObject[] selection = Selection.gameObjects;
        if (selection.Length == 0) return;

        bool anyActive = false;
        foreach (GameObject go in selection)
        {
            if (go.activeSelf)
            {
                anyActive = true;
                break;
            }
        }

        Undo.RecordObjects(selection, "Toggle Active");
        foreach (GameObject go in selection)
        {
            go.SetActive(!anyActive);
            EditorUtility.SetDirty(go);
        }
    }
}
#endif