using WFCLevelGeneration.Util;

namespace WFCLevelGeneration.Editor
{
    public class ModuleGeneration
    {
        public delegate void NextModuleData(ModuleData data);

        public static event NextModuleData OnNextModuleData;

        public static void GenerateNextModule(object state)
        {
            var sData = state as SourceData;
            var mData = new ModuleData
            {
                faces = FaceMeshUtil.GetFaceMeshes(sData.vertices, sData.triangles, sData.normals, sData.meshScale,
                    sData.meshPosition, sData.cellScale),
                meshpartHashes = FaceMeshUtil.GetMeshpartHashes(sData.vertices, sData.triangles, sData.normals,
                    sData.meshScale,
                    sData.meshPosition)
            };

            OnNextModuleData?.Invoke(new ModuleData());
        }
    }
}