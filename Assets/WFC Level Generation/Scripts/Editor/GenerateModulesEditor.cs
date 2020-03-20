using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WFCLevelGeneration.Editor
{
    public class GenerateModulesEditor : EditorWindow
    {
        /**
         * Window fields
         */
        public string folderPath = "Assets/WFC Level Generation";

        public bool generateEmpty = true;
        public bool generateCell = true;
        public Cell cell;
        public GameObject[] modelSources;

        private string ModulesPath => folderPath + "/Modules";

        private ModulesManager _modulesManager;
        [SerializeField] private ModulesInfo modulesInfo;
        private bool _generating;
        private int _currentSourceIndex;

        /**
         * Window properties
         */
        private string _progressBarInfo = "";

        private Vector2 _scrollPosEditor;
        private int _modulesSceneCount = 0;

        /// <summary>
        /// Process next Module Data.
        /// </summary>
        /// <param name="data">Module Data</param>
        private void ProcessNextModule(ModuleGeneration.ModuleData data)
        {
            do
            {
                ++_currentSourceIndex;
            } while (_currentSourceIndex < modelSources.Length && modelSources[_currentSourceIndex] == null);
            
            // TODO
            
            var layoutPos = ModuleGeneration.GetNextLayoutPos(
                new Vector2(cell.transform.localScale.x, cell.transform.localScale.z), ++_modulesSceneCount);
        }

        /// <summary>
        /// Generate Empty Module
        /// </summary>
        private void GenerateEmptyModule()
        {
            // Create prefab
            var obj = new GameObject("_Empty");
            var prefab = PrefabUtility.SaveAsPrefabAsset(obj, $"{ModulesPath}/Prefabs/_Empty.prefab");
            DestroyImmediate(obj);

            EditorUtility.SetDirty(prefab);

            var moduleAsset = CreateInstance<Module>();

            // Create asset
            AssetDatabase.CreateAsset(moduleAsset, $"{ModulesPath}/Assets/_Empty.asset");

            // Assign asset values
            moduleAsset.moduleGO = prefab;
            for (var j = 0; j < moduleAsset.faceConnections.Length; j++) moduleAsset.faceConnections[j] = 0;

            // Mark asset as dirty
            // TODO: Useless?
            EditorUtility.SetDirty(moduleAsset);
        }

        /// <summary>
        /// Setup Module Generation
        /// </summary>
        private void ModuleGenerationSetup()
        {
            modulesInfo = AssetDatabase.LoadAssetAtPath<ModulesInfo>($"{ModulesPath}/ModulesInfo.asset");

            // Setup ModulesManager scene object
            _modulesManager = FindObjectOfType<ModulesManager>();
            if (_modulesManager == null)
            {
                var mm = new GameObject("_ModulesManager");
                mm.AddComponent<ModulesManager>();
                _modulesManager = mm.GetComponent<ModulesManager>();
                _modulesManager.modulesInfo = modulesInfo;
            }
        }

        /// <summary>
        /// Initialize Module Generation
        /// </summary>
        private void ModuleGenerationInitialize()
        {
            var absoluteModulesPath =
                $"{Application.dataPath.Remove(Application.dataPath.Length - 7)}/{ModulesPath}";

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

                // TODO: Set ModulesInfo.asset dirty?
            }

            // Setup Modules scene
            var modulesScene = AssetDatabase.LoadAssetAtPath<SceneAsset>($"{folderPath}/Modules.unity");
            if (modulesScene == null)
            {
                throw new Exception(
                    "No scene named \"Modules\" found at path! You need to create a scene named \"Modules\" at \"{folderPath}\".");
            }

            // Open Modules scene
            EditorSceneManager.OpenScene($"{folderPath}/Modules.unity", OpenSceneMode.Single);

            _modulesSceneCount = FindObjectsOfType<ModuleVisualizer>().Length;

            // TODO: Check if Modules scene is dirty
            // if (SceneManager.GetActiveScene().isDirty)
            // {
            //     EditorUtility.DisplayDialog("Modules scene has unsaved changes!",
            //         "You need to save the scene before you can generate new Modules.", "Ok");
            //
            //     StopModuleGeneration();
            //     return;
            // }
        }

        private void OnGUI()
        {
            DrawGUI();

            if (_generating)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Generating WFC-Modules",
                    _progressBarInfo,
                    (float) _currentSourceIndex / modelSources.Length))
                {
                    // canceled progress
                }
            }
        }

        /// <summary>
        /// Draw Editor Window GUI
        /// </summary>
        private void DrawGUI()
        {
            var serialObj = new SerializedObject(this);
            var serialGenerateEmpty = serialObj.FindProperty("generateEmpty");
            var serialGenerateCell = serialObj.FindProperty("generateCell");
            var serialCell = serialObj.FindProperty("cell");
            var serialModels = serialObj.FindProperty("modelSources");

            _scrollPosEditor = EditorGUILayout.BeginScrollView(_scrollPosEditor, false, false);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label("Generate WFC-Modules from Model Sources", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.Label(
                $"Generated WFC-Modules data will be stored in the \"{ModulesPath}\" folder.",
                EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);

            folderPath = EditorGUILayout.TextField(new GUIContent("Folder Base Path"), folderPath);

            EditorGUILayout.PropertyField(serialGenerateEmpty,
                new GUIContent("Generate empty Module",
                    "Automatically generates an empty Module with none fitting faces."));

            EditorGUILayout.PropertyField(serialGenerateCell,
                new GUIContent("Generate Module cell",
                    "Automatically generate best fitting Module cell based on Model Sources' bounds."));

            if (!generateCell)
                EditorGUILayout.PropertyField(serialCell, new GUIContent("Module cell"));

            GUILayout.Space(15);

            EditorGUILayout.PropertyField(serialModels, true);

            GUILayout.Space(25);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Generate", GUILayout.Width(150), GUILayout.Height(25)))
            {
                if (_generating) return;

                _generating = true;

                try
                {
                    ModuleGenerationInitialize();
                    ModuleGenerationSetup();

                    // Create empty module
                    if (generateEmpty)
                    {
                        GenerateEmptyModule();
                    }

                    // Refresh Asset Database
                    AssetDatabase.Refresh();

                    // Start Module Generation
                    _currentSourceIndex = 0;
                    _progressBarInfo =
                        $"Generating from {modelSources[_currentSourceIndex].name} ({_currentSourceIndex + 1}/{modelSources.Length})";
                    ModuleGeneration.GenerateNextModule();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                    EditorUtility.DisplayDialog("Error!",
                        "There was an error setting up the Module Generation! Look at the console for more information.",
                        "Ok");
                    _generating = false;
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            GUILayout.Space(15);

            EditorGUILayout.EndScrollView();

            serialObj.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            ModuleGeneration.OnNextModuleData += ProcessNextModule;
        }

        private void OnDisable()
        {
            ModuleGeneration.OnNextModuleData -= ProcessNextModule;
        }

        [MenuItem("WFC Level Generation/Generate Modules")]
        public static void ShowWindow()
        {
            GetWindow<GenerateModulesEditor>(false, "Generate Modules");
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}