using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LevelGeneration
{
    public class MeshGeneration
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
        public static ModuleVisualizer.ModuleFace[] GetFaceMeshes(Mesh mesh, Transform transform, Vector3 cellScale)
        {
            var faces = new ModuleVisualizer.ModuleFace[6];

            const float lossOfFractionThreshold = 0.0001f;

            var mVertices = mesh.vertices;
            var mTriangles = mesh.triangles;
            var mNormals = mesh.normals;
            var mCenter = mesh.bounds.center;
            var mExtents = mesh.bounds.extents;
            var tLocalScale = transform.localScale;
            var cTransformCenter = cellScale / 2;
            Debug.Log(cTransformCenter);
            Debug.Log(mesh.bounds.center);
            Debug.Log(mesh.bounds.extents);
            Debug.Log(mesh.bounds.max);
            Debug.Log(mesh.bounds.min);

            var meshes = new Mesh[6];
            meshes.PopulateCollection();

            var fmVertices = new LinkedHashMap<Vector3, int>[6];
            fmVertices.PopulateCollection();

            var fmTriangles = new List<int>[6];
            fmTriangles.PopulateCollection();

            for (int i = 0; i < mTriangles.Length;)
            {
                var indices = new[] {mTriangles[i++], mTriangles[i++], mTriangles[i++]};
                var vertices = new[] {mVertices[indices[0]], mVertices[indices[1]], mVertices[indices[2]]};
                var normals = new[] {mNormals[indices[0]], mNormals[indices[1]], mNormals[indices[2]]};

                var transformOriginToTriCenter = (vertices[0] + vertices[1] + vertices[2]) / 3;
                var meshCenterToTriCenter = transformOriginToTriCenter - mCenter;

                // skip triangles that don't lie on the mesh bounds
                // check if triangle is not on mesh bounds (no coordinate equals extent)
                if (Mathf.Abs(meshCenterToTriCenter.x) < mExtents.x - lossOfFractionThreshold &&
                    Mathf.Abs(meshCenterToTriCenter.y) < mExtents.y - lossOfFractionThreshold &&
                    Mathf.Abs(meshCenterToTriCenter.z) < mExtents.z - lossOfFractionThreshold)
                {
                    continue;
                }

                var faceNormal = (normals[0] + normals[1] + normals[2]) / 3;

                // apply local scale to vertices
                for (int j = 0; j < vertices.Length; j++)
                {
                    vertices[j] = new Vector3(
                        vertices[j].x * tLocalScale.x,
                        vertices[j].y * tLocalScale.y,
                        vertices[j].z * tLocalScale.z
                    );
                }

                /*
                
                // TODO: Filter triangles not touching cell bounds
                transformOriginToTriCenter = (vertices[0] + vertices[1] + vertices[2]) / 3;
                var cTransformCenterRelative = cTransformCenter - transform.position;
                var transformCenterToTriCenter = transformOriginToTriCenter - cTransformCenterRelative;
                Debug.Log(
                    $"{transformOriginToTriCenter.x:F5}, {transformOriginToTriCenter.y:F5}, {transformOriginToTriCenter.z:F5}");
                Debug.Log(
                    $"{transformCenterToTriCenter.x:F5}, {transformCenterToTriCenter.y:F5}, {transformCenterToTriCenter.z:F5}");

                // skip triangles that don't lie on the mesh bounds
                // check if triangle is not on mesh bounds (no coordinate equals extent)
                if (Mathf.Abs(transformCenterToTriCenter.x) < cTransformCenter.x - lossOfFractionThreshold &&
                    Mathf.Abs(transformCenterToTriCenter.y) < cTransformCenter.y - lossOfFractionThreshold &&
                    Mathf.Abs(transformCenterToTriCenter.z) < cTransformCenter.z - lossOfFractionThreshold)
                {
                    Debug.LogWarning("Skip triangle!");
                    continue;
                }
                
                */

                // Sort triangle to right face
                for (int j = 0; j < faceNormals.Length; j++)
                {
                    var angle = Vector3.Angle(faceNormals[j], faceNormal);

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
                GenerateMesh(ref meshes[i], fmVertices[i], fmTriangles[i]);

                faces[i] = new ModuleVisualizer.ModuleFace(meshes[i], GenerateMeshHash(meshes[i], faceNormals[i]));
            }

            return faces;
        }

        public static int[] GetMeshpartHashes(Mesh mesh, Vector3 localScale)
        {
            var mVertices = mesh.vertices;
            var mTriangles = mesh.triangles;
            var mNormals = mesh.normals;

            var meshes = new Mesh[6];
            var meshpartHashes = new int[6];

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

            // iterate through all triangles in the mesh
            for (int i = 0; i < mTriangles.Length;)
            {
                var indices = new[] {mTriangles[i++], mTriangles[i++], mTriangles[i++]};
                var vertices = new[] {mVertices[indices[0]], mVertices[indices[1]], mVertices[indices[2]]};
                var normals = new[] {mNormals[indices[0]], mNormals[indices[1]], mNormals[indices[2]]};

                var triangleNormal = (normals[0] + normals[1] + normals[2]) / 3;

                // apply local scale to vertices
                for (int j = 0; j < vertices.Length; j++)
                {
                    vertices[j] = new Vector3(
                        vertices[j].x * localScale.x,
                        vertices[j].y * localScale.y,
                        vertices[j].z * localScale.z
                    );
                }

                // Sort triangle to right face
                for (int j = 0; j < faceNormals.Length; j++)
                {
                    var angle = Vector3.Angle(faceNormals[j], triangleNormal);

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
            for (int i = 0; i < meshpartHashes.Length; i++)
            {
                GenerateMesh(ref meshes[i], fmVertices[i], fmTriangles[i]);

                // calculate hash
                meshpartHashes[i] = GenerateMeshHash(meshes[i], faceNormals[i]);
            }

            return meshpartHashes;
        }

        private static void GenerateMesh(ref Mesh mesh, LinkedHashMap<Vector3, int> vertices, List<int> triangles)
        {
            var verticesCount = vertices.Count;
            var verticesArr = new Vector3[verticesCount];

            for (int j = 0; j < verticesCount; j++)
            {
                var v = vertices.RemoveFirst().Key;
                verticesArr[j] = v;
            }

            mesh.vertices = verticesArr;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private static int GenerateMeshHash(Mesh mesh, Vector3 meshAxis)
        {
            var hash = 0;
            var center = mesh.bounds.center;

            foreach (var v in mesh.vertices)
            {
                var dir = Vector3AlignForwardAxis(v - center, meshAxis);
                var h = GenerateVector3Hash(dir);
                hash ^= h;
            }

            return hash;
        }

        private static Vector3 Vector3AlignForwardAxis(Vector3 dir, Vector3 curAxis)
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

        private static int GenerateVector3Hash(Vector3 vector)
        {
            return $"{vector.x:F3},{vector.y:F3},{vector.z:F3}".GetHashCode();
        }
    }
}