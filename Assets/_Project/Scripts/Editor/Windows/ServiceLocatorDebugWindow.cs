using System;
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.Editor.Windows
{
    public class ServiceLocatorDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem("Glimmer/Debug/Service Locator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ServiceLocatorDebugWindow>();
            window.titleContent = new GUIContent("Service Locator");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Registered Services", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Services are only available in Play Mode.", MessageType.Info);
                return;
            }

            var services = ServiceLocator.GetAllServices();

            if (services.Count == 0)
            {
                EditorGUILayout.HelpBox("No services registered.", MessageType.Warning);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var kvp in services)
            {
                DrawServiceEntry(kvp.Key, kvp.Value);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh"))
            {
                Repaint();
            }
        }

        private void DrawServiceEntry(Type type, IService service)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(service.GetType().FullName, EditorStyles.miniLabel);
            }
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
