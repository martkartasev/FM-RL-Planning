using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AgentTrainer : Agent
{
    [Header("Target")] public Transform target; //Target the agent will try to grasp.
    [Header("Target Position")] public Transform targetPosition;

    private ArticulationChainComponent m_chain;

    private IRewarder rewarderBox;
    private IRewarder rewarderBoxM;
    private IRewarder rewarderBoxN;
    private IRewarder rewarderLHand;
    private IRewarder rewarderRHand;


    public override void Initialize()
    {
        m_chain = GetComponent<ArticulationChainComponent>();
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        m_chain.Restart(transform.parent.TransformPoint(Vector3.zero), Quaternion.Euler(transform.parent.TransformDirection(Vector3.zero)));

        target.GetComponent<TargetPositionRandomizer>().Randomize();
        targetPosition.GetComponent<TargetPositionRandomizer>().RandomizeWithRespectTo(transform);

        rewarderBox = new ClosenessRewarder(() => (targetPosition.position - target.position).magnitude);
        rewarderBoxM = new ClosenessRewarder(() => (targetPosition.position - target.position).magnitude, 0.6f);
        rewarderBoxN = new ClosenessRewarder(() => (targetPosition.position - target.position).magnitude, 0.3f);
        rewarderLHand = new ClosenessRewarder(() => (m_chain.handL.transform.position - target.position).magnitude, 1.0f);
        rewarderRHand = new ClosenessRewarder(() => (m_chain.handR.transform.position - target.position).magnitude, 1.0f);
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
        var hips = m_chain.root;

        sensor.AddObservation(hips.transform.InverseTransformDirection(bp.velocity));
        sensor.AddObservation(hips.transform.InverseTransformDirection(bp.angularVelocity));
        sensor.AddObservation(bp.transform.localRotation);

        if (bp != hips)
        {
            //Get position relative to hips in the context of our orientation cube's space
            sensor.AddObservation(hips.transform.InverseTransformDirection(bp.transform.position - hips.transform.position));
            //sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(m_chain.hips.transform.InverseTransformDirection(target.transform.position - m_chain.hips.transform.position));
        sensor.AddObservation(target.transform.localRotation);
        sensor.AddObservation(m_chain.hips.transform.InverseTransformDirection(targetPosition.transform.position - m_chain.hips.transform.position));

        foreach (var bodyPart in m_chain.bodyParts)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }
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

        m_chain.DriveControllers[m_chain.spine].SetDriveTargets(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        m_chain.DriveControllers[m_chain.chest].SetDriveTargets(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        m_chain.DriveControllers[m_chain.head].SetDriveTargets(continuousActions[++i], continuousActions[++i], 0);

        m_chain.DriveControllers[m_chain.armL].SetDriveTargets(continuousActions[++i], continuousActions[++i], 0);
        m_chain.DriveControllers[m_chain.forearmL].SetDriveTargets(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.handL].SetDriveTargets(0, continuousActions[++i], 0);

        m_chain.DriveControllers[m_chain.armR].SetDriveTargets(continuousActions[++i], continuousActions[++i], 0);
        m_chain.DriveControllers[m_chain.forearmR].SetDriveTargets(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.handR].SetDriveTargets(0, continuousActions[++i], 0);

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

        reward += rewarderRHand.Reward() * 0.5f; //*dot
        reward += rewarderLHand.Reward() * 0.5f; //*dot 
        // reward += rewarderBox.Reward();
        // reward += rewarderBoxM.Reward();
        // reward += rewarderBoxN.Reward();
        //
        // reward /= 4;
        reward += -1f; //Time penalty

        // if ((targetPosition.position - target.position).magnitude < 0.08f)
        // {
        //     // reward += 10;
        //     targetPosition.GetComponent<TargetPositionRandomizer>().RandomizeWithRespectTo(transform);
        // }

        return reward / MaxStep; //Normalize to -1 : 1 per episode
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
}