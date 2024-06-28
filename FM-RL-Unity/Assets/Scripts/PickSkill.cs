using System;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace DefaultNamespace
{
    public class PickSkill : MonoBehaviour
    {
        public Transform target;
        public Transform rightArm;
        public Transform leftArm;
        public AgentSimple agent;
        private Transform chestTransform;
        private Transform handLTransform;
        private Transform handRTransform;
        private float distanceSide;
        private int counter = 0;
        private int stepLength = 50;

        private void Start()
        {
            chestTransform = agent.m_chain.chest.transform;
            handLTransform = agent.m_chain.handL.transform;
            handRTransform = agent.m_chain.handR.transform;
        }

        private void OnEnable()
        {
            distanceSide = 0.15f;
            counter = 0;
        }

        private void FixedUpdate()
        {
            var rightArmDesiredPosition = chestTransform.InverseTransformPoint(target.position) + chestTransform.right * distanceSide + Vector3.up * 0.09f;
            var leftArmDesiredPosition = chestTransform.InverseTransformPoint(target.position) - chestTransform.right * distanceSide + Vector3.up * 0.09f;

            if (counter > stepLength * 2)
            {
                rightArmDesiredPosition.y = 0;
                leftArmDesiredPosition.y = 0;
            }


            rightArm.localPosition = rightArmDesiredPosition;
            leftArm.localPosition = leftArmDesiredPosition;


            counter++;
            if (counter > stepLength)
            {
                distanceSide = 0.07f;
            }
        }
    }
}