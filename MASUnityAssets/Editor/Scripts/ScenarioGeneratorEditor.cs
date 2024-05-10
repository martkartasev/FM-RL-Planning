using System;
using UnityEditor;
using UnityEngine;

namespace Scripts.Map
{
    [CustomEditor(typeof(ScenarioGenerator))]
    public class ScenarioGeneratorEditor : Editor
    {
        private ScenarioGenerator myScript;

        private void OnEnable()
        {
            myScript = (ScenarioGenerator)target;
            myScript.Initialize();
        }

        public override void OnInspectorGUI()
        {
            var changed = DrawDefaultInspector();

            GUILayout.Space(15);

            GUILayout.Label("Circle Radius non 0 to generate circle");
            GUILayout.Label("Random seed 0 for random placement");
            
            if (GUILayout.Button("Generate Starts"))
            {
                myScript.GenerateStarts();
            }

            if (GUILayout.Button("Generate Targets"))
            {
                myScript.GenerateTargets();
            }
            
        }
    }
}