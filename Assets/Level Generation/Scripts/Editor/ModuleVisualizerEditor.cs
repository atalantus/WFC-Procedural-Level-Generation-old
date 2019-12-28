using System;
using UnityEditor;
using UnityEngine;

namespace LevelGeneration
{
    [CustomEditor(typeof(ModuleVisualizer)), CanEditMultipleObjects]
    class ButtonExampleEditor : Editor
    {
        private readonly string[] _faceNames = {"Forward", "Up", "Right", "Back", "Down", "Left"};

        void OnSceneGUI()
        {
            var moduleVisualizer = (ModuleVisualizer) target;

            if (moduleVisualizer.showHandles)
                ShowFaceHandles(moduleVisualizer);

            #region Scene UI

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 185, 95), new GUIStyle(GUI.skin.box));

            if (moduleVisualizer.showHandles)
            {
                GUILayout.Label("Select a face", EditorStyles.boldLabel);
            }
            else
            {
                EditorStyles.label.wordWrap = true;
                GUILayout.Label(
                    $"Selected face: {_faceNames[moduleVisualizer.selectedFaceMesh]} ({moduleVisualizer.faces[moduleVisualizer.selectedFaceMesh].hash})",
                    EditorStyles.boldLabel);

                if (GUILayout.Button("Set adjacent to nothing", GUILayout.Width(175)))
                    Debug.Log("adjacent to nothing");

                if (GUILayout.Button("Set to new adjacency id", GUILayout.Width(175)))
                    Debug.Log("new adjacency id");
            }

            GUILayout.EndArea();
            Handles.EndGUI();

            #endregion
        }

        private void OnDisable()
        {
            var moduleVisualizer = (ModuleVisualizer) target;

            // deselected object
            moduleVisualizer.selectedFaceMesh = -1;
            moduleVisualizer.showHandles = true;
        }

        public override void OnInspectorGUI()
        {
            var moduleVisualizer = (ModuleVisualizer) target;

            GUILayout.Space(10);

            moduleVisualizer.moduleBottomCenterOffset = EditorGUILayout.Vector3Field("Bottom Center Offset:",
                moduleVisualizer.moduleBottomCenterOffset);

            GUILayout.Label($"{moduleVisualizer.faces.Length} Faces:", EditorStyles.boldLabel);
            for (var i = 0; i < moduleVisualizer.faces.Length; i++)
            {
                var face = moduleVisualizer.faces[i];
                GUILayout.BeginHorizontal();
                GUILayout.Space(25);
                GUILayout.Label($"{_faceNames[i]} ({face.hash})");
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Regenerate faces"))
            {
                moduleVisualizer.faces =
                    MeshGeneration.GetFaceMeshes(moduleVisualizer.ModelMesh,
                        moduleVisualizer.GetComponentInChildren<MeshFilter>(true).transform,
                        moduleVisualizer.cell == null ? Vector3.one : moduleVisualizer.cell.transform.localScale,
                        moduleVisualizer.moduleBottomCenterOffset);
            }
        }

        private void ShowFaceHandles(ModuleVisualizer moduleVisualizer)
        {
            var bounds = moduleVisualizer.ModuleBounds;
            for (int i = 0; i < 6; i++)
            {
                var offset = new Vector3(
                    i % 3 == 2 ? bounds.extents.x : 0,
                    i % 3 == 1 ? bounds.extents.y : 0,
                    i % 3 == 0 ? bounds.extents.z : 0
                );

                var size = Mathf.Max((Mathf.Min(bounds.extents[i % 3], bounds.extents[(i + 1) % 3]) / 2), 0.1f);

                var pos = bounds.center + (i > 2 ? -offset : offset);

                if (Handles.Button(pos, Quaternion.Euler(i % 3 == 1 ? 90 : 0, i % 3 == 2 ? 90 : 0, 0), size, size,
                    Handles.RectangleHandleCap))
                {
                    moduleVisualizer.showHandles = false;
                    moduleVisualizer.selectedFaceMesh = i;
                }
            }
        }
    }
}