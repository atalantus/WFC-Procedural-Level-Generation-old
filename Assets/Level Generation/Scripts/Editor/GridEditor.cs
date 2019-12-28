using UnityEditor;
using UnityEngine;

namespace LevelGeneration
{
    [CustomEditor(typeof(Grid))]
    public class GridEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var grid = (Grid) target;
            
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Generate Level", GUILayout.Width(175), GUILayout.Height(25)))
            {
                grid.GenerateLevel();
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}