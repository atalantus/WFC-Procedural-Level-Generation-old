﻿using System;
using System.Collections.Generic;
using LevelGeneration.WFC;
using UnityEditor;
using UnityEngine;

namespace LevelGeneration
{
    [ExecuteInEditMode]
    public class ModuleVisualizer : MonoBehaviour
    {
#if UNITY_EDITOR

        private delegate void OnFaceEventHandler(ModuleFace selectedFace);

        private delegate void OnModuleVariantsHandler(ModuleVisualizer caller);

        private static event OnFaceEventHandler OnFaceSelectEvent;
        private static event OnFaceEventHandler OnFaceDeselectEvent;

        private static event OnModuleVariantsHandler OnModuleVariantsShowEvent;
        private static event OnModuleVariantsHandler OnModuleVariantsHideEvent;

        private Mesh _modelMesh;
        private Renderer _renderer;
        private bool showVariants = false;
        public ModulesInfo modulesInfo;
        public int selectedFaceMesh = -1;
        private List<int> _shownFaceMeshes;
        public Module[] moduleAssets;
        public Cell cell;

        public Bounds ModuleBounds => new Bounds(
            transform.position + new Vector3(0, cell.transform.localScale.y / 2, 0),
            cell.transform.localScale);

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
            Debug.Log("Awake");
            
            selectedFaceMesh = -1;
            _shownFaceMeshes = new List<int>();
            moduleAssets = new Module[4];

            _modelMesh = GetComponentInChildren<MeshFilter>(true).sharedMesh;
            _renderer = GetComponentInChildren<Renderer>(true);

            OnFaceSelectEvent += face =>
            {
                for (var i = 0; i < faces.Length; i++)
                {
                    var moduleFace = faces[i];
                    if (moduleFace.GetHashCode() == face.GetHashCode()) _shownFaceMeshes.Add(i);
                }
            };

            OnFaceDeselectEvent += face => { _shownFaceMeshes = new List<int>(); };

            OnModuleVariantsShowEvent += caller =>
            {
                Debug.Log("Show event");
                if (caller != this)
                {
                    Renderer.enabled = false;
                }
            };

            OnModuleVariantsHideEvent += caller => { Renderer.enabled = true; };
        }

        private void OnDrawGizmos()
        {
            if (_shownFaceMeshes.Count > 0)
                DrawFaceMeshes();
            else if (showVariants)
                DrawVariantMeshes();
        }

        private void DrawFaceMeshes()
        {
            foreach (var i in _shownFaceMeshes)
            {
                if (faces[i].Mesh.vertexCount == 0)
                    // No face mesh for this face --> everything fits --> show in GUI
                    return;

                Gizmos.color = i == selectedFaceMesh ? Color.red : Color.blue;
                Gizmos.DrawWireMesh(faces[i].Mesh, transform.position, transform.rotation, Vector3.one);
            }
        }

        private void DrawVariantMeshes()
        {
            var offset = transform.position;

            // display module variants
            for (var i = 1; i < moduleAssets.Length; i++)
            {
                var variant = moduleAssets[i];
                if (variant == null) continue;

                offset.x += cell.transform.localScale.x * 1.5f;

                var meshFilter = variant.moduleGO.GetComponentInChildren<MeshFilter>();
                var meshTransform = meshFilter.transform;

                Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
                Gizmos.DrawMesh(meshFilter.sharedMesh, offset + meshTransform.localPosition, meshTransform.rotation,
                    meshTransform.localScale);

                var style = new GUIStyle {normal = {textColor = Color.black}, alignment = TextAnchor.MiddleCenter};
                Handles.Label(new Vector3(offset.x, offset.y + cell.transform.localScale.y + 0.5f, offset.z),
                    variant.name, style);
            }
        }

        public void SelectMeshFace(int i)
        {
            selectedFaceMesh = i;

            OnFaceSelectEvent?.Invoke(faces[selectedFaceMesh]);
        }

        public void DeselectMeshFace()
        {
            if (selectedFaceMesh == -1) return;

            OnFaceDeselectEvent?.Invoke(faces[selectedFaceMesh]);

            selectedFaceMesh = -1;
        }

        public void ShowModuleVariants()
        {
            showVariants = true;
            OnModuleVariantsShowEvent?.Invoke(this);
        }

        public void HideModuleVariants()
        {
            showVariants = false;
            OnModuleVariantsHideEvent?.Invoke(this);
        }

        public void UpdateModuleAssets(int faceId, int newHash)
        {
            // TODO: Apply new hash to module assets
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
                    _mesh = new Mesh {vertices = vertices, triangles = triangles};
                    _mesh.RecalculateNormals();
                    return _mesh;
                }
            }

            private Mesh _mesh;

            [SerializeField] private Vector3[] vertices;
            [SerializeField] public int[] triangles;
            [SerializeField] private int hash;

            public ModuleFace(Mesh mesh, int hash)
            {
                _mesh = mesh;
                vertices = mesh.vertices;
                triangles = mesh.triangles;

                this.hash = hash;
            }

            public override bool Equals(object obj)
            {
                return obj is ModuleFace other && Equals(other);
            }

            public bool Equals(ModuleFace other)
            {
                return Mesh.Equals(other.Mesh);
            }

            public void SetHashCode(int hash)
            {
                this.hash = hash;
            }

            public override int GetHashCode()
            {
                // Unity doesn't serialize readonly fields!
                return hash;
            }
        }
    }
}