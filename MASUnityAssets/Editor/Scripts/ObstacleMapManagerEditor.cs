using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Scripts.Map
{
    [CustomEditor(typeof(ObstacleMapManager))]
    public class ObstacleMapManagerEditor : Editor
    {
        private void OnEnable()
        {
            ObstacleMapManager myScript = (ObstacleMapManager)target;
            myScript.Start();
        }


        public override void OnInspectorGUI()
        {
            var changed = DrawDefaultInspector();

            ObstacleMapManager myScript = (ObstacleMapManager)target;
            if (changed && myScript.drawObstacleMap)
            {
                myScript.Start();
            }
        }
    }
}