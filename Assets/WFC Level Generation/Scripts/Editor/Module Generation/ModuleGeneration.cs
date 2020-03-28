using UnityEditor;
using UnityEngine;
using WFCLevelGeneration.Util;

namespace WFCLevelGeneration.Editor
{
    public static class ModuleGeneration
    {
        public static void GenerateNextModule(object state)
        {
            Debug.Log("GenerateNextModule");

            var sData = state as SourceData;
            var mData = new ModuleData
            {
                faces = FaceMeshUtil.GetFaceMeshes(sData.vertices, sData.triangles, sData.normals, sData.meshScale,
                    sData.meshPosition, sData.cellScale),
                meshpartHashes = FaceMeshUtil.GetMeshpartHashes(sData.vertices, sData.triangles, sData.normals,
                    sData.meshScale,
                    sData.meshPosition)
            };

            // THIS GET'S EXECUTED IN UNITYS MAIN THREAD!
            sData.generateModules._processNextModule = () =>
            {
                Debug.Log("Process Next Module");

                var rotation = Vector3.zero;

                for (var j = 0; j < mData.faces.Length; j++)
                    sData.generateModules.modulesInfo.AddFace(mData.faces[j].faceHash);

                // Create master prefab
                var masterPrefab =
                    PrefabUtility.InstantiatePrefab(
                        sData.generateModules.modelSources[sData.generateModules._currentSourceIndex]) as GameObject;
                masterPrefab.transform.parent = sData.generateModules._modulesManager.transform;

                // Add visualizer component to master prefab
                var masterVisualizer = masterPrefab.AddComponent<ModuleVisualizer>();
                masterVisualizer.faces = mData.faces;
                masterVisualizer.cell = sData.generateModules.cell;
                masterVisualizer.modulesInfo = sData.generateModules.modulesInfo;
                masterVisualizer.modulesManager = sData.generateModules._modulesManager;
                masterVisualizer.RegisterEvents();

                var localScale = sData.generateModules.cell.transform.localScale;
                masterPrefab.transform.position = sData.generateModules.GetNextLayoutPos(
                    new Vector2(localScale.x, localScale.z),
                    ++sData.generateModules._modulesSceneCount);

                EditorUtility.SetDirty(masterPrefab);

                for (var i = 0; i < 4; i++)
                {
                    // Check if rotation is necessary
                    if (i != 0 && mData.meshpartHashes != null)
                    {
                        if (i == 1 && mData.meshpartHashes[0] == mData.meshpartHashes[2] &&
                            mData.meshpartHashes[3] == mData.meshpartHashes[5])
                        {
                            // 90° rotated version would not differ to original -> skip
                            rotation = new Vector3(0, rotation.y + 90, 0);
                            continue;
                        }

                        if (i == 2 && mData.meshpartHashes[0] == mData.meshpartHashes[3] &&
                            mData.meshpartHashes[2] == mData.meshpartHashes[5])
                        {
                            // 180° rotated version would not differ to original -> skip
                            rotation = new Vector3(0, rotation.y + 90, 0);
                            continue;
                        }

                        if (i == 3 && (masterVisualizer.moduleAssets[1] == null ||
                                       masterVisualizer.moduleAssets[2] == null))
                            // 270° rotated version would not differ 90° variant -> skip
                            continue;
                    }

                    var moduleName = rotation == Vector3.zero
                        ? sData.generateModules.modelSources[sData.generateModules._currentSourceIndex].name
                        : $"{sData.generateModules.modelSources[sData.generateModules._currentSourceIndex].name} ({rotation.y})";
                    var variantPath = rotation == Vector3.zero ? "" : "Variants/";

                    var moduleAsset = ScriptableObject.CreateInstance<Module>();

                    // Create asset
                    AssetDatabase.CreateAsset(moduleAsset,
                        $"{sData.generateModules.ModulesPath}/Assets/{variantPath}{moduleName}.asset");

                    // Create prefab variant
                    var prefabVariant = PrefabUtility.SaveAsPrefabAsset(masterPrefab,
                        $"{sData.generateModules.ModulesPath}/Prefabs/{variantPath}{moduleName}.prefab");
                    prefabVariant.GetComponentInChildren<MeshFilter>().transform.rotation = Quaternion.Euler(rotation);

                    // Assign asset values
                    moduleAsset.moduleGO = prefabVariant;
                    for (var j = 0; j < moduleAsset.faceConnections.Length; j++)
                    {
                        if (mData.faces == null) return;

                        var n = j;

                        if (j % 3 == 1 || i == 0)
                            // TODO: Recalculate rotated top/bottom face hash code
                            n = j;
                        else if (j % 3 == 0)
                            n = (j + 2 * Mathf.CeilToInt(i / 2f) + 1 * (i / 2)) % 6;
                        else
                            n = (j + 1 * Mathf.CeilToInt(i / 2f) + 2 * (i / 2)) % 6;

                        moduleAsset.faceConnections[n] = mData.faces[j].faceHash;
                    }

                    masterVisualizer.moduleAssets[i] = moduleAsset;

                    // Mark asset as dirty
                    EditorUtility.SetDirty(moduleAsset);

                    // set next rotation
                    rotation = new Vector3(0, rotation.y + 90, 0);
                }
            };
        }
    }
}