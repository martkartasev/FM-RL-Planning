using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class TargetContact : MonoBehaviour
    {
        public GameObject objectToDetect;
        public bool hasContact;

        public void OnCollisionEnter(Collision other)
        {
            if (other.gameObject == objectToDetect)
            {
                hasContact = true;
            }
        }

        public void OnCollisionExit(Collision other)
        {
            if (other.gameObject == objectToDetect)
            {
                hasContact = false;
            }
        }
    }
}