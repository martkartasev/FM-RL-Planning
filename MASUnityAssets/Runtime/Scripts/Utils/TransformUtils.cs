using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Utils
{
    public static class TransformUtils
    {
        public static List<Transform> GetChildren(this Transform gameObject)
        {
            List<Transform> returnList = new();
            foreach (Transform childTransform in gameObject.transform)
            {
                returnList.Add(childTransform);
            }

            return returnList;
        }
    }
}