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
        m_chain.Restart(transform.parent.TransformPoint(Vector3.zero), Quaternion.Euler(transform.parent.TransformDirection(Vector3.zero)));
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
        
        m_chain.root.AddForce(m_chain.root.transform.forward * forward * 10);
        m_chain.root.AddTorque(m_chain.root.transform.up * turn * 10);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        if (Input.GetKey("up"))
        {
            actions[0] = 1;
        }
        else
        {
            actions[0] = 0;
        }
       
        if (Input.GetKey("left"))
        {
            actions[1] = 1;
        }
        else
        {
            actions[1] = 0;
        }
        
        if (Input.GetKey("right"))
        {
            actions[1] = -1;
        }
        else
        {
            actions[1] = 0;
        }
        
    }
}