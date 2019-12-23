using System;
using System.Collections.Generic;
using LevelGeneration;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Serialization;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(Renderer))]
public class ModuleVisualizer : MonoBehaviour
{
#if UNITY_EDITOR

    private Mesh _modelMesh;
    private Renderer _renderer;
    [HideInInspector] public bool showHandles = true;
    [HideInInspector] public int selectedFaceMesh = -1;

    /// <summary>
    /// different face meshes (forward, up, right, back, down, left)
    /// </summary>
    public ModuleFace[] faces;

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
    }

    private void OnDrawGizmos()
    {
        if (selectedFaceMesh >= 0)
            DrawFaceMesh(selectedFaceMesh);
    }

    private void DrawFaceMesh(int i)
    {
        if (faces == null || faces.Length < 6) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireMesh(faces[i].Mesh, transform.position, transform.rotation, transform.localScale);
    }

#endif

    [Serializable]
    public struct ModuleFace
    {
        public readonly Mesh Mesh;
        public readonly int Hash;

        public ModuleFace(Mesh mesh, int hash)
        {
            Mesh = mesh;
            Hash = hash;
        }

        public override int GetHashCode()
        {
            return Hash;
        }
    }
}