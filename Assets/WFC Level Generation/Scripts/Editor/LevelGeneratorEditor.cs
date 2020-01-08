using UnityEditor;
using UnityEngine;

namespace WFCLevelGeneration.Editor
{
    [CustomEditor(typeof(LevelGenerator), true)]
    public class LevelGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var levelGenerator = (LevelGenerator) target;

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Generate Level", GUILayout.Width(175), GUILayout.Height(25)))
                levelGenerator.GenerateLevel();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}