using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class ActionListDrawer : VisualElement
    {
        #region Private Fields

        private readonly string _label;
        private readonly string _propertyPath;
        private VisualElement _listContainer;
        private SerializedProperty _arrayProperty;
        private Action _onChanged;

        #endregion

        #region Constructor

        public ActionListDrawer(string label, string propertyPath, Action onChanged)
        {
            _label = label;
            _propertyPath = propertyPath;
            _onChanged = onChanged;

            AddToClassList("action-list");
            BuildHeader();

            _listContainer = new VisualElement();
            _listContainer.AddToClassList("action-list__items");
            Add(_listContainer);
        }

        #endregion

        #region Public Methods

        public void Bind(SerializedObject serializedObject)
        {
            _arrayProperty = serializedObject?.FindProperty(_propertyPath);
            Rebuild();
        }

        public void Unbind()
        {
            _arrayProperty = null;
            _listContainer.Clear();
        }

        #endregion

        #region Private Methods — UI

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 4;

            var title = new Label(_label);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            var addButton = new Button(OnAddAction) { text = "+" };
            addButton.style.width = 24;
            addButton.style.height = 20;
            header.Add(addButton);

            Add(header);
        }

        private void Rebuild()
        {
            _listContainer.Clear();

            if (_arrayProperty == null)
                return;

            for (int i = 0; i < _arrayProperty.arraySize; i++)
            {
                BuildActionRow(i);
            }
        }

        private void BuildActionRow(int index)
        {
            var element = _arrayProperty.GetArrayElementAtIndex(index);
            var typeProp = element.FindPropertyRelative("type");
            var paramProp = element.FindPropertyRelative("parameter");
            var delayProp = element.FindPropertyRelative("delay");

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;
            row.style.alignItems = Align.Center;

            var typeField = BuildEnumField(typeProp, 120);
            row.Add(typeField);

            var paramField = BuildStringField(paramProp, "param");
            row.Add(paramField);

            var delayField = BuildFloatField(delayProp, 50);
            row.Add(delayField);

            var removeBtn = new Button(() => OnRemoveAction(index)) { text = "X" };
            removeBtn.style.width = 22;
            removeBtn.style.height = 18;
            removeBtn.style.color = new Color(1f, 0.4f, 0.4f);
            row.Add(removeBtn);

            _listContainer.Add(row);
        }

        private VisualElement BuildEnumField(SerializedProperty prop, int width)
        {
            var field = new EnumField((DialogueActionType)prop.enumValueIndex);
            field.style.width = width;
            field.RegisterValueChangedCallback(evt =>
            {
                prop.enumValueIndex = Convert.ToInt32(evt.newValue);
                ApplyChanges();
            });
            return field;
        }

        private VisualElement BuildStringField(SerializedProperty prop, string placeholder)
        {
            var field = new TextField();
            field.value = prop.stringValue;
            field.style.flexGrow = 1;
            field.style.marginLeft = 4;
            field.style.marginRight = 4;
            field.RegisterValueChangedCallback(evt =>
            {
                prop.stringValue = evt.newValue;
                ApplyChanges();
            });
            return field;
        }

        private VisualElement BuildFloatField(SerializedProperty prop, int width)
        {
            var field = new FloatField();
            field.value = prop.floatValue;
            field.style.width = width;
            field.label = "dt";
            field.RegisterValueChangedCallback(evt =>
            {
                prop.floatValue = evt.newValue;
                ApplyChanges();
            });
            return field;
        }

        #endregion

        #region Private Methods — Actions

        private void OnAddAction()
        {
            if (_arrayProperty == null) return;

            _arrayProperty.arraySize++;
            ApplyChanges();
            Rebuild();
        }

        private void OnRemoveAction(int index)
        {
            if (_arrayProperty == null || index >= _arrayProperty.arraySize) return;

            _arrayProperty.DeleteArrayElementAtIndex(index);
            ApplyChanges();
            Rebuild();
        }

        private void ApplyChanges()
        {
            _arrayProperty?.serializedObject.ApplyModifiedProperties();
            _onChanged?.Invoke();
        }

        #endregion
    }
}
