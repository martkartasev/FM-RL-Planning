using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Map
{
    public class ObstacleMapManager : MonoBehaviour
    {
        private ObstacleMap m_ObstacleMap;

        public Grid grid;
        public MapManager mapManager;
        public bool drawObstacleMap;

        public float blockedUnfilledMargin = 0.1f;
        public float partialUnfilledMargin = 0.1f;
        public ObstacleMap ObstacleMap => m_ObstacleMap;

        public void Start()
        {
            m_ObstacleMap = Initialize(additionalObjects: new List<GameObject>());
        }

        public ObstacleMap Initialize(List<GameObject> additionalObjects)
        {
            var obstacleObjects = mapManager.GetObstacleObjects();
            obstacleObjects.AddRange(additionalObjects);
            var ObstacleMap = new ObstacleMap(obstacleObjects, grid);
            ObstacleMap.blockedUnfilledMargin = blockedUnfilledMargin;
            ObstacleMap.partialUnfilledMargin = partialUnfilledMargin;
            ObstacleMap.GenerateMap();
            return ObstacleMap;
        }

        void OnDrawGizmos()
        {
            if (drawObstacleMap)
            {
                if (m_ObstacleMap == null)
                {
                    Start();
                }

                RenderObstacleMap();
            }
            else
            {
                m_ObstacleMap = null;
            }
        }

        private void RenderObstacleMap()
        {
            foreach (var posEntity in m_ObstacleMap.traversabilityPerCell)
            {
                var position = new Vector3Int(posEntity.Key.x, posEntity.Key.y, 0);

                var cellToWorld = grid.CellToWorld(position);
                cellToWorld += Vector3.Cross(grid.transform.localScale, new Vector3(grid.cellSize.x, 0, grid.cellSize.y) / 2);
                cellToWorld += Vector3.forward;
                cellToWorld += Vector3.up * 0.25f;

                var gizmoSize = new Vector3(grid.cellSize.x, 0.005f, grid.cellSize.y);
                
                if (posEntity.Value == ObstacleMap.Traversability.Blocked)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(cellToWorld, gizmoSize);
                }
                else if (posEntity.Value == ObstacleMap.Traversability.Partial)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(cellToWorld, gizmoSize);
                }
                else
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(cellToWorld, gizmoSize);
                }
            }
        }
    }
}