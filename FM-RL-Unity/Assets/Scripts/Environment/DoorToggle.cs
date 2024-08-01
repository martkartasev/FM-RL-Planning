using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Environment
{
    public class DoorToggle : MonoBehaviour
    {
        public PushDetector trigger;

        private float linear = 0;
        public bool triggered;
        public float increment = 0.01f;

        private void FixedUpdate()
        {
            triggered = triggered || trigger.pushed;
            
            if (triggered && linear < 1.0f)
            {
                linear += increment;
                
                var transformLocalRotation = transform.localRotation.eulerAngles;
                transformLocalRotation.y = Mathf.Lerp(0, 90, linear);
                transform.localRotation = Quaternion.Euler(transformLocalRotation);
            }
        }
    }
}