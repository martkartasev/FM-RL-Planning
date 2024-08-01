using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Agent
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
        public bool done;

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
            distanceSide = 0.25f;
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
            Vector3 orientationVec = new List<Vector3>() {target.forward, target.up, target.right, -target.forward, -target.up, -target.right}
                .Where(vec => Mathf.Abs(Vector3.Dot(vec, Vector3.up)) < 0.5f)
                .Select(vec => (vec, Vector3.SignedAngle(vec.normalized, chestTransform.right, Vector3.up)))
                .MinBy(pair => Mathf.Abs((float)pair.Item2))
                .vec;

            var rightArmDesiredPosition = chestTransform.InverseTransformPoint(target.position + orientationVec * distanceSide);
            var leftArmDesiredPosition = chestTransform.InverseTransformPoint(target.position - orientationVec * distanceSide);

            counter++;
            if (counter < stepLength)
            {
                done = false;
                agent.spineValue = 0.5f;
                rightArmDesiredPosition = new Vector3(0.30f, 0.05f, 0.5f);
                leftArmDesiredPosition = new Vector3(-0.30f, 0.05f, 0.5f);
                agent.handLValue = -0.8f;
                agent.handRValue = -0.8f;
            }

            if (counter > stepLength && counter < stepLength * 2)
            {
                distanceSide = 0.07f;
            }

            if (counter > stepLength * 2 && counter < stepLength * 3)
            {
                agent.handLValue = 0.15f;
                agent.handRValue = 0.15f;
            }

            if (counter > stepLength * 3 && counter < stepLength * 4)
            {
                rightArmDesiredPosition = new Vector3(rightArmDesiredPosition.x, 0.05f, 0.65f);
                leftArmDesiredPosition = new Vector3(leftArmDesiredPosition.x, 0.05f, 0.65f);
            }

            if (counter > stepLength * 4)
            {
                agent.spineValue = 0.0f;
                done = true;
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
            rightArm.localPosition = new Vector3(0.326f, -0.102f, 0.179f);
            leftArm.localPosition = new Vector3(-0.297f, -0.037f, 0.213f);
        }
    }
}