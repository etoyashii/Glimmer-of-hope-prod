using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class ChoiceManager
    {
        #region Private Fields

        private readonly DialogueLineNode _node;
        private readonly List<Port> _ports;
        private readonly VisualElement _container;
        private readonly List<ChoiceRow> _rows = new();
        private readonly ChoiceTextStore _texts = new();
        private Button _addButton;
        private string _activeLanguage = DialogueCSVFormat.DEFAULT_LANGUAGE;

        #endregion

        #region Constructor

        public ChoiceManager(DialogueLineNode node, List<Port> ports, VisualElement container)
        {
            _node = node;
            _ports = ports;
            _container = container;
        }

        #endregion

        #region Public Methods

        public void LoadChoiceTexts(
            DialogueLineSO lineSO,
            Dictionary<string, Dictionary<string, string>> allLangData)
        {
            _texts.Load(lineSO, allLangData);
        }

        public void RebuildUI(string language)
        {
            _activeLanguage = language;
            ClearRows();

            if (_node.LineSO != null && _node.LineSO.HasChoices)
            {
                for (int i = 0; i < _node.LineSO.Choices.Length; i++)
                    BuildChoiceRow(i);
            }

            BuildAddButton();
            _node.RefreshPorts();
        }

        public void RefreshLanguage(string language)
        {
            _activeLanguage = language;

            foreach (var row in _rows)
                row.Field.SetValueWithoutNotify(_texts.Get(row.Index, language));
        }

        public void AddChoice()
        {
            var lineSO = _node.LineSO;
            if (lineSO == null) return;

            var so = new SerializedObject(lineSO);
            var choicesProp = so.FindProperty("_choices");
            choicesProp.arraySize++;
            ResetChoice(choicesProp.GetArrayElementAtIndex(choicesProp.arraySize - 1));
            so.ApplyModifiedProperties();

            int index = choicesProp.arraySize - 1;
            _texts.AddEmpty(index);

            BuildChoiceRow(index);
            MoveAddButtonToEnd();
            _node.RefreshPorts();
            _node.RaiseContentChanged();
        }

        public void RemoveChoice(int index)
        {
            var lineSO = _node.LineSO;
            if (lineSO == null) return;

            var so = new SerializedObject(lineSO);
            var choicesProp = so.FindProperty("_choices");

            if (index < 0 || index >= choicesProp.arraySize) return;

            choicesProp.DeleteArrayElementAtIndex(index);
            so.ApplyModifiedProperties();

            RemoveRow(index);
            _texts.ShiftAfterRemove(index);
            _node.RefreshPorts();
            _node.RaiseContentChanged();
        }

        public void SetChoiceText(int index, string language, string text)
        {
            _texts.Set(_node.LineSO, index, language, text);
        }

        public string GetChoiceText(int index, string language)
        {
            return _texts.Get(index, language);
        }

        public void CollectLocalizationData(
            string lineId,
            Dictionary<string, Dictionary<string, string>> dataPerLang)
        {
            _texts.Collect(lineId, dataPerLang);
        }

        #endregion

        #region Private Methods — Rows

        private void BuildChoiceRow(int index)
        {
            var row = new ChoiceRow { Index = index };

            row.Root = new VisualElement();
            row.Root.style.flexDirection = FlexDirection.Row;
            row.Root.style.alignItems = Align.Center;
            row.Root.style.marginBottom = 2;

            row.Field = BuildChoiceField(row);
            row.Root.Add(row.Field);

            row.Port = DialoguePortFactory.CreateChoiceOutput(_node, "", index);
            row.Root.Add(row.Port);
            row.Root.Add(BuildRemoveButton(row));

            _rows.Add(row);
            _ports.Add(row.Port);
            _container.Add(row.Root);
        }

        private TextField BuildChoiceField(ChoiceRow row)
        {
            var field = new TextField();
            field.value = _texts.Get(row.Index, _activeLanguage);
            field.style.flexGrow = 1;
            field.style.minWidth = 120;
            field.multiline = false;
            field.RegisterValueChangedCallback(evt =>
            {
                SetChoiceText(row.Index, _activeLanguage, evt.newValue);
                _node.RaiseContentChanged();
            });

            return field;
        }

        private Button BuildRemoveButton(ChoiceRow row)
        {
            var button = new Button(() => RemoveChoice(row.Index)) { text = "X" };
            button.style.width = 20;
            button.style.height = 18;
            button.style.color = new Color(1f, 0.4f, 0.4f);
            button.style.fontSize = 10;

            return button;
        }

        private void RemoveRow(int index)
        {
            if (index < 0 || index >= _rows.Count) return;

            var row = _rows[index];

            foreach (var edge in new List<Edge>(row.Port.connections))
                PortEdgeUtility.Disconnect(edge);

            _ports.Remove(row.Port);
            _rows.RemoveAt(index);
            row.Root.RemoveFromHierarchy();

            for (int i = index; i < _rows.Count; i++)
                _rows[i].SetIndex(i);
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
            {
                foreach (var edge in new List<Edge>(row.Port.connections))
                    PortEdgeUtility.Disconnect(edge);
            }

            _ports.Clear();
            _rows.Clear();
            _container.Clear();
            _addButton = null;
        }

        private void BuildAddButton()
        {
            _addButton = new Button(AddChoice) { text = "+ Choix" };
            _addButton.style.marginTop = 4;
            _addButton.style.alignSelf = Align.FlexStart;
            _addButton.style.backgroundColor = new Color(0.2f, 0.35f, 0.5f);
            _addButton.style.height = 20;
            _addButton.style.fontSize = 10;
            _container.Add(_addButton);
        }

        private void MoveAddButtonToEnd()
        {
            if (_addButton == null)
            {
                BuildAddButton();
                return;
            }

            _addButton.RemoveFromHierarchy();
            _container.Add(_addButton);
        }

        private static void ResetChoice(SerializedProperty choice)
        {
            choice.FindPropertyRelative("choiceText").stringValue = "";
            choice.FindPropertyRelative("conditionFlag").stringValue = "";
            choice.FindPropertyRelative("setFlag").stringValue = "";
            choice.FindPropertyRelative("targetLine").objectReferenceValue = null;
        }

        #endregion
    }
}
