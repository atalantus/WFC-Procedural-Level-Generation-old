using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LevelGeneration
{
    public class ModuleGeneration : EditorWindow
    {
        public Cell cell;
        public Vector3 modelBottomCenterOffset;
        public GameObject[] modelSources;

        private const string ModulesPath = "Assets/Level Generation/Modules";

        private ModulesInfo _modulesInfo;
        private Action _generateModules;
        private bool _generating;
        private int _i;

        private string _progressBarInfo = "";
        private Vector2 _scrollPosScene;
        private Vector2 _scrollPosEditor;
        private int _modulesSceneCount = 0;

        public ModulesInfo ModulesInfo
        {
            get
            {
                if (_modulesInfo != null) return _modulesInfo;

                var m = AssetDatabase.LoadAssetAtPath<ModulesInfo>($"{ModulesPath}/ModulesInfo.asset");
                if (m == null)
                {
                    Directory.CreateDirectory(
                        $"{Application.dataPath.Remove(Application.dataPath.Length - 7)}/{ModulesPath}");
                    AssetDatabase.CreateAsset(CreateInstance<ModulesInfo>(), $"{ModulesPath}/ModulesInfo.asset");
                }
                else
                {
                    _modulesInfo = m;
                    return _modulesInfo;
                }

                _modulesInfo = AssetDatabase.LoadAssetAtPath<ModulesInfo>($"{ModulesPath}/ModulesInfo.asset");
                return _modulesInfo;
            }
        }

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
                if (modelSources == null || cell == null || _i == modelSources.Length)
                {
                    StopModuleGeneration();
                }
                else
                {
                    _progressBarInfo = $"Generating from {modelSources[_i].name} ({_i}/{modelSources.Length})";
                    DisplayProgressBar();
                    _generateModules();

                    if (_i == modelSources.Length)
                    {
                        _progressBarInfo = "Finishing Module Generation...";
                        DisplayProgressBar();
                    }
                }
            }
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
                // Make sure the folder structure is correctly set up
                Directory.CreateDirectory(absoluteModulesPath);
                Directory.CreateDirectory($"{absoluteModulesPath}/Assets");
                Directory.CreateDirectory($"{absoluteModulesPath}/Assets/Variants");
                Directory.CreateDirectory($"{absoluteModulesPath}/Prefabs");
                Directory.CreateDirectory($"{absoluteModulesPath}/Prefabs/Variants");

                // Setup ModulesInfo asset
                if (AssetDatabase.LoadAssetAtPath<ModulesInfo>($"{ModulesPath}/ModulesInfo.asset") == null)
                {
                    Debug.Log("Create Info Asset.");
                    // Create Module Connections Asset
                    AssetDatabase.CreateAsset(CreateInstance<ModulesInfo>(), $"{ModulesPath}/ModulesInfo.asset");
                }

                // Setup Modules scene
                var modulesScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Level Generation/Modules.unity");
                if (modulesScene == null)
                {
                    EditorUtility.DisplayDialog("No scene named \"Modules\" found at path!",
                        "You need to create a scene named \"Modules\" at \"Assets/Level Generation/\".", "Ok");

                    StopModuleGeneration();
                    return;
                }

                // Open Modules scene
                EditorSceneManager.OpenScene("Assets/Level Generation/Modules.unity", OpenSceneMode.Single);

                _modulesSceneCount = FindObjectsOfType<ModuleVisualizer>().Length;

                // Check if Modules scene is dirty
                // if (SceneManager.GetActiveScene().isDirty)
                // {
                //     EditorUtility.DisplayDialog("Modules scene has unsaved changes!",
                //         "You need to save the scene before you can generate new Modules.", "Ok");
                //
                //     StopModuleGeneration();
                //     return;
                // }

                // Create empty module
                CreateEmptyModule();

                // Refresh Asset Database
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

        private void CreateEmptyModule()
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
            for (int j = 0; j < moduleAsset.faceConnections.Length; j++)
            {
                moduleAsset.faceConnections[j] = 0;
            }

            // Mark asset as dirty
            EditorUtility.SetDirty(moduleAsset);
        }

        /// <summary>
        /// Generates the next module
        /// </summary>
        private void GenerateModule()
        {
            try
            {
                var rotation = Vector3.zero;
                GameObject masterPrefab = null;
                ModuleVisualizer masterVisualizer = null;
                ModuleVisualizer.ModuleFace[] faces = null;
                int[] meshpartHashes = null;

                for (int i = 0; i < 4; i++)
                {
                    var moduleName = rotation == Vector3.zero
                        ? modelSources[_i].name
                        : $"{modelSources[_i].name} ({rotation.y})";
                    var variantPath = rotation == Vector3.zero ? "" : "Variants/";

                    if (i == 0)
                    {
                        var meshFilter = modelSources[_i].GetComponentInChildren<MeshFilter>(true);

                        if (meshFilter == null || modelSources[_i].GetComponentInChildren<Renderer>(true) == null)
                        {
                            Debug.LogWarning(
                                $"Skipped {modelSources[_i].name} because it doesn't have a \"Mesh Filter\"" +
                                " or a \"Renderer\" component on itself or any of this children!");
                            ++_i;
                            return;
                        }

                        var modelMesh = meshFilter.sharedMesh;
                        faces =
                            MeshGeneration.GetFaceMeshes(modelMesh, meshFilter.transform,
                                cell.transform.localScale, modelBottomCenterOffset);
                        meshpartHashes =
                            MeshGeneration.GetMeshpartHashes(modelMesh, meshFilter.transform.localScale);

                        for (int j = 0; j < faces.Length; j++)
                        {
                            ModulesInfo.AddFace(false, faces[j].GetHashCode());
                        }

                        // Create master prefab
                        masterPrefab = PrefabUtility.InstantiatePrefab(modelSources[_i]) as GameObject;
                        if (masterPrefab != null)
                        {
                            masterVisualizer = masterPrefab.AddComponent<ModuleVisualizer>();
                            masterVisualizer.faces = faces;
                            masterVisualizer.cell = cell;
                            masterVisualizer.moduleBottomCenterOffset = modelBottomCenterOffset;
                        }

                        var localScale = meshFilter.transform.localScale;
                        masterPrefab.transform.position = GetNextLayoutPos(new Vector2(localScale.x, localScale.z));

                        EditorUtility.SetDirty(masterPrefab);
                    }
                    else if (meshpartHashes != null)
                    {
                        if (i == 1 && meshpartHashes[0] == meshpartHashes[2] &&
                            meshpartHashes[3] == meshpartHashes[5])
                        {
                            // 90° rotated version would not differ to original
                            rotation = new Vector3(0, rotation.y + 90, 0);
                            continue;
                        }

                        if (i == 2 && meshpartHashes[0] == meshpartHashes[3] &&
                            meshpartHashes[2] == meshpartHashes[5])
                        {
                            // 180° rotated version would not differ to original
                            rotation = new Vector3(0, rotation.y + 90, 0);
                            continue;
                        }

                        if (i == 3 && masterVisualizer.moduleAssets.Count <= 2)
                        {
                            // 270° rotated version would not differ 90° variant
                            continue;
                        }
                    }

                    var moduleAsset = CreateInstance<Module>();

                    // Create asset
                    AssetDatabase.CreateAsset(moduleAsset,
                        $"{ModulesPath}/Assets/{variantPath}{moduleName}.asset");

                    // Create prefab variant
                    var prefabVariant = PrefabUtility.SaveAsPrefabAsset(masterPrefab,
                        $"{ModulesPath}/Prefabs/{variantPath}{moduleName}.prefab");
                    prefabVariant.GetComponentInChildren<MeshFilter>().transform.rotation = Quaternion.Euler(rotation);

                    // Assign asset values
                    moduleAsset.moduleGO = prefabVariant;
                    for (int j = 0; j < moduleAsset.faceConnections.Length; j++)
                    {
                        if (faces == null) return;

                        int n;

                        if (j % 3 == 1 || i == 0)
                        {
                            // TODO: Recalculate rotated top/bottom face hash code
                            n = j;
                        }
                        else
                            n = (j + i + (i == 3 ? 2 : 1)) % 6;

                        moduleAsset.faceConnections[j] = faces[n].GetHashCode();
                    }

                    masterVisualizer.moduleAssets.Add(moduleAsset);

                    // Mark asset as dirty
                    EditorUtility.SetDirty(moduleAsset);

                    rotation = new Vector3(0, rotation.y + 90, 0);
                }

                ++_i;
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
            _progressBarInfo = "Finishing Module Generation...";

            // Finished generating modules
            _generating = false;
            _i = 0;
            _generateModules = null;

            // Write changes to disc
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            EditorUtility.ClearProgressBar();
        }

        private Vector3 GetNextLayoutPos(Vector2 stepSize)
        {
            var pos = new Vector3();

            var n = ++_modulesSceneCount;

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

        #endregion

        #region GUI

        /// <summary>
        /// Draw Editor Window GUI
        /// </summary>
        private void DrawGUI()
        {
            var serialObj = new SerializedObject(this);
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
                "Generated WFC-Modules data will be stored in the \"Assets/Level Generation/Modules\" folder.",
                EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);

            EditorGUILayout.PropertyField(serialCell, new GUIContent("Module cell"));

            modelBottomCenterOffset =
                EditorGUILayout.Vector3Field(
                    new GUIContent("Model's Bottom Center Offset:",
                        "The offset between of the mesh's transform position to it's bottom center point"),
                    modelBottomCenterOffset);

            GUILayout.Space(15);

            EditorGUILayout.PropertyField(serialModels, true);

            GUILayout.Space(25);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Generate", GUILayout.Width(150), GUILayout.Height(25)))
            {
                if (_generating) return;

                _generating = true;

                SetupGenerateModules();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            GUILayout.Space(15);

            EditorGUILayout.EndScrollView();

            serialObj.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw Editor Window's scene GUI
        /// </summary>
        /// <param name="sceneView"></param>
        private void OnSceneGUI(SceneView sceneView)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(Screen.width - 160, Screen.height - 295, 150, 250),
                new GUIStyle(GUI.skin.box));

            GUILayout.Label("Face Filters:", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Toggle all", GUILayout.Width(100)))
            {
                Debug.Log("Toggle all");
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            if (ModulesInfo != null)
            {
                _scrollPosScene = GUILayout.BeginScrollView(_scrollPosScene, false, false, GUILayout.Width(146),
                    GUILayout.Height(200));

                foreach (var faceConnection in ModulesInfo.generatedConnections)
                {
                    GUILayout.Toggle(false, $"{faceConnection.Key} ({faceConnection.Value})");
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

        /// <summary>
        /// Draws the progress bar
        /// </summary>
        private void DisplayProgressBar()
        {
            if (EditorUtility.DisplayCancelableProgressBar("Generating WFC-Modules",
                _progressBarInfo,
                (float) _i / modelSources.Length))
            {
                _generating = false;
            }
        }

        #endregion
    }
}