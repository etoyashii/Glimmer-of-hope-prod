using Codice.Client.BaseCommands.BranchExplorer;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// Logique système du système Enum.
    /// </summary>
    public class EnumSectionUtils
    {
        #region Cache
        private static readonly Dictionary<Type, bool> _hasSectionsCache = new Dictionary<Type, bool>();
        private static readonly Dictionary<Type, FieldInfo[]> _fieldsCache = new Dictionary<Type, FieldInfo[]>();
        #endregion

        #region API Publique
        //Draw dans l'éditeur en fonctions des Enum sections activées
        public static void DrawWithSections(SerializedObject so)
        {
            so.Update();
            DrawProperties(so);
            so.ApplyModifiedProperties();
        }

        //check s'il y a des Enum sections dans le MonoBehaviour
        public static bool HasAnySections(SerializedObject so)
        {
            if (so == null) return false;
            Type type = so.targetObject.GetType();
            if (_hasSectionsCache.TryGetValue(type, out bool result))
                return result;

            bool has = false;
            foreach (FieldInfo field in GetAllFields(type))
            {
                if (field.GetCustomAttribute<EnumSectionBeginAttribute>() != null)
                {
                    has = true;
                    break;
                }
            }

            _hasSectionsCache[type] = has;
            return has;
        }
        #endregion

        #region Draw
        //Draw les variables si elles sont dans les Enum sections selectionnées
        private static void DrawProperties(SerializedObject so)
        {
            object target = so.targetObject;
            Type targetType = target.GetType();

            bool inSection = false;
            bool sectionVisible = true;

            SerializedProperty prop = so.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (prop.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(prop);

                    continue;
                }

                FieldInfo field = GetFieldInfo(targetType, prop.name);

                if (field != null)
                {
                    if (field.GetCustomAttribute<EnumSectionEndAttribute>() != null)
                    {
                        inSection = false;
                        sectionVisible = true;
                        EditorGUILayout.PropertyField(prop, true);
                        continue;
                    }

                    var beginAttr = field.GetCustomAttribute<EnumSectionBeginAttribute>();
                    if (beginAttr != null)
                    {
                        inSection = true;
                        sectionVisible = EvaluateSection(targetType, target, beginAttr);

                        if (sectionVisible)
                            EditorGUILayout.PropertyField(prop, true);

                        continue;
                    }
                }

                if (!inSection || sectionVisible)
                    EditorGUILayout.PropertyField(prop, true);
            }
        }
        #endregion

        #region Helpers
        //Récupère les Enum sections sélectionnées
        private static bool EvaluateSection(Type targetType, object target, EnumSectionBeginAttribute attr)
        {
            FieldInfo enumField = GetFieldInfo(targetType, attr.fieldName);
            if (enumField == null) return false;

            object currentValue = enumField.GetValue(target);
            return ValuesMatch(currentValue, attr.value);
        }

        //Check de correspondance pour les enums selectionnées
        private static bool ValuesMatch(object current, object expected)
        {
            if (current == null || expected == null) return false;
            try
            {
                return Convert.ToInt32(current) == Convert.ToInt32(expected);
            }
            catch
            {
                return current.Equals(expected);
            }
        }

        //récupère les infos d'un champ présent dans une balise d'enum section
        private static FieldInfo GetFieldInfo(Type type, string  fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance 
                | BindingFlags.Public
                | BindingFlags.NonPublic;

            Type t = type;
            while (t != null && t != typeof(object))
            {
                FieldInfo f = t.GetField(fieldName, flags);
                if (f != null) return f;
                t = t.BaseType;
            }
            return null;
        }

        //récupère tous les champs présent dans une balise d'enum section
        private static IEnumerable<FieldInfo> GetAllFields(Type type)
        {
            if (_fieldsCache.TryGetValue(type, out FieldInfo[] cached))
                return cached;

            var list = new List<FieldInfo>();
            Type t = type;
            const BindingFlags flags = BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic;

            while ( t != null && t != typeof(object))
            {
                list.AddRange(t.GetFields(flags));
                t = t.BaseType;
            }

            FieldInfo[] arr = list.ToArray();
            _fieldsCache[type] = arr;
            return arr;
        }
        #endregion
    }
}
