using System;
using System.Collections.Generic;
using LevelGeneration.WFC;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace LevelGeneration
{
    [ExecuteInEditMode]
    [Serializable]
    public class ModuleVisualizer : MonoBehaviour
    {
#if UNITY_EDITOR

        private Mesh _modelMesh;
        private Renderer _renderer;
        private bool showVariants = false;
        private List<int> _shownFaceMeshes = new List<int>();

        public ModulesManager modulesManager;
        public ModulesInfo modulesInfo;
        public int selectedFaceMesh = -1;
        public Module[] moduleAssets = new Module[4];
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
            _modelMesh = GetComponentInChildren<MeshFilter>(true).sharedMesh;
            _renderer = GetComponentInChildren<Renderer>(true);
        }

        public void RegisterEvents()
        {
            UnityEventTools.AddPersistentListener(modulesManager.OnFaceSelectEvent, OnSelectMeshFace);
            UnityEventTools.AddPersistentListener(modulesManager.OnFaceDeselectEvent, OnDeselectMeshFace);
            UnityEventTools.AddVoidPersistentListener(modulesManager.OnModuleVariantsShowEvent, OnShowModuleVariants);
            UnityEventTools.AddVoidPersistentListener(modulesManager.OnModuleVariantsHideEvent, OnHideModuleVariants);

            modulesManager.OnFaceSelectEvent.SetPersistentListenerState(
                modulesManager.OnFaceSelectEvent.GetPersistentEventCount() - 1,
                UnityEventCallState.EditorAndRuntime);
            modulesManager.OnFaceDeselectEvent.SetPersistentListenerState(
                modulesManager.OnFaceDeselectEvent.GetPersistentEventCount() - 1,
                UnityEventCallState.EditorAndRuntime);
            modulesManager.OnModuleVariantsShowEvent.SetPersistentListenerState(
                modulesManager.OnModuleVariantsShowEvent.GetPersistentEventCount() - 1,
                UnityEventCallState.EditorAndRuntime);
            modulesManager.OnModuleVariantsHideEvent.SetPersistentListenerState(
                modulesManager.OnModuleVariantsHideEvent.GetPersistentEventCount() - 1,
                UnityEventCallState.EditorAndRuntime);
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

            modulesManager.OnFaceSelectEvent.Invoke(faces[selectedFaceMesh]);
        }

        public void OnSelectMeshFace(ModuleFace face)
        {
            for (var i = 0; i < faces.Length; i++)
            {
                var moduleFace = faces[i];
                if (moduleFace.GetHashCode() == face.GetHashCode()) _shownFaceMeshes.Add(i);
            }
        }

        public void DeselectMeshFace()
        {
            if (selectedFaceMesh == -1) return;

            modulesManager.OnFaceDeselectEvent.Invoke(faces[selectedFaceMesh]);

            selectedFaceMesh = -1;
        }

        public void OnDeselectMeshFace(ModuleFace face)
        {
            _shownFaceMeshes = new List<int>();
        }

        public void ShowModuleVariants()
        {
            showVariants = true;
            modulesManager.OnModuleVariantsShowEvent.Invoke();
            Renderer.enabled = true;
        }

        public void OnShowModuleVariants()
        {
            Renderer.enabled = false;
        }

        public void HideModuleVariants()
        {
            showVariants = false;
            modulesManager.OnModuleVariantsHideEvent.Invoke();
        }

        public void OnHideModuleVariants()
        {
            Renderer.enabled = true;
        }

        public void UpdateModuleAssets(int faceId, int newHash)
        {
            for (var i = 0; i < moduleAssets.Length; i++)
            {
                if (moduleAssets[i] == null) continue;

                int n;

                if (faceId % 3 == 1 || i == 0)
                    // TODO: Recalculate rotated top/bottom face hash code
                    n = faceId;
                else if (faceId % 3 == 0)
                    n = (faceId + 2 * Mathf.CeilToInt(i / 2f) + 1 * (i / 2)) % 6;
                else
                    n = (faceId + 1 * Mathf.CeilToInt(i / 2f) + 2 * (i / 2)) % 6;

                moduleAssets[i].faceConnections[n] = newHash;
                EditorUtility.SetDirty(moduleAssets[i]);
            }
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