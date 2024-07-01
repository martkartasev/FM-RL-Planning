using System;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace DefaultNamespace
{
    public class PickSkill : MonoBehaviour
    {
        public float secondsPerPhase = 1;
        public Transform target;
        public Transform rightArm;
        public Transform leftArm;
        public AgentSimple agent;
        private Transform chestTransform;
        private Transform handLTransform;
        private Transform handRTransform;
        private float distanceSide;
        private int counter = 0;
        private int stepLength;

        private void Start()
        {
            chestTransform = agent.m_chain.chest.transform;
            handLTransform = agent.m_chain.handL.transform;
            handRTransform = agent.m_chain.handR.transform;
            stepLength = (int) (secondsPerPhase / Time.fixedDeltaTime);
        }

        private void OnEnable()
        {
            stepLength = (int) (secondsPerPhase / Time.fixedDeltaTime);
            distanceSide = 0.4f;
            counter = 0;
            agent.spineValue = 0.0f;
            agent.handLValue = 0f;
            agent.handRValue = 0f;
        }

        private void FixedUpdate()
        {
            DoUpdate();
        }

        private void DoUpdate()
        {
            var rightArmDesiredPosition = chestTransform.InverseTransformPoint(target.position) + chestTransform.right * distanceSide;
            var leftArmDesiredPosition = chestTransform.InverseTransformPoint(target.position) - chestTransform.right * distanceSide;

            counter++;
            if (counter < stepLength)
            {
                agent.spineValue = 0.5f;
                rightArmDesiredPosition = new Vector3(0.30f, 0.05f, 0.5f);
                leftArmDesiredPosition = new Vector3(-0.30f, 0.05f, 0.5f);
                agent.handLValue = -0.8f;
                agent.handRValue = -0.8f;
            }

            if (counter > stepLength)
            {
                distanceSide = 0.07f;
            }
            
            if (counter > stepLength * 2)
            {
                agent.handLValue = 0.15f;
                agent.handRValue = 0.15f;
            }

            if (counter > stepLength * 3)
            {
                rightArmDesiredPosition = new Vector3(rightArmDesiredPosition.x, 0.05f, 0.65f);
                leftArmDesiredPosition = new Vector3(leftArmDesiredPosition.x, 0.05f, 0.65f);
            }
            
            if (counter > stepLength * 4)
            {
                agent.spineValue = 0.0f;
            }
            else
            {
                rightArm.localPosition = rightArmDesiredPosition;
                leftArm.localPosition = leftArmDesiredPosition;
            }
      
        }

        private void OnDisable()
        {
            distanceSide = 0.15f;
            counter = 0;
            DoUpdate();
            counter = 0;
        }
    }
}