using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public static class DialogueLineAssetFactory
    {
        #region Public Methods

        public static DialogueLineSO Create(ConversationSO conversation, string lineId, int sequenceOrder)
        {
            var folder = ResolveFolder(conversation);
            if (folder == null)
                return null;

            var lineSO = ScriptableObject.CreateInstance<DialogueLineSO>();
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{lineId}.asset");
            AssetDatabase.CreateAsset(lineSO, path);

            InitializeFields(lineSO, conversation, lineId, sequenceOrder);
            EditorUtility.SetDirty(lineSO);
            AssetDatabase.SaveAssetIfDirty(lineSO);

            return lineSO;
        }

        public static string GenerateUniqueLineId(string conversationId, ICollection<string> usedIds)
        {
            int index = usedIds.Count + 1;
            var id = NodeFactory.GenerateLineId(conversationId, index);

            while (usedIds.Contains(id) || DialogueAssetLocator.FindLine(id) != null)
            {
                index++;
                id = NodeFactory.GenerateLineId(conversationId, index);
            }

            return id;
        }

        #endregion

        #region Private Methods

        private static string ResolveFolder(ConversationSO conversation)
        {
            var folder = DialogueAssetLocator.LinesFolderFor(conversation);

            if (AssetDatabase.IsValidFolder(folder))
                return folder;

            if (AssetDatabase.IsValidFolder(DialogueCSVFormat.LINES_FOLDER))
                return DialogueCSVFormat.LINES_FOLDER;

            Debug.LogError(
                $"[DialogueGraph] Dossier de lignes introuvable : \"{folder}\". " +
                $"Impossible de creer l'asset. Verifiez {DialogueCSVFormat.LINES_FOLDER}.");

            return null;
        }

        private static void InitializeFields(
            DialogueLineSO lineSO,
            ConversationSO conversation,
            string lineId,
            int sequenceOrder)
        {
            var so = new SerializedObject(lineSO);

            so.FindProperty("_lineId").stringValue = lineId;
            so.FindProperty("_conversationId").stringValue = conversation != null ? conversation.ConversationId : "";
            so.FindProperty("_sequenceOrder").intValue = sequenceOrder;
            so.FindProperty("_choices").arraySize = 0;
            so.FindProperty("_conditionalNexts").arraySize = 0;
            so.FindProperty("_onStartActions").arraySize = 0;
            so.FindProperty("_onEndActions").arraySize = 0;

            so.ApplyModifiedProperties();
        }

        #endregion
    }
}
