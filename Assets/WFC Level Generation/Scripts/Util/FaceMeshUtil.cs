using System;
using System.Collections.Generic;
using UnityEngine;
using WFCLevelGeneration.Util.Datastructures;

namespace WFCLevelGeneration.Util
{
    public static class FaceMeshUtil
    {
        private static readonly Vector3[] FaceNormals =
        {
            Vector3.forward, Vector3.up, Vector3.right, Vector3.back, Vector3.down, Vector3.left
        };

        /// <summary>
        /// Creates the 6 different face meshes of a given mesh in the mesh's local transform space.
        /// Note that the face meshes only contain vertices which actually touch the cell's bounds.
        /// </summary>
        /// <returns>The different face meshes (forward, up, right, back, down, left)</returns>
        public static ModuleVisualizer.ModuleFace[] GetFaceMeshes(Vector3[] mVertices, int[] mTriangles,
            Vector3[] mNormals, Vector3 mScale, Vector3 mPos, Vector3 cellScale)
        {
            var faces = new ModuleVisualizer.ModuleFace[6];

            const float lossOfFractionThreshold = 0.0001f;

            var cBounds = new Bounds(new Vector3(0, cellScale.y / 2, 0),
                cellScale);

            var faceMeshes = new FaceMesh[6];
            for (var i = 0; i < faceMeshes.Length; i++) faceMeshes[i] = new FaceMesh();

            // iterate over each triangle of the mesh
            for (var i = 0; i < mTriangles.Length;)
            {
                var indices = new[] {mTriangles[i++], mTriangles[i++], mTriangles[i++]};
                var vertices = new[] {mVertices[indices[0]], mVertices[indices[1]], mVertices[indices[2]]};
                var normals = new[] {mNormals[indices[0]], mNormals[indices[1]], mNormals[indices[2]]};

                var triangleNormal = (normals[0] + normals[1] + normals[2]) / 3;

                // apply mesh's local scale and local position to vertices
                for (var j = 0; j < vertices.Length; j++)
                {
                    vertices[j] = new Vector3(
                        vertices[j].x * mScale.x,
                        vertices[j].y * mScale.y,
                        vertices[j].z * mScale.z
                    );
                    vertices[j] += mPos;
                }

                var transformOriginToTriCenter = (vertices[0] + vertices[1] + vertices[2]) / 3;
                var transformCenterToTriCenter = transformOriginToTriCenter - cBounds.center;

                // skip triangles that don't lie on the cell's bounds
                if (Mathf.Abs(transformCenterToTriCenter.x) < cBounds.extents.x - lossOfFractionThreshold &&
                    Mathf.Abs(transformCenterToTriCenter.y) < cBounds.extents.y - lossOfFractionThreshold &&
                    Mathf.Abs(transformCenterToTriCenter.z) < cBounds.extents.z - lossOfFractionThreshold)
                    continue;

                SortTriangle(triangleNormal, vertices, faceMeshes);
            }

            // Apply face meshes
            for (var i = 0; i < faces.Length; i++)
            {
                var (vertices, triangles) = GenerateFaceMesh(faceMeshes[i].vertices, faceMeshes[i].triangles);

                faces[i] = new ModuleVisualizer.ModuleFace(vertices, triangles,
                    faceMeshes[i].mesh.GenerateFaceMeshHash(FaceNormals[i]));
            }

            return faces;
        }

