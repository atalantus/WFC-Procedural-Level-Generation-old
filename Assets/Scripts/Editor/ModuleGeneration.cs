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
        public string folderPath = "Assets/Level Generation";
        public GameObject[] modelSources;
        public bool addToExistingModules = true;
        private ModuleConnections ModuleConnections => ModuleConnections.Instance;

        private string AbsoluteFolderPath =>
            $"{Application.dataPath.Remove(Application.dataPath.Length - 7)}/{folderPath}";

        private string RelativeMCPath => $"{folderPath}/Assets/Resources/Module Connections.asset";

        private Action _generateModules;
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
                if (modelSources == null || _i == modelSources.Length)
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
            GUILayout.BeginArea(new Rect(Screen.width - 160, Screen.height - 300, 150, 250),
                new GUIStyle(GUI.skin.box));

            if (ModuleConnections != null)
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(140), GUILayout.Height(240));

                foreach (var faceConnection in ModuleConnections.faceConnectionsMap)
                {
                    GUILayout.Toggle(false, $"{faceConnection.Value} ({faceConnection.Key})");
                }

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
            var absoluteAssetFolderPath = AbsoluteFolderPath + "/Assets";
            var absolutePrefabFolderPath = AbsoluteFolderPath + "/Prefabs";

            try
            {
                // Clear asset directory
                if (!addToExistingModules && Directory.Exists(absoluteAssetFolderPath))
                {
                    // Remove all existing modules
                    Directory.CreateDirectory(absoluteAssetFolderPath).Delete(true);
                }

                // Clear prefab directory
                if (!addToExistingModules && Directory.Exists(absolutePrefabFolderPath))
                {
                    // Remove all existing prefabs
                    Directory.CreateDirectory(absolutePrefabFolderPath).Delete(true);
                }

                // Make sure we have these directories
                Directory.CreateDirectory(absoluteAssetFolderPath);
                Directory.CreateDirectory(absoluteAssetFolderPath + "/Variants");
                Directory.CreateDirectory(absolutePrefabFolderPath);
                Directory.CreateDirectory(absolutePrefabFolderPath + "/Variants");

                AssetDatabase.Refresh();

                // Create Module Connections Asset
                Directory.CreateDirectory(absoluteAssetFolderPath + "/Resources");

                if (AssetDatabase.LoadAssetAtPath<ModuleConnections>(RelativeMCPath) == null)
                {
                    // Create Module Connections Asset
                    AssetDatabase.CreateAsset(CreateInstance<ModuleConnections>(), RelativeMCPath);
                }

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
                var rotation = Vector3.zero;

                for (int i = 0; i < 4; i++)
                {
                    var moduleName = rotation == Vector3.zero
                        ? modelSources[_i].name
                        : $"{modelSources[_i].name} ({rotation.y})";
                    var variantPath = rotation == Vector3.zero ? "" : "Variants/";

                    var moduleAsset = CreateInstance<Module>();

                    // Create asset
                    AssetDatabase.CreateAsset(moduleAsset,
                        $"{folderPath}/Assets/{variantPath}{moduleName}.asset");

                    // Create prefab
                    // var instanceRoot = PrefabUtility.InstantiatePrefab(modelSources[_i]) as GameObject;
                    // var prefabVariant = PrefabUtility.SaveAsPrefabAsset(instanceRoot,
                    //     $"{folderPath}/Prefabs/{variantPath}{moduleName}.prefab");
                    // prefabVariant.transform.rotation = Quaternion.Euler(rotation);
                    //
                    // moduleAsset.moduleGO = prefabVariant;

                    // TODO: Create hash key from vertices
                    if (!ModuleConnections.faceConnectionsMap.ContainsKey(modelSources[_i].name))
                        ModuleConnections.faceConnectionsMap.Add(modelSources[_i].name, _i);

                    rotation = new Vector3(0, rotation.y + 90, 0);
                }

                _i++;
            }
            catch (Exception e)
            {
                _generating = false;
                _generateModules = null;
                EditorUtility.ClearProgressBar();

                // Write changes to disc
                AssetDatabase.SaveAssets();
                Debug.LogError(e);
            }
        }
    }
}