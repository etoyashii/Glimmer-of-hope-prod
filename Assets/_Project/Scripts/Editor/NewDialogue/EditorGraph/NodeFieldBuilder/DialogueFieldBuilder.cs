using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace GlimmerOfHope.Editor.NewDialogue
{
    public class DialogueFieldBuilder : DialogueNodeFieldBuilderBase
    {
        #region Public Methods
        public DialogueFieldBuilder(DialogueNodeBase node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }
        /// <summary>
        /// Builds and appends the UI fields of the Dialogue node type into Container.
        /// </summary>
        public override void Build()
        {
            var node = (DialogueLineNode)Node;
           
            var speakerField = new TextField("Speaker ID") { value = node.speakerId };
            speakerField.RegisterValueChangedCallback(evt =>
            {
                node.speakerId = evt.newValue;
                // Speaker ID doubles as the node's display title
                SetTitle?.Invoke(string.IsNullOrEmpty(evt.newValue) ? "(no speaker)" : evt.newValue);
                MarkDirty();
            });
            Container.Add(speakerField);
            
            var textField = new TextField("Text") { value = node.text, multiline = true };
            textField.style.minHeight = 40;
            textField.RegisterValueChangedCallback(evt => { node.text = evt.newValue; MarkDirty(); });
            Container.Add(textField);
           
            var bubbleField = new ObjectField("Bubble Prefab") { objectType = typeof(GameObject), value = node.bubblePrefab };
            bubbleField.RegisterValueChangedCallback(evt => { node.bubblePrefab = evt.newValue as GameObject; MarkDirty(); });
            Container.Add(bubbleField);
            
            // Offset only applies when the bubble follows the speaker
            var offsetField = new Vector3Field("Bubble Offset") { value = node.bubbleOffset };
            offsetField.style.display = node.followSpeaker ? DisplayStyle.Flex : DisplayStyle.None;
            offsetField.RegisterValueChangedCallback(evt => { node.bubbleOffset = evt.newValue; MarkDirty(); });
            
            var followToggle = new Toggle("Follows Speaker") { value = node.followSpeaker };
            followToggle.RegisterValueChangedCallback(evt =>
            {
                node.followSpeaker = evt.newValue;
                offsetField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                MarkDirty();
            });
            Container.Add(followToggle);
            Container.Add(offsetField);
           
            // Speed only applies when the typewriter effect is enabled
            var speedField = new FloatField("Typewriter Speed") { value = node.typewriterCharsPerSecond };
            speedField.style.display = node.useTypewriter ? DisplayStyle.Flex : DisplayStyle.None;
            speedField.RegisterValueChangedCallback(evt => { node.typewriterCharsPerSecond = evt.newValue; MarkDirty(); });
            
            var typewriterToggle = new Toggle("Use Typewriter") { value = node.useTypewriter };
            typewriterToggle.RegisterValueChangedCallback(evt =>
            {
                node.useTypewriter = evt.newValue;
                speedField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                MarkDirty();
            });
            Container.Add(typewriterToggle);
            Container.Add(speedField);
        }
        #endregion
    }
}