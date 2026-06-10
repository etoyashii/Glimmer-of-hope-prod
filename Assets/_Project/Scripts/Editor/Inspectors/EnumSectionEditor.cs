using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// Active logique de section enum si dans script.
    /// 
    /// Si le script possède déjà un [CustomEditor] spécifique,
    /// appeler EnumSectionUtils.DrawWithSections(serializedObject)
    /// depuis son OnInspectorGUI() pour activer les sections.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class EnumSectionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI ()
        {
            if (EnumSectionUtils.HasAnySections(serializedObject))
                EnumSectionUtils.DrawWithSections(serializedObject);
            else
                DrawDefaultInspector();
        }
    }
}
