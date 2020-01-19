using UnityEditor;
using UnityEngine;

namespace WFCLevelGeneration.Editor
{
    [CustomEditor(typeof(WFCBase), true)]
    public class WFCBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var levelGenerator = (WFCBase) target;

            GUILayout.Label(new GUIContent($"Cells Histories ({levelGenerator.cellHistories.Count} entries)",
                "The wfc algorithms cell history."));

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