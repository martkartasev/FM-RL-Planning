using UnityEditor;
using UnityEngine;

namespace Scripts.Map
{
    [CustomEditor(typeof(MapManager))]
    public class MapManagerEditor : Editor
    {
        public bool testVariable = true;

        private void OnEnable()
        {
            MapManager myScript = (MapManager)target;
            myScript.Initialize();
            ((MapManager)target).SyncObjectsToPositions();
        }


        public override void OnInspectorGUI()
        {
            var changed = DrawDefaultInspector();

            MapManager myScript = (MapManager)target;
         //   myScript.SyncPositionsToObjects();

            if (changed)
            {
                myScript.Initialize();
            }

            GUILayout.Space(15);

            if (GUILayout.Button("Save") && myScript.fileName.Length > 0)
            {
                myScript.SaveMap();
            }

            if (GUILayout.Button("Load") && myScript.fileName.Length > 0)
            {
                myScript.LoadMap();
            }

            if (GUILayout.Button("Load Legacy") && myScript.fileName.Length > 0)
            {
                myScript.LoadLegacyMap();
            }

            if (GUILayout.Button("Clear"))
            {
                myScript.ClearMap();
            }

            GUILayout.Space(15);

            if (GUILayout.Button("Instantiate Starts"))
            {
                myScript.InstantiateStarts();
            }

            if (GUILayout.Button("Clear Starts"))
            {
                myScript.ClearStarts();
            }

            if (GUILayout.Button("Instantiate Targets"))
            {
                myScript.InstantiateTargets();
            }

            if (GUILayout.Button("Clear Targets"))
            {
                myScript.ClearTargets();
            }
        }
    }
}