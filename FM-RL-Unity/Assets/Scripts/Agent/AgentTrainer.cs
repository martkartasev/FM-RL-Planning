using DefaultNamespace;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Agent
{
    public class AgentTrainer : Unity.MLAgents.Agent
    {
        [Header("Target")] public Transform target; //Target the agent will try to grasp.
        [Header("Target Position")] public Transform targetPosition;
        public Transform referenceFrame;

        private ArticulationChainComponent m_chain;

        private IRewarder rewarderBox;
        private IRewarder rewarderBoxM;
        private IRewarder rewarderBoxN;
        private IRewarder rewarderLHand;
        private IRewarder rewarderRHand;
        private float reward_norm_mult;

        public override void Initialize()
        {
            m_chain = GetComponent<ArticulationChainComponent>();
            var decisionRequester = GetComponent<DecisionRequester>();
            reward_norm_mult = 1f / MaxStep *
                               (decisionRequester.TakeActionsBetweenDecisions ? 1f : decisionRequester.DecisionPeriod);
        }


        /// <summary>
        /// Loop over body parts and reset them to initial conditions.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            m_chain.root.immovable = true;
            m_chain.Restart(transform.parent.TransformPoint(Vector3.zero), Quaternion.Euler(transform.parent.TransformDirection(Vector3.zero)));

            target.GetComponent<TargetPositionRandomizer>().Randomize();
            targetPosition.GetComponent<TargetPositionRandomizer>().RandomizeWithRespectTo(transform);

            rewarderBox = new ClosenessRewarder(() => (targetPosition.position - target.position).magnitude);
            rewarderBoxM = new ClosenessRewarder(() => (targetPosition.position - target.position).magnitude, 0.6f);
            rewarderBoxN = new ClosenessRewarder(() => (targetPosition.position - target.position).magnitude, 0.3f);
            rewarderLHand = new ClosenessRewarder(() => (m_chain.handL.transform.position - target.position).magnitude, 0.5f);
            rewarderRHand = new ClosenessRewarder(() => (m_chain.handR.transform.position - target.position).magnitude, 0.5f);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            var targetWorldPos = target.transform.position;
            var goalPositionWorldPos = targetPosition.transform.position;

            sensor.AddObservation(target.transform.localRotation);
            sensor.AddObservation(referenceFrame.InverseTransformDirection(targetWorldPos).NormalizeVector(2f));
            sensor.AddObservation(referenceFrame.InverseTransformDirection(goalPositionWorldPos).NormalizeVector(2f));
            sensor.AddObservation(referenceFrame.InverseTransformVector(targetWorldPos - goalPositionWorldPos).NormalizeVector(2f));

            sensor.AddObservation(referenceFrame.InverseTransformVector(targetWorldPos - m_chain.handL.transform.position).NormalizeVector(1f));
            sensor.AddObservation(referenceFrame.InverseTransformVector(targetWorldPos - m_chain.handR.transform.position).NormalizeVector(1f));

            foreach (var bodyPart in m_chain.bodyParts)
            {
                if (!bodyPart.name.ToLower().Contains("wheel"))
                {
                    CollectObservationBodyPart(bodyPart, sensor);
                }
            }
        }

        /// <summary>
        /// Add relevant information on each body part to observations.
        /// </summary>
        public void CollectObservationBodyPart(ArticulationBody bp, VectorSensor sensor)
        {
            //GROUND CHECK
            sensor.AddObservation(bp.GetComponent<GroundContact>().touchingGround); // Is this bp touching the ground

            //Get velocities in the context of our orientation cube's space
            //Note: You can get these velocities in world space as well but it may not train as well.

            sensor.AddObservation(referenceFrame.InverseTransformDirection(bp.velocity).NormalizeVector(5f));
            sensor.AddObservation(referenceFrame.InverseTransformDirection(bp.angularVelocity).NormalizeVector(5f));
            sensor.AddObservation(bp.transform.localRotation);
            sensor.AddObservation(referenceFrame
                .InverseTransformDirection(bp.transform.position - m_chain.chest.transform.position).NormalizeVector(1.5f));
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            SetDriveValues(actionBuffers);
            var reward = ComputeReward();
            AddReward(reward);
        }

        private void SetDriveValues(ActionBuffers actionBuffers)
        {
            var continuousActions = actionBuffers.ContinuousActions;
            var i = -1;

            m_chain.DriveControllers[m_chain.spine].SetDriveTargetsNorm(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
            m_chain.DriveControllers[m_chain.chest].SetDriveTargetsNorm(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
            m_chain.DriveControllers[m_chain.head].SetDriveTargetsNorm(continuousActions[++i], continuousActions[++i], 0);

            m_chain.DriveControllers[m_chain.armL_pitch].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
            m_chain.DriveControllers[m_chain.armL_yaw].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
            m_chain.DriveControllers[m_chain.forearmL].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
            m_chain.DriveControllers[m_chain.handL].SetDriveTargetsNorm(0, continuousActions[++i], 0);

            m_chain.DriveControllers[m_chain.armR_pitch].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
            m_chain.DriveControllers[m_chain.armR_yaw].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
            m_chain.DriveControllers[m_chain.forearmR].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
            m_chain.DriveControllers[m_chain.handR].SetDriveTargetsNorm(0, continuousActions[++i], 0);
            ////// Drive forces / strengths
            // m_chain.DriveControllers[m_chain.spine].SetDriveStrength(continuousActions[++i]);
            // m_chain.DriveControllers[m_chain.chest].SetDriveStrength(continuousActions[++i]);
            // m_chain.DriveControllers[m_chain.head].SetDriveStrength(continuousActions[++i]);
            //
            // m_chain.DriveControllers[m_chain.armL].SetDriveStrength(continuousActions[++i]);
            // m_chain.DriveControllers[m_chain.forearmL].SetDriveStrength(continuousActions[++i]);
            // m_chain.DriveControllers[m_chain.handL].SetDriveStrength(continuousActions[++i]);
            //
            // m_chain.DriveControllers[m_chain.armR].SetDriveStrength(continuousActions[++i]);
            // m_chain.DriveControllers[m_chain.forearmR].SetDriveStrength(continuousActions[++i]);
            // m_chain.DriveControllers[m_chain.handR].SetDriveStrength(continuousActions[++i]);
        }

        private float ComputeReward()
        {
            if (rewarderLHand == null || rewarderRHand == null || rewarderBox == null || MaxStep == 0) return 0.0f;

            var reward = 0.0f;

            var right = m_chain.hips.transform.right;

            var dotPosition = Mathf.Max(DotPosition(right));
            var dotOrient = Mathf.Max(DotOrientation(right));
            var dot = dotPosition * dotOrient;

            reward += (m_chain.handL.GetComponent<TargetContact>().hasContact ? 1 : 0) * 0.5f * reward_norm_mult;
            reward += (m_chain.handR.GetComponent<TargetContact>().hasContact ? 1 : 0) * 0.5f * reward_norm_mult;

            reward += rewarderBox.Reward() * reward_norm_mult;
            reward += rewarderBoxM.Reward() * reward_norm_mult;
            reward += rewarderBoxN.Reward() * reward_norm_mult;
            reward += rewarderRHand.Reward() * 0.5f * reward_norm_mult; // Only goes to one per episode anyway //*dot 
            reward += rewarderLHand.Reward() * 0.5f * reward_norm_mult; //*dot 
            reward /= 5;

            reward += -0.1f * reward_norm_mult; //Time penalty
            // Even smaller?

            // if ((targetPosition.position - target.position).magnitude < 0.08f)
            // {
            //     // reward += 10;
            //     targetPosition.GetComponent<TargetPositionRandomizer>().RandomizeWithRespectTo(transform);
            // }

            return reward;
        }

        private float DotOrientation(Vector3 boxBaseVector)
        {
            var leftHand = m_chain.handL.transform.right;
            var rightHand = m_chain.handR.transform.right;
            return Vector3.Dot(leftHand, boxBaseVector) *
                   Vector3.Dot(rightHand, boxBaseVector) *
                   Vector3.Dot(rightHand, leftHand);
        }

        private float DotPosition(Vector3 boxBaseVector)
        {
            var leftHand = (m_chain.handL.transform.position - target.transform.position).normalized;
            var rightHand = (m_chain.handR.transform.position - target.transform.position).normalized;

            //Vector3.Dot(leftHand, boxBaseVector) * Vector3.Dot(rightHand, boxBaseVector) *
            return -Vector3.Dot(rightHand, leftHand);
        }

        public void FixedUpdate()
        {
            if (m_chain.root.immovable) m_chain.root.immovable = false;
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
        }
    }
}