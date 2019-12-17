using System;
using UnityEditor;
using UnityEngine;

namespace LevelGeneration
{
    [CustomEditor(typeof(ModuleVisualizer))]
    class ButtonExampleEditor : Editor
    {
        void OnSceneGUI()
        {
            var moduleVisualizer = (ModuleVisualizer) target;

            if (moduleVisualizer.showHandles)
                ShowFaceHandles(moduleVisualizer);

            #region Scene UI

            Handles.color = Color.white;
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 150, 200), new GUIStyle(GUI.skin.box));

            if (moduleVisualizer.showHandles)
            {
                GUILayout.Box("Select a face");
            }
            
            if (GUILayout.Button("click me", GUILayout.Width(100)))
                Debug.Log("u clicked me");
            
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

        private void ShowFaceHandles(ModuleVisualizer moduleVisualizer)
        {
            var bounds = moduleVisualizer.Renderer.bounds;

            for (int i = 0; i < 6; i++)
            {
                var offset = new Vector3(
                    i % 3 == 2 ? bounds.extents.x : 0,
                    i % 3 == 1 ? bounds.extents.y : 0,
                    i % 3 == 0 ? bounds.extents.z : 0);

                var size = Mathf.Min(bounds.extents[i % 3], bounds.extents[(i + 1) % 3]) / 2;

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