        /// <summary>
        /// Generates 6 hashes (one for each face) for a given mesh.
        /// These face hashes do also include vertices that lie inside the cell's bounds.
        /// </summary>
        /// <returns></returns>
        public static int[] GetMeshpartHashes(Vector3[] mVertices, int[] mTriangles,
            Vector3[] mNormals, Vector3 mScale, Vector3 mPos)
        {
            var meshpartHashes = new int[6];

            var faceMeshes = new FaceMesh[6];
            for (var i = 0; i < faceMeshes.Length; i++) faceMeshes[i] = new FaceMesh();

            // iterate through all triangles in the mesh
            for (var i = 0; i < mTriangles.Length;)
            {
                var indices = new[] {mTriangles[i++], mTriangles[i++], mTriangles[i++]};
                var vertices = new[] {mVertices[indices[0]], mVertices[indices[1]], mVertices[indices[2]]};
                var normals = new[] {mNormals[indices[0]], mNormals[indices[1]], mNormals[indices[2]]};

                var triangleNormal = (normals[0] + normals[1] + normals[2]) / 3;

                // apply local scale to vertices
                for (var j = 0; j < vertices.Length; j++)
                {
                    vertices[j] = new Vector3(
                        vertices[j].x * mScale.x,
                        vertices[j].y * mScale.y,
                        vertices[j].z * mScale.z
                    );
                    vertices[j] += mPos;
                }

                SortTriangle(triangleNormal, vertices, faceMeshes);
            }

            // Apply face meshes
            for (var i = 0; i < meshpartHashes.Length; i++)
            {
                var (vertices, triangles) = GenerateFaceMesh(faceMeshes[i].vertices, faceMeshes[i].triangles);
                faceMeshes[i].mesh.vertices = vertices;
                faceMeshes[i].mesh.triangles = triangles;
                faceMeshes[i].mesh.RecalculateNormals();
                faceMeshes[i].mesh.RecalculateBounds();

                // calculate hash
                meshpartHashes[i] = faceMeshes[i].mesh.GenerateFaceMeshHash(FaceNormals[i]);
            }

            return meshpartHashes;
        }

        /// <summary>
        /// Sorts a triangle to it's right face
        /// </summary>
        /// <param name="triangleNormal">The triangles normal vector</param>
        /// <param name="vertices">The vertices of the triangle</param>
        /// <param name="faceMeshes">The array of <see cref="FaceMesh"/> objects</param>
        private static void SortTriangle(Vector3 triangleNormal, Vector3[] vertices, FaceMesh[] faceMeshes)
        {
            for (var j = 0; j < FaceNormals.Length; j++)
            {
                var angle = Vector3.Angle(FaceNormals[j], triangleNormal);

                // check if this is the correct face for the triangle
                if (angle <= 45)
                {
                    var actualVertIndices = new int[3];

                    for (var k = 0; k < vertices.Length; k++)
                        if (faceMeshes[j].vertices.ContainsKey(vertices[k]))
                        {
                            // vertex already exists --> get index
                            actualVertIndices[k] = faceMeshes[j].vertices[vertices[k]];
                        }
                        else
                        {
                            // add new vertex
                            var count = faceMeshes[j].vertices.Count;
                            faceMeshes[j].vertices.Add(vertices[k], count);
                            actualVertIndices[k] = count;
                        }

                    // add new triangle
                    faceMeshes[j].triangles.AddRange(new[]
                        {actualVertIndices[0], actualVertIndices[1], actualVertIndices[2]});

                    break;
                }
            }
        }

        private static Tuple<Vector3[], int[]> GenerateFaceMesh(LinkedHashMap<Vector3, int> vertices,
            List<int> triangles)
        {
            var verticesCount = vertices.Count;
            var verticesArr = new Vector3[verticesCount];

            for (var j = 0; j < verticesCount; j++)
            {
                var v = vertices.RemoveFirst().Key;
                verticesArr[j] = v;
            }

            return new Tuple<Vector3[], int[]>(verticesArr, triangles.ToArray());
        }

        private static int GenerateFaceMeshHash(this Mesh mesh, Vector3 meshAxis)
        {
            var hash = 0;
            var center = mesh.bounds.center;

            foreach (var v in mesh.vertices)
            {
                var dir = v - center;
                dir.Vector3AlignForwardAxis(meshAxis);
                hash ^= dir.GenerateVector3Hash();
            }

            return hash;
        }

        private static void Vector3AlignForwardAxis(this ref Vector3 dir, Vector3 curAxis)
        {
            if (curAxis == Vector3.up || curAxis == Vector3.down)
                dir = Quaternion.Euler(90 * curAxis.y, 0, 0) * dir;
            else if (curAxis == Vector3.left || curAxis == Vector3.right)
                dir = Quaternion.Euler(0, 90 * curAxis.x, 0) * dir;
        }

        private static int GenerateVector3Hash(this Vector3 vector)
        {
            return $"{vector.x:F3},{vector.y:F3},{vector.z:F3}".GetHashCode();
        }

        private class FaceMesh
        {
            public readonly Mesh mesh;
            public readonly LinkedHashMap<Vector3, int> vertices;
            public readonly List<int> triangles;

            public FaceMesh()
            {
                mesh = new Mesh();
                vertices = new LinkedHashMap<Vector3, int>();
                triangles = new List<int>();
            }
        }
    }
}