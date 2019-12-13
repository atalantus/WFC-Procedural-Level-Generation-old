using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LevelGeneration
{
    public class ModuleGeneration : EditorWindow
    {
        public GameObject[] modelSources;
        private bool generating = false;

        [MenuItem("Level Generation/Generate Modules")]
        public static void ShowWindow()
        {
            GetWindow<ModuleGeneration>(false, "Generate Modules");
        }

        private void OnGUI()
        {
            var serialObj = new SerializedObject(this);
            var serialModels = serialObj.FindProperty("modelSources");

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label("Generate WFC-Modules from Model Sources", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.Label("Generated WFC-Modules will be stored in the \"Assets/WFC Modules\" folder.",
                EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);

            EditorGUILayout.PropertyField(serialModels, true);

            GUILayout.Space(25);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Generate", GUILayout.Width(150), GUILayout.Height(25)))
            {
                if (modelSources.Length == 0) return;

                Debug.Log("Generate Modules");
                generating = true;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DisplayProgressBar();

            serialObj.ApplyModifiedProperties();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void DisplayProgressBar()
        {
            if (generating)
            {
                Debug.Log("Generating = true");
                if (EditorUtility.DisplayCancelableProgressBar("Generating WFC-Modules",
                    $"Generating from {modelSources.Length} ({0}/{modelSources.Length})", 0.45f))
                {
                    Debug.Log("Process canceled!");
                    generating = false;
                    Debug.Log("Generating = false");
                }
            }
            else
                EditorUtility.ClearProgressBar();
        }
    }
}