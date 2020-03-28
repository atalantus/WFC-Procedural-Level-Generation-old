using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        public string ModulesPath => folderPath + "/Modules";

        public ModulesManager _modulesManager;
        [SerializeField] public ModulesInfo modulesInfo;
        public bool _generating;
        public int _currentSourceIndex;
        public Action _processNextModule;

        /**
         * Window properties
         */
        private string _progressBarInfo = "";

        private Vector2 _scrollPosEditor;
        public int _modulesSceneCount = 0;

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
            EditorUtility.SetDirty(moduleAsset);
        }

        /// <summary>
        /// Setup Module Generation
        /// </summary>
        private void ModuleGenerationSetup()
        {
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
            modulesInfo = AssetDatabase.LoadAssetAtPath<ModulesInfo>($"{ModulesPath}/ModulesInfo.asset");
            if (modulesInfo == null)
            {
                // Create Module Connections Asset
                modulesInfo = CreateInstance<ModulesInfo>();
                AssetDatabase.CreateAsset(modulesInfo, $"{ModulesPath}/ModulesInfo.asset");

                EditorUtility.SetDirty(modulesInfo);
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

        /// <summary>
        /// Stops Module Generation process
        /// </summary>
        private void FinishModuleGeneration()
        {
            _progressBarInfo = "Finishing Module Generation...";

            _generating = false;

            // Write changes to disc
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Starts the generation of the next Module Data
        /// </summary>
        private void EnqueueNextModuleGeneration()
        {
            while (true)
            {
                ++_currentSourceIndex;

                if (_currentSourceIndex >= modelSources.Length)
                {
                    // DONE
                    FinishModuleGeneration();
                    return;
                }

                if (modelSources[_currentSourceIndex] == null) continue;

                var meshFilter = modelSources[_currentSourceIndex].GetComponentInChildren<MeshFilter>(true);

                if (meshFilter == null ||
                    modelSources[_currentSourceIndex].GetComponentInChildren<Renderer>(true) == null)
                {
                    Debug.LogWarning(
                        $"Skipped {modelSources[_currentSourceIndex].name} because it doesn't have a \"Mesh Filter\"" +
                        " or a \"Renderer\" component on itself or any of this children!");
                    continue;
                }

                _progressBarInfo =
                    $"Generating from {modelSources[_currentSourceIndex].name} ({_currentSourceIndex + 1}/{modelSources.Length})";

                var mesh = meshFilter.sharedMesh;
                var meshTransform = meshFilter.transform;

                Debug.Log("EnqueueNextModuleGeneration");

                // next module
                ThreadPool.QueueUserWorkItem(ModuleGeneration.GenerateNextModule,
                    new SourceData(this, mesh.vertices, mesh.triangles, mesh.normals,
                        meshTransform.localScale, meshTransform.localPosition,
                        cell.transform.localScale));
                return;
            }
        }

        /// <summary>
        /// Gets the 3d world position for the next master prefab
        /// </summary>
        /// <returns>Position for next master prefab</returns>
        public Vector3 GetNextLayoutPos(Vector2 stepSize, int n)
        {
            var pos = new Vector3();

            var rc = Mathf.CeilToInt((float) Math.Sqrt(n));
            var rcc = Mathf.Pow(rc, 2);
            var abs = Mathf.Abs(rcc - n);
            var z = Mathf.CeilToInt(abs / 2f);

            var x = rc - z * (abs % 2);
            var y = rc - z * (1 - abs % 2);

            pos.x = x * (stepSize.x * 1.5f);
            pos.z = -y * (stepSize.y * 1.5f);

            return pos;
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
                    FinishModuleGeneration();
                }

                if (_processNextModule != null)
                {
                    _processNextModule();
                    _processNextModule = null;
                    EnqueueNextModuleGeneration();
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
                _currentSourceIndex = -1;

                EnqueueNextModuleGeneration();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            GUILayout.Space(15);

            EditorGUILayout.EndScrollView();

            serialObj.ApplyModifiedProperties();
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