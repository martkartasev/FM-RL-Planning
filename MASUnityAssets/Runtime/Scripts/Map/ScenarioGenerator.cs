using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Scripts.Map
{
    [RequireComponent(typeof(MapManager))]
    public class ScenarioGenerator : MonoBehaviour
    {
        public int randomSeed;
        public int numberToGenerate = 10;

        public float circleRadius = 1f;
        public bool halfCircle;

        private MapManager mapManager;
        private ObstacleMap obstacleMap;

        public void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            mapManager = transform.GetComponent<MapManager>();

            var obstacleManager = transform.GetComponentInChildren<ObstacleMapManager>();
            obstacleManager.Start();

            obstacleMap = obstacleManager.ObstacleMap;
        }

        public void GenerateStarts()
        {
            var generateCircle = circleRadius != 0;
            var generatedPositions = generateCircle ? GenerateCirclePositions(halfCircle ? 180f : 360f) : GeneratePositions();

            mapManager.ClearStarts();
            mapManager.startPositions = generatedPositions;
        }

        public void GenerateTargets()
        {
            var generateCircle = circleRadius != 0;
            var generatedPositions = generateCircle ? GenerateCirclePositions(halfCircle ? 180f : 360f, 180f) : GeneratePositions();

            mapManager.ClearTargets();
            mapManager.targetPositions = generatedPositions;
        }

        private List<Vector3> GenerateCirclePositions(float t_max = 360, float t_offset = 0)
        {
            if (obstacleMap == null) Initialize();
            List<Vector3> generatedPositions = new();

            if (circleRadius != 0)
            {
                float tD = t_max / numberToGenerate;
                for (float t = t_offset; t < t_max + t_offset; t += tD)
                {
                    var x = circleRadius * Mathf.Sin(Mathf.Deg2Rad * t);
                    var z = circleRadius * Mathf.Cos(Mathf.Deg2Rad * t);

                    generatedPositions.Add(obstacleMap.localBounds.center + new Vector3(x, 0, z));
                }
            }

            return generatedPositions;
        }


        private List<Vector3> GeneratePositions()
        {
            if (obstacleMap == null) Initialize();
            if (randomSeed != 0) Random.InitState(randomSeed);

            List<Vector3> generatedPositions = new();
            for (int i = 0; i < numberToGenerate; i++)
            {
                bool done = false;
                Vector3 pos = Vector3.zero;

                while (!done)
                {
                    pos = new Vector3(Random.Range(obstacleMap.localBounds.min.x, obstacleMap.localBounds.max.x), 0f, Random.Range(obstacleMap.localBounds.min.z, obstacleMap.localBounds.max.z));

                    if (obstacleMap.IsLocalPointTraversable(pos) == ObstacleMap.Traversability.Free)
                    {
                        done = true;
                        generatedPositions.Add(pos);
                    }
                }
            }

            return generatedPositions;
        }
    }
}