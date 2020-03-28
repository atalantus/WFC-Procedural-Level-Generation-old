using UnityEngine;

namespace WFCLevelGeneration.Editor
{
    public class SourceData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector3[] normals;
        public Vector3 meshScale;
        public Vector3 meshPosition;
        public Vector3 cellScale;

        public SourceData(Vector3[] vertices, int[] triangles, Vector3[] normals, Vector3 meshScale, Vector3 meshPosition, Vector3 cellScale)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.normals = normals;
            this.meshScale = meshScale;
            this.meshPosition = meshPosition;
            this.cellScale = cellScale;
        }
    }
}