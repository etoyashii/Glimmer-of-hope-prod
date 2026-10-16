using UnityEditor;
using UnityEngine;

namespace Kebab.SceneFlow.Settings.Editors
{
    [InitializeOnLoad]
    public class SceneFlowInitializer
    {
        static SceneFlowInitializer()
        {
            EditorApplication.delayCall += Initialize;
        }


        [MenuItem("Tools/SceneFlow/Initialize")]
        private static void Initialize()
        {
            var settings = SceneFlowSettings.GetOrCreate();
        }

    }
}