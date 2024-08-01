using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.Map;
using Scripts.Utils;
using UnityEngine;

namespace Scripts.Game
{
    public abstract class AbstractGameManager : MonoBehaviour
    {
        public float completionTime;

        public MapManager mapManager;
        public GameObject vehiclePrefab;
        public Camera driveCamera;

        protected List<GameObject> vehicleList = new();
        protected List<Goal> goals;

        public abstract List<Goal> CreateGoals(List<GameObject> vehicles);

        protected virtual void Start()
        {
            if (vehiclePrefab == null) throw new Exception("No vehicle defined in game manager!");

            vehicleList = GameManagerUtils.CreatePrefabs(mapManager, vehiclePrefab);
            goals = CreateGoals(vehicleList);

            if (driveCamera != null)
            {
                AssignDriveCamera(driveCamera.GetComponent<FollowObject>(), vehicleList.First());
            }
        }


        private void AssignDriveCamera(FollowObject followObject, GameObject vehicleInstance)
        {
            if (followObject != null)
            {
                followObject.target_object = vehicleInstance.transform;
                followObject.CameraFixed = vehicleInstance.name.ToLower().Contains("drone");
            }
        }
    }
}