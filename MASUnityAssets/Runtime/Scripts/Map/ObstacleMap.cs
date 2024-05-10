using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts.Map
{
    public class ObstacleMap
    {
        private List<GameObject> obstacles;

        public Dictionary<Vector2Int, Traversability> traversabilityPerCell;
        public Dictionary<Vector2Int, List<GameObject>> gameGameObjectsPerCell;
        public List<GameObject> obstacleObjects;
        public Grid mapGrid;
        public BoundsInt localBounds;
        public BoundsInt cellBounds;

        public float blockedUnfilledMargin = 0.1f;
        public float partialUnfilledMargin = 0.1f;

        public ObstacleMap(List<GameObject> obstacleObjects, Grid mapGrid)
        {
            if (mapGrid.cellSize.x == 0 || mapGrid.cellSize.y == 0) throw new ArgumentException("Invalid Grid size. Cannot be 0!");

            this.mapGrid = mapGrid;
            this.obstacleObjects = obstacleObjects;
        }

        public void GenerateMap()
        {
            var mapBoundsHelper = EncapsulateGameObjects(this.obstacleObjects);
            mapBoundsHelper = InverseTransformBounds(mapGrid.transform, mapBoundsHelper);

            var minToInt = Vector3Int.CeilToInt(new Vector3(mapBoundsHelper.min.x / mapGrid.cellSize.x, mapBoundsHelper.min.z / mapGrid.cellSize.y, 0));
            var maxToInt = Vector3Int.FloorToInt(new Vector3(mapBoundsHelper.max.x / mapGrid.cellSize.x, mapBoundsHelper.max.z / mapGrid.cellSize.y, 1));
            cellBounds = new BoundsInt(minToInt, maxToInt - minToInt);

            minToInt = Vector3Int.CeilToInt(new Vector3(mapBoundsHelper.min.x, 0, mapBoundsHelper.min.z));
            maxToInt = Vector3Int.FloorToInt(new Vector3(mapBoundsHelper.max.x, 1, mapBoundsHelper.max.z));
            localBounds = new BoundsInt(minToInt, maxToInt - minToInt);

            (gameGameObjectsPerCell, traversabilityPerCell) = GenerateMapData(this.obstacleObjects, this.mapGrid);
        }

        public Traversability IsGlobalPointTraversable(Vector3 worldPosition)
        {
            var cellPos = mapGrid.WorldToCell(worldPosition);
            if (worldPosition.y > 1 && cellPos.y == 0) Debug.LogWarning("Unexpected coordinates! Check reference frames. World: " + worldPosition + " Cell: " + cellPos);
            return traversabilityPerCell[new Vector2Int(cellPos.x, cellPos.y)];
        }

        public Traversability IsLocalPointTraversable(Vector3 localPosition)
        {
            var cellPos = mapGrid.LocalToCell(localPosition);
            if (localPosition.y > 1 && cellPos.y == 0) Debug.LogWarning("Unexpected coordinates! Check reference frames. World: " + localPosition + " Cell: " + cellPos);
            return traversabilityPerCell[new Vector2Int(cellPos.x, cellPos.y)];
        }

        public Traversability IsCellTraversable(Vector2Int cell)
        {
            return traversabilityPerCell[new Vector2Int(cell.x, cell.y)];
        }

        private (Dictionary<Vector2Int, List<GameObject>>, Dictionary<Vector2Int, Traversability>) GenerateMapData(List<GameObject> gameObjects, Grid grid)
        {
            var gameObjectsPerCell = new Dictionary<Vector2Int, List<GameObject>>();
            var traversabilityData = new Dictionary<Vector2Int, Traversability>();

            foreach (var pos in cellBounds.allPositionsWithin)
            {
                gameObjectsPerCell[new Vector2Int(pos.x, pos.y)] = new List<GameObject>();
                traversabilityData[new Vector2Int(pos.x, pos.y)] = Traversability.Free;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = grid.transform.localScale;
            cube.transform.localScale = new Vector3(cube.transform.localScale.x * grid.cellSize.x,
                (cube.transform.localScale.x * grid.cellSize.x + cube.transform.localScale.z * grid.cellSize.y) / 2,
                cube.transform.localScale.z * grid.cellSize.y);
            var unitCollider = cube.GetComponent<BoxCollider>();

            try
            {
                foreach (var gameObject in gameObjects)
                {
                    if (gameObject.name.Contains("road")) continue;

                    var objectBounds = ConvertToMapBoundsIntWithCellMargin(InverseTransformBounds(grid.transform, gameObject.GetComponent<Renderer>().bounds), grid.cellSize);

                    foreach (var cellPosition in objectBounds.allPositionsWithin)
                    {
                        var dictVector = new Vector2Int(cellPosition.x, cellPosition.y);

                        var collider = gameObject.GetComponent<Collider>();

                        if (collider != null)
                        {
                            var cubeLocation = grid.CellToWorld(new Vector3Int(dictVector.x, dictVector.y, 0)) + new Vector3(grid.cellSize.x, 0, grid.cellSize.y) / 2;
                            cubeLocation.y = cube.transform.localScale.y / 2;
                            cube.transform.position = cubeLocation;

                            var currentConv = false;
                            if (collider.GetType() == typeof(MeshCollider)) //TODO: Ugly case handling, needs refactor
                            {
                                currentConv = ((MeshCollider)collider).convex;
                                ((MeshCollider)collider).convex = true;
                            }

                            var overlapped = Physics.ComputePenetration(
                                unitCollider, cubeLocation, unitCollider.transform.rotation,
                                collider, gameObject.transform.position, gameObject.transform.rotation,
                                out var direction, out var distance
                            );

                            if (collider.GetType() == typeof(MeshCollider))
                            {
                                ((MeshCollider)collider).convex = currentConv;
                            }

                            Vector3 directionAbs = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
                            var transformLocalScale = mapGrid.cellSize - distance * directionAbs;
                            if (overlapped && traversabilityData.ContainsKey(dictVector) &&
                                (transformLocalScale.x < mapGrid.cellSize.x * blockedUnfilledMargin ||
                                 transformLocalScale.y < mapGrid.cellSize.y * blockedUnfilledMargin ||
                                 transformLocalScale.z < mapGrid.cellSize.z * blockedUnfilledMargin)
                               )
                            {
                                traversabilityData[dictVector] = Traversability.Blocked;
                                gameObjectsPerCell[dictVector].Add(gameObject);
                            }
                            else if (overlapped && traversabilityData.ContainsKey(dictVector)
                                                && traversabilityData[dictVector] != Traversability.Blocked &&
                                                (transformLocalScale.x < mapGrid.cellSize.x * partialUnfilledMargin ||
                                                 transformLocalScale.y < mapGrid.cellSize.y * partialUnfilledMargin ||
                                                 transformLocalScale.z < mapGrid.cellSize.z * partialUnfilledMargin))
                            {
                                traversabilityData[dictVector] = Traversability.Partial;
                                gameObjectsPerCell[dictVector].Add(gameObject);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (Application.isPlaying)
                {
                    GameObject.Destroy(cube);
                }
                else
                {
                    GameObject.DestroyImmediate(cube);
                }
            }

            return (gameObjectsPerCell, traversabilityData);
        }


        private Bounds EncapsulateGameObjects(List<GameObject> gameObjects)
        {
            var mapBoundsHelper = gameObjects[0].GetComponent<Renderer>().bounds;
            foreach (GameObject renderer in gameObjects)
            {
                var rendererBounds = renderer.transform.GetComponent<Renderer>();
                if (rendererBounds != null)
                {
                    mapBoundsHelper.Encapsulate(rendererBounds.bounds);
                }
                else
                {
                    foreach (var componentsInChild in renderer.GetComponentsInChildren<Renderer>())
                    {
                        mapBoundsHelper.Encapsulate(componentsInChild.bounds);
                    }
                }
            }

            return mapBoundsHelper;
        }

        private Bounds InverseTransformBounds(Transform _transform, Bounds _localBounds)
        {
            var center = _transform.InverseTransformPoint(_localBounds.center);

            var extents = _localBounds.extents;
            var axisX = _transform.InverseTransformVector(extents.x, 0, 0);
            var axisY = _transform.InverseTransformVector(0, extents.y, 0);
            var axisZ = _transform.InverseTransformVector(0, 0, extents.z);

            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

        private BoundsInt ConvertToMapBoundsIntWithCellMargin(Bounds bounds, Vector3 cellSize)
        {
            var minToInt = Vector3Int.FloorToInt(new Vector3(bounds.min.x / cellSize.x, bounds.min.z / cellSize.y, 0));
            var maxToInt = Vector3Int.CeilToInt(new Vector3(bounds.max.x / cellSize.x, bounds.max.z / cellSize.y, 1));

            BoundsInt boundsInt = new BoundsInt(minToInt, maxToInt - minToInt);
            return boundsInt;
        }


        public enum Traversability
        {
            Free = 0,
            Partial = 1,
            Blocked = 2,
        }
    }
}