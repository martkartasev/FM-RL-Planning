using System;
using UnityEngine;

namespace Agent
{
    public class MoveToSkill : MonoBehaviour
    {
        public Transform hips;
        public Transform targetPosition;
        public AgentSimple agent;
        public bool done;

        private void FixedUpdate()
        {
            var relativeTarget = targetPosition.position - hips.position;
            relativeTarget.y = 0;
            if (relativeTarget.magnitude > 0.65f)
            {
                done = false;
                var hipsForward = hips.forward;
                hipsForward.y = 0;
                var angle = Vector3.SignedAngle(relativeTarget.normalized, hipsForward, Vector3.up);
                if (Mathf.Abs(angle) > 4f)
                {
                    agent.turnValue = -Mathf.Sign(angle * 0.5f) * Math.Min(1, Mathf.Abs(angle) / 45 + 0.2f);
                    agent.forwardValue = -0.001f;
                }
                else
                {
                    agent.turnValue = 0.0f;
                    agent.forwardValue = 1f;
                }
            }
            else
            {
                done = true;
                agent.turnValue = 0.0f;
                agent.forwardValue = 0.0f;
            }
        }

        private void OnEnable()
        {
            agent.turnValue = 0.0f;
            agent.forwardValue = 0.0f;
        }

        private void OnDisable()
        {
            OnEnable();
        }
    }
}