using UnityEngine;

namespace Environment
{
    public class PushDetector : MonoBehaviour
    {
        public bool pushed = false;
        private void OnCollisionStay(Collision other)
        {
            if (transform.localPosition.y < 0.01)
            {
                pushed = true;
            }
        }

        private void OnCollisionExit(Collision other)
        {
            pushed = false;
        }
    }
}