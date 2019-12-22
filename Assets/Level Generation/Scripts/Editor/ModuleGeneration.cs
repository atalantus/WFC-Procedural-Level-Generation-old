using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace LevelGeneration
{
    public class ModuleGeneration : EditorWindow
    {
        public GameObject[] modelSources;
        public bool clearExistingModules;

        private const string ModulesPath = "Assets/Level Generation/Modules";

        private Action _generateModules;
        private bool _generating;
        private int _i;

        private Vector2 scrollPos;

        #region Unity Events

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
            DrawGUI();

            if (_generating)
            {
                if (modelSources == null || _i == modelSources.Length)
                {
                    StopModuleGeneration();
                }
                else
                {
                    DisplayProgressBar();
                    _generateModules();
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(Screen.width - 160, Screen.height - 300, 150, 250),
                new GUIStyle(GUI.skin.box));

            if (ModulesInfo.Instance != null)
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(140), GUILayout.Height(240));

                foreach (var faceConnection in ModulesInfo.Instance.faceConnectionsMap)
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

        #endregion

        #region Module Generation

        /// <summary>
        /// Sets up all the necessary folder and files for the GenerateModule process.
        /// </summary>
        private void SetupGenerateModules()
        {
            var absoluteModulesPath = $"{Application.dataPath.Remove(Application.dataPath.Length - 7)}/{ModulesPath}";

            try
            {
                if (clearExistingModules && Directory.Exists(absoluteModulesPath))
                {
                    // Clear modules folder
                    Directory.CreateDirectory(absoluteModulesPath).Delete(true);
                }

                // Make sure the folder structure is correctly set up
                Directory.CreateDirectory(absoluteModulesPath);
                Directory.CreateDirectory($"{absoluteModulesPath}/Assets");
                Directory.CreateDirectory($"{absoluteModulesPath}/Assets/Variants");
                Directory.CreateDirectory($"{absoluteModulesPath}/Prefabs");
                Directory.CreateDirectory($"{absoluteModulesPath}/Prefabs/Variants");

                // Setup ModulesInfo asset
                if (AssetDatabase.LoadAssetAtPath<ModulesInfo>($"{ModulesPath}/ModulesInfo.asset") == null)
                {
                    // Create Module Connections Asset
                    AssetDatabase.CreateAsset(CreateInstance<ModulesInfo>(), $"{ModulesPath}/ModulesInfo.asset");
                }

                // Setup Modules scene
                var modulesScene = AssetDatabase.LoadAssetAtPath<SceneAsset>($"{ModulesPath}/Modules.unity");
                if (modulesScene == null)
                {
                    // Create modules scene
                    var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    newScene.name = "Modules";
                }
                else
                {
                    // Open scene
                    EditorSceneManager.OpenScene($"{ModulesPath}/Modules.unity", OpenSceneMode.Single);
                }

                AssetDatabase.Refresh();

                // Setup Generate Modules
                _i = 0;
                _generateModules = GenerateModule;
            }
            catch (Exception e)
            {
                StopModuleGeneration();
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Generates the next module
        /// </summary>
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
                        $"{ModulesPath}/Assets/{variantPath}{moduleName}.asset");

                    // Create prefab
                    var instanceRoot = PrefabUtility.InstantiatePrefab(modelSources[_i]) as GameObject;
                    var prefabVariant = PrefabUtility.SaveAsPrefabAsset(instanceRoot,
                        $"{ModulesPath}/Prefabs/{variantPath}{moduleName}.prefab");
                    prefabVariant.transform.rotation = Quaternion.Euler(rotation);

                    moduleAsset.moduleGO = prefabVariant;

                    // TODO: Create hash key from vertices
                    if (!ModulesInfo.Instance.faceConnectionsMap.ContainsKey(modelSources[_i].name))
                        ModulesInfo.Instance.faceConnectionsMap.Add(modelSources[_i].name, _i);

                    rotation = new Vector3(0, rotation.y + 90, 0);
                }

                _i++;
            }
            catch (Exception e)
            {
                StopModuleGeneration();
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Stops the GenerateModule process
        /// </summary>
        private void StopModuleGeneration()
        {
            // Finished generating modules
            _generating = false;
            _i = 0;
            _generateModules = null;
            EditorUtility.ClearProgressBar();
            EditorSceneManager.SaveOpenScenes();

            // Write changes to disc
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region GUI

        /// <summary>
        /// Draw Editor Window GUI
        /// </summary>
        private void DrawGUI()
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

            clearExistingModules = EditorGUILayout.Toggle("Clear existing Modules", clearExistingModules);

            GUILayout.Space(5);

            EditorGUILayout.PropertyField(serialModels, true);

            GUILayout.Space(25);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Generate", GUILayout.Width(150), GUILayout.Height(25)))
            {
                _generating = true;

                SetupGenerateModules();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            serialObj.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the progress bar
        /// </summary>
        private void DisplayProgressBar()
        {
            if (EditorUtility.DisplayCancelableProgressBar("Generating WFC-Modules",
                $"Generating from {modelSources[_i].name} ({_i}/{modelSources.Length})",
                (float) _i / modelSources.Length))
            {
                _generating = false;
            }
        }

        #endregion
    }
}