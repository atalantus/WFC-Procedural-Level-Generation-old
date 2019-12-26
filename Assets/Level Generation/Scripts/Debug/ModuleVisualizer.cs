using System;
using System.Collections.Generic;
using LevelGeneration;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Serialization;

namespace LevelGeneration
{
    [ExecuteInEditMode]
    public class ModuleVisualizer : MonoBehaviour
    {
#if UNITY_EDITOR

        private Mesh _modelMesh;
        private Renderer _renderer;
        [HideInInspector] public bool showHandles = true;
        [HideInInspector] public int selectedFaceMesh = -1;
        [HideInInspector] public List<Module> moduleAssets;

        /// <summary>
        /// different face meshes (forward, up, right, back, down, left)
        /// </summary>
        public ModuleFace[] faces;

        public Mesh ModelMesh
        {
            get
            {
                if (_modelMesh == null) _modelMesh = GetComponentInChildren<MeshFilter>(true).sharedMesh;
                return _modelMesh;
            }
        }

        public Renderer Renderer
        {
            get
            {
                if (_renderer == null) _renderer = GetComponentInChildren<Renderer>(true);
                return _renderer;
            }
        }

        private void Awake()
        {
            showHandles = true;
            selectedFaceMesh = -1;
            moduleAssets = new List<Module>();

            _modelMesh = GetComponentInChildren<MeshFilter>(true).sharedMesh;
            _renderer = GetComponentInChildren<Renderer>(true);
        }

        private void OnDrawGizmos()
        {
            if (selectedFaceMesh >= 0)
                DrawFaceMesh(selectedFaceMesh);
        }

        private void DrawFaceMesh(int i)
        {
            if (faces == null || faces.Length < 6)
                faces = MeshGeneration.GetFaceMeshes(ModelMesh, GetComponentInChildren<MeshFilter>(true).transform);

            if (faces[i].Mesh.vertexCount == 0)
            {
                // No face mesh for this face --> everything fits --> show in GUI
                return;
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireMesh(faces[i].Mesh, transform.position, transform.rotation, Vector3.one);
        }

#endif

        [Serializable]
        public struct ModuleFace
        {
            public Mesh Mesh
            {
                get
                {
                    if (_mesh != null) return _mesh;
                    _mesh = new Mesh {vertices = _vertices, triangles = _triangles};
                    _mesh.RecalculateNormals();
                    return _mesh;
                }
            }

            private Mesh _mesh;
            private readonly Vector3[] _vertices;
            private readonly int[] _triangles;

            // Needs to be serialized by unity so it can't be readonly
            public int hash;

            public ModuleFace(Mesh mesh, int hash)
            {
                _mesh = mesh;
                _vertices = mesh.vertices;
                _triangles = mesh.triangles;

                this.hash = hash;
            }

            public override int GetHashCode()
            {
                return hash;
            }
        }
    }
}