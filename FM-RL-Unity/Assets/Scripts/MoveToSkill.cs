using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

namespace DefaultNamespace
{
    public class MoveToSkill : MonoBehaviour
    {
        public Transform hips;
        public Transform targetPosition;
        public AgentSimple agent;


        private void FixedUpdate()
        {
            if ((hips.position - targetPosition.position).magnitude > 0.9f)
            {
                var relativeTarget = targetPosition.position - hips.position;
                relativeTarget.y = 0;
                var angle = Vector3.SignedAngle(relativeTarget.normalized, hips.forward, Vector3.up);
                if (Mathf.Abs(angle) > 2f)
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