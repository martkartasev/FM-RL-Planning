using System.Collections.Generic;
using System.Linq;
using Scripts.Map;
using UnityEngine;

namespace Scripts.Utils
{
    public static class GameManagerUtils
    {
        public static List<GameObject> CreatePrefabs(MapManager mapToUse, GameObject prefab)
        {
            var vehicleAssignments = new List<GameObject>();
            foreach (var indexedGameObject in mapToUse.GetStartObjects().Select((value, index) => new { value, index }))
            {
                var vehicleInstance = Object.Instantiate(prefab);

                vehicleInstance.transform.position = indexedGameObject.value.transform.position + prefab.transform.localPosition;
                vehicleInstance.transform.rotation = indexedGameObject.value.transform.rotation;

                vehicleAssignments.Add(vehicleInstance);
            }

            return vehicleAssignments;
        }

        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }

            return copy as T;
        }
    }
}