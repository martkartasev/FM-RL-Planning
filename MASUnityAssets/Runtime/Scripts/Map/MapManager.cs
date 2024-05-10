using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Scripts.Map
{
    public class MapManager : MonoBehaviour
    {
        public Grid grid;
        public GameObject groundPlane;

        public GameObject startPrefab;
        public GameObject targetPrefab;

        public List<Vector3> startPositions;
        public List<Vector3> targetPositions;

        public String fileName;

        private void Start()
        {
            Initialize();
        }

        public List<GameObject> GetObstacleObjects()
        {
            Initialize();
            return FindActiveTilemaps()
                .Select(tilemap => tilemap.transform)
                .SelectMany(transform =>
                    {
                        var ret = new List<GameObject>();
                        foreach (Transform child in transform)
                        {
                            if (child.GetComponent<Renderer>() != null)
                            {
                                ret.Add(child.gameObject);
                            }
                            else
                            {
                                foreach (var checkSubRenderers in child.GetChildren())
                                {
                                    if (checkSubRenderers.gameObject.GetComponent<Renderer>() != null)
                                    {
                                        ret.Add(checkSubRenderers.gameObject);
                                    }
                                }
                            }
                        }

                        return ret;
                    }
                ).Where(obj => obj.activeInHierarchy)
                .ToList();
        }

        public void Initialize()
        {
            if (grid != null)
            {
                startPositions = transform.Find("Grid/Starts")?.GetChildren().Select(child => grid.WorldToLocal(child.transform.position)).ToList();
                targetPositions = transform.Find("Grid/Targets")?.GetChildren().Select(child => grid.WorldToLocal(child.transform.position)).ToList();
            }
        }

        public Vector3 GetGlobalStartPosition()
        {
            return grid.LocalToWorld(startPositions[0]);
        }

        public Vector3 GetGlobalGoalPosition()
        {
            return grid.LocalToWorld(targetPositions[0]);
        }

        public void ClearMap()
        {
            foreach (var tilemap in FindActiveTilemaps())
            {
                List<GameObject> children = FetchChildren(tilemap.gameObject);
                foreach (var child in children)
                {
                    DestroyImmediate(child);
                }

                DestroyImmediate(tilemap.gameObject);
            }

            ClearStarts();
            ClearTargets();

            if (groundPlane != null) groundPlane.transform.localScale = Vector3.zero;
        }
#if UNITY_EDITOR
        public void SaveMap()
        {
            if (FindActiveTilemaps().Count == 0) return;

            var saveInfo = PrepareSaveData();

            var json = JsonUtility.ToJson(saveInfo);
            FileUtils.WriteJsonToFile(json, fileName);
        }
#endif
        public void LoadMap()
        {
            var saveData = JsonUtility.FromJson<SaveData>(FileUtils.ReadJsonFromFile(fileName));
            LoadSaveData(saveData);
        }

        public void LoadLegacyMap()
        {
            var saveData = new SaveData();
            var terrainInfo = FileUtils.CreateTerrainInfoFromJSONFileLegacy(fileName);

            ClearMap();

            float x_step = (terrainInfo.x_high - terrainInfo.x_low) / terrainInfo.x_N;
            float z_step = (terrainInfo.z_high - terrainInfo.z_low) / terrainInfo.z_N;
            saveData.grid = new SaveData.SavedGrid();
            saveData.grid.scale = new Vector3(x_step, 15.0f, z_step);
            saveData.goalPosition = new Vector3(terrainInfo.goal_pos.x / saveData.grid.scale.x,
                terrainInfo.goal_pos.y / saveData.grid.scale.y, terrainInfo.goal_pos.z / saveData.grid.scale.z);
            saveData.startPosition = new Vector3(terrainInfo.start_pos.x / saveData.grid.scale.x,
                terrainInfo.start_pos.y / saveData.grid.scale.y + 0.05f,
                terrainInfo.start_pos.z / saveData.grid.scale.z);

            saveData.savedTileMaps = new List<SaveData.SavedTileSet>();
            var savedTilemap = new SaveData.SavedTileSet();
            savedTilemap.name = "Ground 0";
            savedTilemap.position = Vector3.zero;
            savedTilemap.scale = Vector3.one;


            var savedTiles = new List<SaveData.SavedTile>();
            for (int i = 0; i < terrainInfo.x_N; i++)
            {
                for (int j = 0; j < terrainInfo.z_N; j++)
                {
                    if (terrainInfo.traversability[i, j] > 0.5f)
                    {
                        var savedTile = new SaveData.SavedTile();

                        savedTile.name = "block";
                        savedTile.hasCollider = true;
                        savedTile.isConvex = true;
                        savedTile.position = new Vector3(terrainInfo.get_x_pos(i) / saveData.grid.scale.x, 0.0f,
                            terrainInfo.get_z_pos(j) / saveData.grid.scale.z);
                        savedTile.rotation = Quaternion.identity;
                        savedTile.scale = Vector3.one;

                        savedTiles.Add(savedTile);
                    }
                }
            }

            //TODO: Calc based on actual placement of tiles
            saveData.groundPosition = new Vector3((terrainInfo.x_low + terrainInfo.x_high) / 2, 0.0f,
                (terrainInfo.z_high + terrainInfo.z_low) / 2);
            saveData.groundScale = new Vector3((terrainInfo.x_high - terrainInfo.x_low), 1,
                (terrainInfo.z_high - terrainInfo.z_low));

            savedTilemap.savedTiles = savedTiles;
            saveData.savedTileMaps.Add(savedTilemap);

            LoadSaveData(saveData);
        }

        private SaveData PrepareSaveData()
        {
            var saveData = new SaveData();
            saveData.savedTileMaps = new List<SaveData.SavedTileSet>();
            if (groundPlane != null)
            {
                saveData.groundScale = groundPlane.transform.localScale;
                saveData.groundPosition = groundPlane.transform.localPosition;
            }


            saveData.startPositions = startPositions;
            saveData.targetPositions = targetPositions;

            if (targetPrefab != null) saveData.targetPrefabName = targetPrefab.name;
            if (startPrefab != null) saveData.startPrefabName = startPrefab.name;

            saveData.grid = new SaveData.SavedGrid
            {
                scale = grid.transform.localScale
            };

            foreach (var tilemap in FindActiveTilemaps())
            {
                var saveMap = CreateTileSetSaveData(tilemap.gameObject);
                saveData.savedTileMaps.Add(saveMap);
            }

            return saveData;
        }

        private static SaveData.SavedTileSet CreateTileSetSaveData(GameObject tilemap)
        {
            var saveMap = new SaveData.SavedTileSet();
            saveMap.name = tilemap.name;
            saveMap.scale = tilemap.transform.localScale;
            saveMap.position = tilemap.transform.localPosition;

            saveMap.savedTiles = new List<SaveData.SavedTile>();

            foreach (Transform child in tilemap.transform)
            {
                if (FileUtils.LoadPrefabFromFile(child.name) != null)
                {
                    var tileSaveData = CreateTileSaveData(child.gameObject);
                    saveMap.savedTiles.Add(tileSaveData);
                }
            }

            return saveMap;
        }

        private static SaveData.SavedTile CreateTileSaveData(GameObject tile)
        {
            var meshCollider = tile.GetComponent<MeshCollider>();
            var children = new List<SaveData.SavedTile>();

            foreach (Transform child in tile.transform)
            {
                if (FileUtils.LoadPrefabFromFile(child.name) != null)
                {
                    children.Add(CreateTileSaveData(child.gameObject));
                }
            }

            return new SaveData.SavedTile
            {
                name = tile.name,
                position = tile.transform.localPosition,
                rotation = tile.transform.localRotation,
                scale = tile.transform.localScale,
                hasCollider = meshCollider != null,
                isConvex = meshCollider != null && meshCollider.convex,
                children = children
            };
        }


        private List<Tilemap> FindActiveTilemaps()
        {
            return grid.GetComponentsInChildren<Tilemap>().ToList().FindAll(tilemap => tilemap.enabled);
        }

        private void LoadSaveData(SaveData saveData)
        {
            ClearMap();

            grid.transform.localScale = saveData.grid.scale;
            grid.cellSwizzle = GridLayout.CellSwizzle.XZY;

            if (groundPlane != null)
            {
                groundPlane.transform.localScale = saveData.groundScale;
                groundPlane.transform.localPosition = saveData.groundPosition;
            }

            if (!string.IsNullOrEmpty(saveData.startPrefabName)) startPrefab = (GameObject)FileUtils.LoadPrefabFromFile(saveData.startPrefabName);
            if (!string.IsNullOrEmpty(saveData.targetPrefabName)) targetPrefab = (GameObject)FileUtils.LoadPrefabFromFile(saveData.targetPrefabName);

            foreach (var savedTileMap in saveData.savedTileMaps)
            {
                InstantiateTileSet(savedTileMap, grid.gameObject);
            }
        }

        private static void InstantiateTileSet(SaveData.SavedTileSet tileSet, GameObject parent)
        {
            var tilemapObj = new GameObject("Cool GameObject made from Code");
            tilemapObj.transform.parent = parent.transform;

            tilemapObj.name = tileSet.name;
            tilemapObj.transform.localScale = tileSet.scale;
            tilemapObj.transform.localPosition = tileSet.position;

            var tilemap = tilemapObj.AddComponent<Tilemap>();
            tilemapObj.AddComponent<TilemapRenderer>();
            tilemap.orientation = Tilemap.Orientation.XZ;

            foreach (var savedTile in tileSet.savedTiles)
            {
                InstantiateObject(savedTile, tilemapObj);
            }
        }

        private static void InstantiateObject(SaveData.SavedTile savedTile, GameObject parentObject)
        {
            var loadedPrefabResource = FileUtils.LoadPrefabFromFile(savedTile.name);
            var instantiated = (GameObject)Instantiate(loadedPrefabResource, Vector3.zero, Quaternion.identity);
            instantiated.transform.parent = parentObject.transform;

            instantiated.name = savedTile.name;
            instantiated.transform.localRotation = savedTile.rotation;
            instantiated.transform.localScale = savedTile.scale;
            instantiated.transform.localPosition = savedTile.position;
            if (savedTile.hasCollider)
            {
                var collider = instantiated.AddComponent<MeshCollider>();
                collider.convex = savedTile.isConvex;
            }

            if (savedTile.children.Count > 0)
            {
                if (instantiated.transform.childCount == savedTile.children.Count)
                {
                    for (int i = 0; i < savedTile.children.Count; i++)
                    {
                        var child = instantiated.transform.GetChild(i);
                        child.localPosition = savedTile.children[i].position;
                        child.localRotation = savedTile.children[i].rotation;
                        child.localScale = savedTile.children[i].scale;
                        child.name = savedTile.children[i].name;
                    }
                }
                else
                {
                    foreach (var savedTileChild in savedTile.children)
                    {
                        InstantiateObject(savedTileChild, instantiated);
                    }
                }
            }
        }


        private List<GameObject> FetchChildren(GameObject gameObject)
        {
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in gameObject.transform)
            {
                children.Add(child.gameObject);
            }

            return children;
        }

        public void InstantiateStarts()
        {
            ClearStarts();
            if (startPrefab == null) return;
            var starts = transform.Find("Grid/Starts");
            if (starts == null) starts = new GameObject("Starts").transform;
            if (starts.gameObject.GetComponent<Tilemap>() == null)
            {
                starts.gameObject.AddComponent<Tilemap>();
                starts.gameObject.AddComponent<TilemapRenderer>();
            }

            starts.transform.parent = grid.transform;
            starts.transform.localScale = Vector3.one;

            var createObjects = InstantiateAtPositions(startPositions.ConvertAll(pos => grid.LocalToWorld(pos)), startPrefab);
            foreach (var createdObject in createObjects)
            {
                createdObject.transform.parent = starts;
            }
        }

        public void InstantiateTargets()
        {
            ClearTargets();
            if (targetPrefab == null) return;
            var targets = transform.Find("Grid/Targets");
            if (targets == null) targets = new GameObject("Targets").transform;
            if (targets.gameObject.GetComponent<Tilemap>() == null)
            {
                targets.gameObject.AddComponent<Tilemap>();
                targets.gameObject.AddComponent<TilemapRenderer>();
            }

            targets.transform.parent = grid.transform;
            targets.transform.localScale = Vector3.one;

            var createdObjects = InstantiateAtPositions(targetPositions.ConvertAll(pos => grid.LocalToWorld(pos)), targetPrefab);
            foreach (var createdObject in createdObjects)
            {
                createdObject.transform.parent = targets;
            }
        }

        public List<GameObject> InstantiateAtPositions(List<Vector3> positions, GameObject prefab)
        {
            List<GameObject> instantiatedObjects = new();
            foreach (var position in positions)
            {
                var instantiate = Instantiate(prefab);
                instantiate.name = prefab.name;
                instantiate.transform.position = position + prefab.transform.localPosition;
                instantiatedObjects.Add(instantiate);
            }

            return instantiatedObjects;
        }

        public void ClearStarts()
        {
            var starts = transform.Find("Grid/Starts");
            if (starts != null) DestroyImmediate(starts.gameObject);
        }

        public void ClearTargets()
        {
            var targets = transform.Find("Grid/Targets");
            if (targets != null) DestroyImmediate(targets.gameObject);
        }

        public void SyncObjectsToPositions()
        {
            var targets = transform.Find("Grid/Targets");
            if (targets != null)
            {
                targetPositions = targets.GetChildren().Select(trns => grid.WorldToLocal(trns.position)).ToList();
            }

            var starts = transform.Find("Grid/Starts");
            if (starts != null)
            {
                startPositions = starts.GetChildren().Select(trns => grid.WorldToLocal(trns.position)).ToList();
            }
        }

        public void SyncPositionsToObjects()
        {
            var targets = transform.Find("Grid/Targets");
            if (targets != null)
            {
                var transforms = targets.GetChildren();
                if (targetPositions.Count > transforms.Count)
                {
                    InstantiateTargets();
                    targets = transform.Find("Grid/Targets");
                    transforms = targets.GetChildren();
                }

                bool changed = false;
                foreach (var indexedPosition in targetPositions.Select((value, index) => new { value, index }))
                {
                    var targetObject = transforms[indexedPosition.index];
                    if (targetObject.transform.localPosition != indexedPosition.value)
                    {
                        changed = true;
                        targetObject.transform.localPosition = indexedPosition.value;
                    }
                }

                if (changed)
                {
                    ClearTargets();
                    InstantiateTargets();
                }
            }

            var starts = transform.Find("Grid/Starts");
            if (starts != null)
            {
                var transforms = starts.GetChildren();
                if (startPositions.Count > transforms.Count)
                {
                    InstantiateStarts();
                    starts = transform.Find("Grid/Starts");
                    transforms = starts.GetChildren();
                }


                bool changed = false;
                foreach (var indexedPosition in startPositions.Select((value, index) => new { value, index }))
                {
                    var startObject = transforms[indexedPosition.index];
                    if (startObject.transform.localPosition != indexedPosition.value)
                    {
                        changed = true;
                        startObject.transform.localPosition = indexedPosition.value;
                    }
                }

                if (changed)
                {
                    ClearStarts();
                    InstantiateStarts();
                }
            }
        }

        public List<GameObject> GetStartObjects()
        {
            return transform.Find("Grid/Starts").GetChildren().Select(trans => trans.gameObject).ToList();
        }

        public List<GameObject> GetTargetObjects()
        {
            return transform.Find("Grid/Targets").GetChildren().Select(trans => trans.gameObject).ToList();
        }
    }


    [Serializable]
    public class SaveData
    {
        public SavedGrid grid;
        public List<SavedTileSet> savedTileMaps;
        public SavedTileSet targets;
        public SavedTileSet starts;

        public Vector3 groundScale;
        public Vector3 groundPosition;

        //Deprecated
        public Vector3 startPosition;

        //Deprecated
        public Vector3 goalPosition;

        public List<Vector3> startPositions;
        public List<Vector3> targetPositions;
        public String startPrefabName;
        public String targetPrefabName;

        [Serializable]
        public struct SavedTileSet
        {
            public String name;
            public Vector3 scale;
            public Vector3 position;
            public List<SavedTile> savedTiles;
        }

        [Serializable]
        public struct SavedTile
        {
            public String name;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;

            public bool render;
            public bool hasCollider;
            public bool isConvex;
            public List<SavedTile> children;
        }

        [Serializable]
        public struct SavedGrid
        {
            public Vector3 scale;
        }
    }
}