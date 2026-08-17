using System.Collections.Generic;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// One-time tool to convert every DialogueGraph asset from the old flat DialogueNode list
    /// to the new typed hierarchy (DialogueLineNode, StartNode, EndNode, GateNode,
    /// ConditionNode, ActionNode). Safe to run multiple times - it just overwrites
    /// TypedNodes each time, the old "nodes" field is never touched or deleted.
    /// </summary>
    public static class DialogueGraphMigrationTool
    {
        [MenuItem("Window/Dialogue System/Migrate To Typed Nodes")]
        public static void MigrateAllGraphs()
        {
            var guids = AssetDatabase.FindAssets("t:DialogueGraph");
            int migratedCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(path);
                if (graph == null) continue;

                MigrateGraph(graph);
                migratedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DialogueGraphMigrationTool] Migrated {migratedCount} DialogueGraph asset(s).");
        }

        private static void MigrateGraph(DialogueGraph graph)
        {
            var typedNodes = new List<DialogueNodeBase>();

            foreach (var oldNode in graph.nodes)
            {
                var newNode = ConvertNode(oldNode);
                if (newNode == null)
                {
                    Debug.LogWarning($"[DialogueGraphMigrationTool] Unknown node type '{oldNode.nodeType}' on '{graph.name}', skipped.");
                    continue;
                }

                newNode.nodeId = oldNode.nodeId;
                newNode.editorPosition = oldNode.editorPosition;
                newNode.choices = oldNode.choices;

                typedNodes.Add(newNode);
            }

            graph.SetTypedNodes(typedNodes);
            EditorUtility.SetDirty(graph);
        }

        private static DialogueNodeBase ConvertNode(DialogueNode old)
        {
            switch (old.nodeType)
            {
                case DialogueNodeType.Dialogue:
                    return new DialogueLineNode
                    {
                        speakerId = old.speakerId,
                        text = old.text,
                        bubblePrefab = old.bubblePrefab,
                        followSpeaker = old.followSpeaker,
                        bubbleOffset = old.bubbleOffset,
                        useTypewriter = old.useTypewriter,
                        typewriterCharsPerSecond = old.typewriterCharsPerSecond,
                        hasChoices = old.hasChoices
                    };

                case DialogueNodeType.Start:
                    return new StartNode
                    {
                        triggerType = old.triggerType,
                        buttonOffset = old.buttonOffset,
                        triggerZoneRadius = old.triggerZoneRadius
                    };

                case DialogueNodeType.End:
                    return new EndNode { outcomeId = old.outcomeId };

                case DialogueNodeType.Gate:
                    return new GateNode
                    {
                        gateTriggerType = old.gateTriggerType,
                        gateEventId = old.gateEventId,
                        gateTimerSeconds = old.gateTimerSeconds,
                        gateFlagName = old.gateFlagName,
                        gateFlagExpectedValue = old.gateFlagExpectedValue
                    };

                case DialogueNodeType.Condition:
                    return new ConditionNode
                    {
                        conditionType = old.conditionType,
                        conditionFlagName = old.conditionFlagName,
                        conditionExpectedValue = old.conditionExpectedValue,
                        conditionScriptId = old.conditionScriptId
                    };

                case DialogueNodeType.Action:
                    return new ActionNode
                    {
                        actionType = old.actionType,
                        actionFlagName = old.actionFlagName,
                        actionFlagValue = old.actionFlagValue,
                        actionScriptId = old.actionScriptId
                    };

                default:
                    return null;
            }
        }
    }
}
