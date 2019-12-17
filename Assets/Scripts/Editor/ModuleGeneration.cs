using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace LevelGeneration
{
    public class ModuleGeneration : EditorWindow
    {
        public string folderPath = "Assets/WFC Modules";
        public GameObject[] modelSources;
        public bool addToExistingModules = true;

        private string AbsoluteFolderPath =>
            $"{Application.dataPath.Remove(Application.dataPath.Length - 7)}/{folderPath}";

        private string RelativeMCPath => $"{folderPath}/Resources/Module Connections.asset";

        private Action _generateModules;
        private ModuleConnections _moduleConnections;
        private bool _generating;
        private int _i;

        private Vector2 scrollPos;

        [MenuItem("Level Generation/Generate Modules")]
        public static void ShowWindow()
        {
            GetWindow<ModuleGeneration>(false, "Generate Modules");
        }

        private void OnFocus()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            #region Basic GUI

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

            folderPath = EditorGUILayout.TextField("Destination Folder Path", folderPath);

            GUILayout.Space(5);

            addToExistingModules = EditorGUILayout.Toggle("Add to existing Modules", addToExistingModules);

            GUILayout.Space(5);

            EditorGUILayout.PropertyField(serialModels, true);

            GUILayout.Space(25);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            #endregion

            if (GUILayout.Button("Generate", GUILayout.Width(150), GUILayout.Height(25)))
            {
                _generating = true;

                SetupGenerateModules();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_generating)
            {
                if (_i == modelSources.Length)
                {
                    // Finished generating modules
                    _generating = false;
                    _generateModules = null;
                    EditorUtility.ClearProgressBar();

                    // Write changes to disc
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    DisplayProgressBar();
                    _generateModules();
                }
            }

            serialObj.ApplyModifiedProperties();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(Screen.width - 160, Screen.height - 300, 150, 250), new GUIStyle(GUI.skin.box));
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(150), GUILayout.Height(250));

            for (int i = 0; i < 30; i++)
            {
                GUILayout.Toggle(false, i.ToString());
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            Handles.EndGUI();
        }


        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void DisplayProgressBar()
        {
            if (EditorUtility.DisplayCancelableProgressBar("Generating WFC-Modules",
                $"Generating from {modelSources[_i].name} ({_i}/{modelSources.Length})",
                (float) _i / modelSources.Length))
            {
                _generating = false;
            }
        }

        private void SetupGenerateModules()
        {
            if (modelSources.Length == 0) return;
            try
            {
                // Clear asset directory
                if (!addToExistingModules && Directory.Exists(AbsoluteFolderPath))
                {
                    // Remove all existing modules
                    Directory.CreateDirectory(AbsoluteFolderPath).Delete(true);
                }

                Directory.CreateDirectory(AbsoluteFolderPath);

                AssetDatabase.Refresh();

                // Create Module Connections Asset
                Directory.CreateDirectory(AbsoluteFolderPath + "/Resources");

                if (AssetDatabase.LoadAssetAtPath<ModuleConnections>(RelativeMCPath) == null)
                {
                    // Create Module Connections Asset
                    AssetDatabase.CreateAsset(CreateInstance<ModuleConnections>(), RelativeMCPath);
                }

                _moduleConnections = AssetDatabase.LoadAssetAtPath<ModuleConnections>(RelativeMCPath);

                // Setup Generate Modules
                _i = 0;
                _generateModules = GenerateModule;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void GenerateModule()
        {
            try
            {
                var moduleAsset = CreateInstance<Module>();

                AssetDatabase.CreateAsset(moduleAsset,
                    $"{folderPath}/{modelSources[_i].name}.asset");

                // TODO: Create hash key from vertices
                if (!_moduleConnections.faceConnectionsMap.ContainsKey(modelSources[_i].name))
                    _moduleConnections.faceConnectionsMap.Add(modelSources[_i].name, _i);

                // TODO: Check for rotated versions and set asset properties

                _i++;
            }
            catch (Exception e)
            {
                _generating = false;
                _generateModules = null;

                // Write changes to disc
                AssetDatabase.SaveAssets();
                Debug.LogError(e);
            }
        }
    }
}