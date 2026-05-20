using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue
{
    public static class DialogueSceneSetup
    {
        private const string PREFABS_PATH = "Assets/_Project/Prefabs/UI/Dialogue";
        private const string DIALOGUE_PATH = "Assets/_Project/Data/Dialogue";

        [MenuItem("Glimmer/Dialogue/Setup Current Scene")]
        public static void SetupCurrentScene()
        {
            // Add DialogueBootstrapper (required for services)
            var bootstrapperGO = GameObject.Find("DialogueBootstrapper");
            if (bootstrapperGO == null)
            {
                bootstrapperGO = new GameObject("DialogueBootstrapper");
                bootstrapperGO.AddComponent<DialogueBootstrapper>();
                Undo.RegisterCreatedObjectUndo(bootstrapperGO, "Create DialogueBootstrapper");
            }

            // Find or create Canvas
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            }

            // Add DialoguePanel prefab
            var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS_PATH + "/DialoguePanel.prefab");
            if (panelPrefab != null)
            {
                var existing = canvas.transform.Find("DialoguePanel");
                if (existing == null)
                {
                    var panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvas.transform);
                    panel.name = "DialoguePanel";
                    Undo.RegisterCreatedObjectUndo(panel, "Create DialoguePanel");
                }
            }
            else
            {
                Debug.LogWarning("DialoguePanel prefab not found! Run Setup Wizard first.");
            }

            // Add test trigger
            var triggerGO = GameObject.Find("DialogueTestTrigger");
            if (triggerGO == null)
            {
                triggerGO = new GameObject("DialogueTestTrigger");
                var trigger = triggerGO.AddComponent<DialogueTestTrigger>();

                var testConv = AssetDatabase.LoadAssetAtPath<ConversationSO>(
                    DIALOGUE_PATH + "/Conversations/TestConversation.asset");

                if (testConv != null)
                {
                    SerializedObject so = new SerializedObject(trigger);
                    so.FindProperty("_testConversation").objectReferenceValue = testConv;
                    so.ApplyModifiedProperties();
                }

                Undo.RegisterCreatedObjectUndo(triggerGO, "Create DialogueTestTrigger");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[DialogueSetup] Scene setup complete! Press T to test dialogue.");
        }
    }
}
