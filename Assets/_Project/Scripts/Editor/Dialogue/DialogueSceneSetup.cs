using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue
{
    public static class DialogueSceneSetup
    {
        private const string PREFABS_PATH = "Assets/_Project/Prefabs/UI/Dialogue";
        private const string DIALOGUE_PATH = "Assets/_Project/Data/Dialogue";
        private const string EVENTS_PATH = "Assets/_Project/Data/Events/Dialogue";

        [MenuItem("Glimmer/Dialogue/Setup Current Scene")]
        public static void SetupCurrentScene()
        {
            var bootstrapper = EnsureBootstrapper();
            WireActionChannels(bootstrapper);

            var canvas = EnsureCanvas();
            EnsureDialoguePanel(canvas);
            EnsureTestTrigger();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[DialogueSetup] Scene setup complete! Press T to test dialogue.");
        }

        private static DialogueBootstrapper EnsureBootstrapper()
        {
            var go = GameObject.Find("DialogueBootstrapper");
            if (go == null)
            {
                go = new GameObject("DialogueBootstrapper");
                go.AddComponent<DialogueBootstrapper>();
                Undo.RegisterCreatedObjectUndo(go, "Create DialogueBootstrapper");
            }

            return go.GetComponent<DialogueBootstrapper>();
        }

        private static void WireActionChannels(DialogueBootstrapper bootstrapper)
        {
            if (bootstrapper == null) return;

            var so = new SerializedObject(bootstrapper);
            AssignChannel(so, "_onDialogueEvent", "OnDialogueEvent");
            AssignChannel(so, "_onCharacterShow", "OnCharacterShow");
            AssignChannel(so, "_onCharacterHide", "OnCharacterHide");
            so.ApplyModifiedProperties();
        }

        private static void AssignChannel(SerializedObject so, string fieldName, string assetName)
        {
            var property = so.FindProperty(fieldName);
            if (property == null) return;

            var channel = AssetDatabase.LoadAssetAtPath<StringEventChannel>($"{EVENTS_PATH}/{assetName}.asset");
            if (channel == null)
            {
                Debug.LogWarning($"[DialogueSetup] {assetName}.asset not found in {EVENTS_PATH}. " +
                                 "Create it via Create > Glimmer > Events > String Event.");
                return;
            }

            if (property.objectReferenceValue == channel) return;

            property.objectReferenceValue = channel;
            Debug.Log($"[DialogueSetup] Wired {fieldName} to {assetName}.asset");
        }

        private static Canvas EnsureCanvas()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null) return canvas;

            var canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");

            return canvas;
        }

        private static void EnsureDialoguePanel(Canvas canvas)
        {
            if (canvas.transform.Find("DialoguePanel") != null) return;

            var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS_PATH + "/DialoguePanel.prefab");
            if (panelPrefab == null)
            {
                Debug.LogWarning($"[DialogueSetup] DialoguePanel.prefab not found in {PREFABS_PATH}.");
                return;
            }

            var panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvas.transform);
            panel.name = "DialoguePanel";
            Undo.RegisterCreatedObjectUndo(panel, "Create DialoguePanel");
        }

        private static void EnsureTestTrigger()
        {
            var go = GameObject.Find("DialogueTestTrigger");
            if (go == null)
            {
                go = new GameObject("DialogueTestTrigger");
                go.AddComponent<DialogueTestTrigger>();
                Undo.RegisterCreatedObjectUndo(go, "Create DialogueTestTrigger");
            }

            var trigger = go.GetComponent<DialogueTestTrigger>();
            if (trigger == null) return;

            var so = new SerializedObject(trigger);
            var property = so.FindProperty("_testConversation");
            if (property == null || property.objectReferenceValue != null) return;

            var conversation = FindSampleConversation();
            if (conversation == null)
            {
                Debug.LogWarning("[DialogueSetup] No usable ConversationSO found. Drag one into " +
                                 "DialogueTestTrigger._testConversation for the T key to work.");
                return;
            }

            property.objectReferenceValue = conversation;
            so.ApplyModifiedProperties();
            Debug.Log($"[DialogueSetup] Test conversation set to {conversation.name}. Press T to play it.");
        }

        private static ConversationSO FindSampleConversation()
        {
            var named = AssetDatabase.LoadAssetAtPath<ConversationSO>(
                DIALOGUE_PATH + "/Conversations/TestConversation.asset");

            if (named != null) return named;

            var guids = AssetDatabase.FindAssets("t:ConversationSO", new[] { DIALOGUE_PATH });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var conversation = AssetDatabase.LoadAssetAtPath<ConversationSO>(path);
                if (conversation != null && conversation.StartLine != null) return conversation;
            }

            return null;
        }
    }
}
