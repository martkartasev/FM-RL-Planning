using UnityEngine;

namespace Agent
{
    public class PushSkill : MonoBehaviour
    {
        public float secondsPerPhase = 1f;
        public Transform target;
        public Transform rightArm;
        public AgentSimple agent;
        private Transform chestTransform;
        private int counter = 0;
        private int stepLength;
        private Vector3 initialPosition;
        public bool done = false;

        private void Start()
        {
            chestTransform = agent.m_chain.chest.transform;
            stepLength = (int)(secondsPerPhase / Time.fixedDeltaTime);
        }

        private void OnEnable()
        {
            agent.spineValue = 0.0f;
            agent.handLValue = 0f;
            agent.handRValue = 0f;
            initialPosition = target.position;
            counter = 0;
            done = false;
        }

        private void FixedUpdate()
        {
            DoUpdate();
        }

        private void DoUpdate()
        {
            counter++;
            if (counter < stepLength)
            {
                rightArm.localPosition = chestTransform.InverseTransformPoint(initialPosition).normalized / 2;
            }

            if (counter > stepLength && counter < stepLength * 2)
            {
                agent.spineValue = 0.5f;
                rightArm.localPosition = chestTransform.InverseTransformPoint(initialPosition);
            }

            if (counter > stepLength * 2 && counter < stepLength * 3)
            {
                done = true;
            }
        }

        private void OnDisable()
        {
            DoUpdate();
            rightArm.localPosition = new Vector3(0.326f, -0.102f, 0.179f);
        }
    }
}