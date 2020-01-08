using System;
using System.IO;
using WFCLevelGeneration.Util;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WFCLevelGeneration.Editor
{
    public class ModuleGeneration : EditorWindow
    {
        public string folderPath = "Assets/WFC Level Generation";
        public Cell cell;
        public GameObject[] modelSources;

        private string ModulesPath => folderPath + "/Modules";
        private ModulesManager _modulesManager;
        [SerializeField] private ModulesInfo modulesInfo;
        private Action _generateModules;
        private bool _generating;
        private int _i;

        private string _progressBarInfo = "";
        private Vector2 _scrollPosEditor;
        private int _modulesSceneCount = 0;

        private ModulesInfo ModulesInfo
        {
            get
            {
                if (modulesInfo != null) return modulesInfo;

                var m = AssetDatabase.LoadAssetAtPath<ModulesInfo>($"{ModulesPath}/ModulesInfo.asset");
                if (m == null)
                {
                    Directory.CreateDirectory(
                        $"{Application.dataPath.Remove(Application.dataPath.Length - 7)}/{ModulesPath}");
                    AssetDatabase.CreateAsset(CreateInstance<ModulesInfo>(), $"{ModulesPath}/ModulesInfo.asset");
                }
                else
                {
                    modulesInfo = m;
                    return modulesInfo;
                }

                modulesInfo = AssetDatabase.LoadAssetAtPath<ModulesInfo>($"{ModulesPath}/ModulesInfo.asset");
                return modulesInfo;
            }
        }

        #region Unity Events

        [MenuItem("WFC Level Generation/Generate Modules")]
        public static void ShowWindow()
        {
            GetWindow<ModuleGeneration>(false, "Generate Modules");
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
                    // Create Module Connections Asset
                    AssetDatabase.CreateAsset(CreateInstance<ModulesInfo>(), $"{ModulesPath}/ModulesInfo.asset");
                }

                // Setup Modules scene
                var modulesScene = AssetDatabase.LoadAssetAtPath<SceneAsset>($"{folderPath}/Modules.unity");
                if (modulesScene == null)
                {
                    EditorUtility.DisplayDialog("No scene named \"Modules\" found at path!",
                        $"You need to create a scene named \"Modules\" at \"{folderPath}\".", "Ok");

                    StopModuleGeneration();
                    return;
                }

                // Open Modules scene
                EditorSceneManager.OpenScene($"{folderPath}/Modules.unity", OpenSceneMode.Single);

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

                // Get ModulesManager
                GetModulesManager();

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

        private void GetModulesManager()
        {
            _modulesManager = FindObjectOfType<ModulesManager>();
            if (_modulesManager == null)
            {
                var mm = new GameObject("_ModulesManager");
                mm.AddComponent<ModulesManager>();
                _modulesManager = mm.GetComponent<ModulesManager>();
                _modulesManager.modulesInfo = ModulesInfo;
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
            for (var j = 0; j < moduleAsset.faceConnections.Length; j++) moduleAsset.faceConnections[j] = 0;

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

                for (var i = 0; i < 4; i++)
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
                            FaceMeshUtil.GetFaceMeshes(modelMesh, meshFilter.transform, cell.transform.localScale);
                        meshpartHashes =
                            FaceMeshUtil.GetMeshpartHashes(modelMesh, meshFilter.transform);

                        for (var j = 0; j < faces.Length; j++) ModulesInfo.AddFace(faces[j].faceHash);

                        // Create master prefab
                        masterPrefab = PrefabUtility.InstantiatePrefab(modelSources[_i]) as GameObject;
                        masterPrefab.transform.parent = _modulesManager.transform;
                        if (masterPrefab != null)
                        {
                            masterVisualizer = masterPrefab.AddComponent<ModuleVisualizer>();
                            masterVisualizer.faces = faces;
                            masterVisualizer.cell = cell;
                            masterVisualizer.modulesInfo = ModulesInfo;
                            masterVisualizer.modulesManager = _modulesManager;
                            masterVisualizer.RegisterEvents();
                        }

                        var localScale = cell.transform.localScale;
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

                        if (i == 3 && (masterVisualizer.moduleAssets[1] == null ||
                                       masterVisualizer.moduleAssets[2] == null))
                            // 270° rotated version would not differ 90° variant
                            continue;
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
                    for (var j = 0; j < moduleAsset.faceConnections.Length; j++)
                    {
                        if (faces == null) return;

                        var n = j;

                        if (j % 3 == 1 || i == 0)
                            // TODO: Recalculate rotated top/bottom face hash code
                            n = j;
                        else if (j % 3 == 0)
                            n = (j + 2 * Mathf.CeilToInt(i / 2f) + 1 * (i / 2)) % 6;
                        else
                            n = (j + 1 * Mathf.CeilToInt(i / 2f) + 2 * (i / 2)) % 6;

                        moduleAsset.faceConnections[n] = faces[j].faceHash;
                    }

                    masterVisualizer.moduleAssets[i] = moduleAsset;

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
                $"Generated WFC-Modules data will be stored in the \"{ModulesPath}\" folder.",
                EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);

            folderPath = EditorGUILayout.TextField(new GUIContent("Folder Path"), folderPath);

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

                SetupGenerateModules();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            GUILayout.Space(15);

            EditorGUILayout.EndScrollView();

            serialObj.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the progress bar
        /// </summary>
        private void DisplayProgressBar()
        {
            if (EditorUtility.DisplayCancelableProgressBar("Generating WFC-Modules",
                _progressBarInfo,
                (float) _i / modelSources.Length))
                _generating = false;
        }

        #endregion
    }
}