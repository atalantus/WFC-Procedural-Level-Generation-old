using System;
using UnityEditor;
using UnityEngine;

namespace WFCLevelGeneration.Editor
{
    [CustomEditor(typeof(ModulesManager))]
    public class ModulesManagerEditor : UnityEditor.Editor
    {
        private Vector2 _scrollPosScene;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        private void OnSceneGUI()
        {
            var modulesManager = (ModulesManager) target;

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(Screen.width - 160, Screen.height - 295, 150, 250),
                new GUIStyle(GUI.skin.box));

            GUILayout.Label("Face Filters:", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Toggle all", GUILayout.Width(100))) Debug.Log("Toggle all");

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            if (modulesManager.modulesInfo != null)
            {
                _scrollPosScene = GUILayout.BeginScrollView(_scrollPosScene, false, false, GUILayout.Width(146),
                    GUILayout.Height(200));

                foreach (var faceConnection in modulesManager.modulesInfo.generatedConnections)
                    GUILayout.Toggle(false, $"{faceConnection.Key} ({faceConnection.Value})");

                GUILayout.EndScrollView();
            }
            else
            {
                GUI.skin.label.wordWrap = true;
                GUILayout.Label(
                    "Make sure you have a ModuleConnections Asset storing the different face connections. " +
                    "\nUse the \"Generate Modules\" editor window to automatically generate this asset.");
            }

            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}