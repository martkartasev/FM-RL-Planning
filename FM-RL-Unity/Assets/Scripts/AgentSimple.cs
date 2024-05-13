using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AgentSimple : Agent
{
    [Header("Target")] public Transform target; //Target the agent will try to grasp.
    [Header("Target Position")] public Transform targetPosition;

    private ArticulationChainComponent m_chain;

    private ClosenessRewarder rewarderBox;
    private ClosenessRewarder rewarderBoxM;
    private ClosenessRewarder rewarderBoxN;
    private OnlyImprovingRewarder rewarderLHand;
    private OnlyImprovingRewarder rewarderRHand;


    public override void Initialize()
    {
        m_chain = GetComponent<ArticulationChainComponent>();
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        m_chain.Restart(m_chain.hips.transform.parent.TransformPoint(new Vector3(0, 0.1f, 0)), Quaternion.Euler(transform.parent.TransformDirection(Vector3.zero)));
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

        //Get position relative to hips in the context of our orientation cube's space
        sensor.AddObservation(hips.transform.InverseTransformDirection(bp.transform.position - hips.transform.position));

        if (bp != hips)
        {
            sensor.AddObservation(bp.transform.localRotation);
            //sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(target.transform.position));
        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(targetPosition.transform.position));

        foreach (var bodyPart in m_chain.bodyParts)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }
    }


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;
        var i = -1;

        var forward = continuousActions[++i];
        var turn = continuousActions[++i];

        m_chain.root.AddRelativeForce(Vector3.forward * forward * 6.5f, ForceMode.Acceleration);
        m_chain.root.AddRelativeTorque(Vector3.up * turn * 25, ForceMode.Acceleration);

        m_chain.DriveControllers[m_chain.spine].SetDriveTargets(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        m_chain.DriveControllers[m_chain.chest].SetDriveTargets(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        m_chain.DriveControllers[m_chain.head].SetDriveTargets(continuousActions[++i], continuousActions[++i], 0);

        m_chain.DriveControllers[m_chain.armL].SetDriveTargets(continuousActions[++i], continuousActions[++i], 0);
        m_chain.DriveControllers[m_chain.forearmL].SetDriveTargets(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.handL].SetDriveTargets(0, continuousActions[++i], 0);

        m_chain.DriveControllers[m_chain.armR].SetDriveTargets(continuousActions[++i], continuousActions[++i], 0);
        m_chain.DriveControllers[m_chain.forearmR].SetDriveTargets(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.handR].SetDriveTargets(0, continuousActions[++i], 0);

        var resetSignal = actionBuffers.DiscreteActions[1];
        if (resetSignal == 1)
        {
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        if (Input.GetKey("up"))
        {
            actions[0] = 1;
        }
        else if (Input.GetKey("down"))
        {
            actions[0] = -1;
        }
        else
        {
            actions[0] = 0;
        }

        if (Input.GetKey("left"))
        {
            actions[1] = -1;
        }
        else if (Input.GetKey("right"))
        {
            actions[1] = 1;
        }
        else
        {
            actions[1] = 0;
        }

        var actionsOutDiscreteActions = actionsOut.DiscreteActions;
        actionsOutDiscreteActions[0] = 0;
        actionsOutDiscreteActions[1] = Input.GetKey("space") ? 1 : 0;
    }
}