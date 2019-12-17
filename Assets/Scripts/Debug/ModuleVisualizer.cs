using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Serialization;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(Renderer))]
public class ModuleVisualizer : MonoBehaviour
{
    private Mesh _modelMesh;
    private Renderer _renderer;
    [HideInInspector]
    public bool showHandles = true;
    [HideInInspector]
    public int selectedFaceMesh = -1;

    /// <summary>
    /// different face meshes (forward, up, right, back, down, left)
    /// </summary>
    private KeyValuePair<int, Mesh>[] _faceMeshes;

    // (forward, up, right, back, down, left)
    public static readonly Vector3[] FmNormals =
    {
        Vector3.forward, Vector3.up, Vector3.right, Vector3.back, Vector3.down, Vector3.left
    };

    public Mesh ModelMesh
    {
        get
        {
            if (_modelMesh == null) _modelMesh = GetComponent<MeshFilter>().sharedMesh;
            return _modelMesh;
        }
    }

    public Renderer Renderer
    {
        get
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            return _renderer;
        }
    }

    private void Awake()
    {
        showHandles = true;
        selectedFaceMesh = -1;

        _modelMesh = GetComponent<MeshFilter>().sharedMesh;
        _renderer = GetComponent<Renderer>();

        GenerateFaceMeshes();
    }

    private void OnDrawGizmos()
    {
        if (selectedFaceMesh >= 0)
            DrawFaceMesh(selectedFaceMesh);
    }

    private void DrawFaceMesh(int i)
    {
        if (_faceMeshes == null) GenerateFaceMeshes();

        Gizmos.color = Color.red;
        Gizmos.DrawMesh(_faceMeshes[i].Value, transform.position, transform.rotation, Vector3.one);
    }

    private void GenerateFaceMeshes()
    {
        var mVertices = ModelMesh.vertices;
        var mTriangles = ModelMesh.triangles;
        var mNormals = ModelMesh.normals;

        _faceMeshes = new KeyValuePair<int, Mesh>[6];

        for (int i = 0; i < _faceMeshes.Length; i++)
        {
            _faceMeshes[i] = new KeyValuePair<int, Mesh>(0, new Mesh());
        }

        var fmVertices = new List<Vector3>[6];
        for (int i = 0; i < fmVertices.Length; i++)
        {
            fmVertices[i] = new List<Vector3>();
        }

        var fmTriangles = new List<int>[6];
        for (int i = 0; i < fmTriangles.Length; i++)
        {
            fmTriangles[i] = new List<int>();
        }

        for (int i = 0; i < mTriangles.Length;)
        {
            var i1 = mTriangles[i++];
            var i2 = mTriangles[i++];
            var i3 = mTriangles[i++];

            var v1 = mVertices[i1];
            var v2 = mVertices[i2];
            var v3 = mVertices[i3];

            var n1 = mNormals[i1];
            var n2 = mNormals[i2];
            var n3 = mNormals[i3];

            var faceNormal = (n1 + n2 + n3) / 3;

            // Sort triangle to right face
            for (int j = 0; j < FmNormals.Length; j++)
            {
                float angle = Vector3.Angle(FmNormals[j], faceNormal);

                if (angle <= 45)
                {
                    // found face triangle
                    fmVertices[j].AddRange(new[] {v1, v2, v3});
                    fmTriangles[j].AddRange(new[]
                        {fmVertices[j].Count - 3, fmVertices[j].Count - 2, fmVertices[j].Count - 1});
                }
            }
        }

        // Apply face meshes
        for (int i = 0; i < _faceMeshes.Length; i++)
        {
            _faceMeshes[i].Value.vertices = fmVertices[i].ToArray();
            _faceMeshes[i].Value.triangles = fmTriangles[i].ToArray();
            _faceMeshes[i].Value.RecalculateNormals();
        }
    }
}