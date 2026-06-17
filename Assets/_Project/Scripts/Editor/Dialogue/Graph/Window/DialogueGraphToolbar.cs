using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GlimmerOfHope.Gameplay.Dialogue;
using GlimmerOfHope.Editor.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class DialogueGraphToolbar : VisualElement
    {
        #region Private Fields

        private readonly DialogueGraphView _graphView;
        private DropdownField _languageDropdown;
        private DropdownField _conversationPicker;
        private Label _convNameLabel;
        private Action<ConversationSO> _onConversationSelected;
        private Action _onSaveClicked;
        private Action _onPreviewClicked;

        #endregion

        #region Constructor

        public DialogueGraphToolbar(
            DialogueGraphView graphView,
            Action<ConversationSO> onConversationSelected,
            Action onSaveClicked,
            Action onPreviewClicked = null)
        {
            _graphView = graphView;
            _onConversationSelected = onConversationSelected;
            _onSaveClicked = onSaveClicked;
            _onPreviewClicked = onPreviewClicked;

            AddToClassList("graph-toolbar");
            style.flexDirection = FlexDirection.Row;
            style.height = 32;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.alignItems = Align.Center;
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            style.borderBottomWidth = 1;
            style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);

            BuildButtons();
            BuildConversationPicker();
            BuildLanguageToggle();
            BuildConversationLabel();
        }

        #endregion

        #region Public Methods

        public void UpdateConversationLabel(ConversationSO conversation)
        {
            if (conversation == null)
            {
                _convNameLabel.text = "Aucune conversation";
                return;
            }

            var lineCount = _graphView.GetLineNodes().Count;
            _convNameLabel.text = $"{conversation.DisplayName} ({lineCount} lignes)";
        }

        public void RefreshConversationList()
        {
            var names = LoadConversationNames();
            _conversationPicker.choices = names;
        }

        #endregion

        #region Private Methods — Build

        private void BuildButtons()
        {
            var saveBtn = CreateButton("Sauvegarder", () => _onSaveClicked?.Invoke());
            saveBtn.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f);
            Add(saveBtn);

            AddSpacer(4);

            var previewBtn = CreateButton("Preview", () => _onPreviewClicked?.Invoke());
            previewBtn.style.backgroundColor = new Color(0.3f, 0.2f, 0.5f);
            Add(previewBtn);

            AddSpacer(4);
        }

        private void BuildConversationPicker()
        {
            var names = LoadConversationNames();
            _conversationPicker = new DropdownField("", names, 0);
            _conversationPicker.style.width = 200;
            _conversationPicker.RegisterValueChangedCallback(OnConversationPickerChanged);
            Add(_conversationPicker);

            AddSpacer(8);
        }

        private void BuildLanguageToggle()
        {
            var languages = new List<string> { "fr", "en", "es" };
            _languageDropdown = new DropdownField("Langue", languages, 0);
            _languageDropdown.style.width = 140;
            _languageDropdown.RegisterValueChangedCallback(OnLanguageChanged);
            Add(_languageDropdown);
        }

        private void BuildConversationLabel()
        {
            AddFlexSpacer();

            _convNameLabel = new Label("Aucune conversation");
            _convNameLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _convNameLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            Add(_convNameLabel);
        }

        #endregion

        #region Private Methods — Callbacks

        private void OnConversationPickerChanged(ChangeEvent<string> evt)
        {
            var conversation = FindConversationByName(evt.newValue);
            _onConversationSelected?.Invoke(conversation);
        }

        private void OnLanguageChanged(ChangeEvent<string> evt)
        {
            _graphView.SetActiveLanguage(evt.newValue);
        }

        #endregion

        #region Private Methods — Helpers

        private List<string> LoadConversationNames()
        {
            var names = new List<string> { "(choisir...)" };
            var guids = AssetDatabase.FindAssets("t:ConversationSO", new[] { DialogueCSVFormat.CONVERSATIONS_FOLDER });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var conv = AssetDatabase.LoadAssetAtPath<ConversationSO>(path);
                if (conv != null)
                    names.Add(conv.ConversationId);
            }

            return names;
        }

        private ConversationSO FindConversationByName(string convId)
        {
            if (convId == "(choisir...)")
                return null;

            var guids = AssetDatabase.FindAssets("t:ConversationSO", new[] { DialogueCSVFormat.CONVERSATIONS_FOLDER });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var conv = AssetDatabase.LoadAssetAtPath<ConversationSO>(path);
                if (conv != null && conv.ConversationId == convId)
                    return conv;
            }

            return null;
        }

        private Button CreateButton(string text, Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.height = 22;
            return btn;
        }

        private void AddSpacer(int width)
        {
            var spacer = new VisualElement();
            spacer.style.width = width;
            Add(spacer);
        }

        private void AddFlexSpacer()
        {
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            Add(spacer);
        }

        #endregion
    }
}
