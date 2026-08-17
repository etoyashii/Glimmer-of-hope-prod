using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class DialogueFieldBuilder : DialogueNodeFieldBuilderBase
    {
        public DialogueFieldBuilder(DialogueNode node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }

        public override void Build()
        {
            var speakerField = new TextField("Speaker ID") { value = Node.speakerId };
            speakerField.RegisterValueChangedCallback(evt =>
            {
                Node.speakerId = evt.newValue;
                SetTitle?.Invoke(string.IsNullOrEmpty(evt.newValue) ? "(no speaker)" : evt.newValue);
                MarkDirty();
            });
            Container.Add(speakerField);

            var textField = new TextField("Text") { value = Node.text, multiline = true };
            textField.style.minHeight = 40;
            textField.RegisterValueChangedCallback(evt => { Node.text = evt.newValue; MarkDirty(); });
            Container.Add(textField);

            var bubbleField = new ObjectField("Bubble Prefab") { objectType = typeof(GameObject), value = Node.bubblePrefab };
            bubbleField.RegisterValueChangedCallback(evt => { Node.bubblePrefab = evt.newValue as GameObject; MarkDirty(); });
            Container.Add(bubbleField);

            var offsetField = new Vector3Field("Bubble Offset") { value = Node.bubbleOffset };
            offsetField.style.display = Node.followSpeaker ? DisplayStyle.Flex : DisplayStyle.None;
            offsetField.RegisterValueChangedCallback(evt => { Node.bubbleOffset = evt.newValue; MarkDirty(); });

            var followToggle = new Toggle("Follows Speaker") { value = Node.followSpeaker };
            followToggle.RegisterValueChangedCallback(evt =>
            {
                Node.followSpeaker = evt.newValue;
                offsetField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                MarkDirty();
            });
            Container.Add(followToggle);
            Container.Add(offsetField);

            var speedField = new FloatField("Typewriter Speed") { value = Node.typewriterCharsPerSecond };
            speedField.style.display = Node.useTypewriter ? DisplayStyle.Flex : DisplayStyle.None;
            speedField.RegisterValueChangedCallback(evt => { Node.typewriterCharsPerSecond = evt.newValue; MarkDirty(); });

            var typewriterToggle = new Toggle("Use Typewriter") { value = Node.useTypewriter };
            typewriterToggle.RegisterValueChangedCallback(evt =>
            {
                Node.useTypewriter = evt.newValue;
                speedField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                MarkDirty();
            });
            Container.Add(typewriterToggle);
            Container.Add(speedField);
        }
    }
}
