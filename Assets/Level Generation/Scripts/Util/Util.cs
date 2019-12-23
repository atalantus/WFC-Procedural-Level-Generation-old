using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LevelGeneration
{
    public static class Util
    {
        public static readonly Vector3[] faceNormals =
        {
            Vector3.forward, Vector3.up, Vector3.right, Vector3.back, Vector3.down, Vector3.left
        };

        /// <summary>
        /// Creates the 6 different face meshes of a given mesh.
        /// </summary>
        /// <param name="mesh">The mesh</param>
        /// <returns>The different face meshes (forward, up, right, back, down, left)</returns>
        public static ModuleVisualizer.ModuleFace[] GetFaceMeshes(Mesh mesh)
        {
            var mVertices = mesh.vertices;
            var mTriangles = mesh.triangles;
            var mNormals = mesh.normals;

            var meshes = new Mesh[6];
            var faces = new ModuleVisualizer.ModuleFace[6];

            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i] = new Mesh();
            }

            var fmVertices = new LinkedHashMap<Vector3, int>[6];
            for (int i = 0; i < fmVertices.Length; i++)
            {
                fmVertices[i] = new LinkedHashMap<Vector3, int>();
            }

            var fmTriangles = new List<int>[6];
            for (int i = 0; i < fmTriangles.Length; i++)
            {
                fmTriangles[i] = new List<int>();
            }

            for (int i = 0; i < mTriangles.Length;)
            {
                var indices = new[] {mTriangles[i++], mTriangles[i++], mTriangles[i++]};
                var vertices = new[] {mVertices[indices[0]], mVertices[indices[1]], mVertices[indices[2]]};
                // TODO: Not dependent on meshes local rotation
                var normals = new[] {mNormals[indices[0]], mNormals[indices[1]], mNormals[indices[2]]};

                var faceNormal = (normals[0] + normals[1] + normals[2]) / 3;

                // Sort triangle to right face
                for (int j = 0; j < faceNormals.Length; j++)
                {
                    float angle = Vector3.Angle(faceNormals[j], faceNormal);

                    if (angle <= 45)
                    {
                        // add triangle to this face

                        var actualVertIndices = new int[3];

                        for (int k = 0; k < vertices.Length; k++)
                        {
                            if (fmVertices[j].ContainsKey(vertices[k]))
                            {
                                // vertex already exists --> get index
                                actualVertIndices[k] = fmVertices[j][vertices[k]];
                            }
                            else
                            {
                                // add new vertex
                                var count = fmVertices[j].Count;
                                fmVertices[j].Add(vertices[k], count);
                                actualVertIndices[k] = count;
                            }
                        }

                        // add new triangle
                        fmTriangles[j]
                            .AddRange(new[] {actualVertIndices[0], actualVertIndices[1], actualVertIndices[2]});

                        break;
                    }
                }
            }

            // Apply face meshes
            for (int i = 0; i < faces.Length; i++)
            {
                var verticesCount = fmVertices[i].Count;
                var vertices = new Vector3[verticesCount];

                for (int j = 0; j < verticesCount; j++)
                {
                    vertices[j] = fmVertices[i].RemoveFirst().Key;
                }

                meshes[i].vertices = vertices;
                meshes[i].triangles = fmTriangles[i].ToArray();
                meshes[i].RecalculateNormals();
                meshes[i].RecalculateBounds();

                faces[i] = new ModuleVisualizer.ModuleFace(meshes[i], GenerateFaceHash(meshes[i], faceNormals[i]));
            }

            return faces;
        }

        public static int GenerateFaceHash(Mesh mesh, Vector3 meshAxis)
        {
            var hash = 0;
            var center = mesh.bounds.center;

            foreach (var v in mesh.vertices)
            {
                var dir = Vector3AlignAxis(v - center, meshAxis);
                hash ^= GenerateVector3Hash(dir);
            }

            return hash;
        }

        public static Vector3 Vector3AlignAxis(Vector3 dir, Vector3 curAxis)
        {
            var newDir = dir;
            if (curAxis == Vector3.up || curAxis == Vector3.down)
            {
                newDir = Quaternion.Euler(90 * curAxis.y, 0, 0) * newDir;
                return newDir;
            }

            if (curAxis == Vector3.left || curAxis == Vector3.right)
            {
                newDir = Quaternion.Euler(0, 90 * curAxis.x, 0) * newDir;
                return newDir;
            }

            return dir;
        }

        public static int GenerateVector3Hash(Vector3 vector)
        {
            return $"{vector.x:F3},{vector.y:F3},{vector.z:F3}".GetHashCode();
        }
    }
}