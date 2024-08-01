using UnityEngine;

namespace Scripts
{
    public class FollowObject : MonoBehaviour
    {
        public Transform target_object;


        public Vector3 cameraPosition;
        public Vector3 cameraTargetOffset;

        public bool CameraFixed;

        void FixedUpdate()
        {
            transform.localPosition = target_object.localPosition
                                 + cameraPosition.x * Vector3.right
                                 + cameraPosition.z * Vector3.forward
                                 + cameraPosition.y * Vector3.up;
        }
    }
}