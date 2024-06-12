using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.Serialization;

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

    public ArticulationBody wheelYawLFront;
    public ArticulationBody wheelYawRFront;
    public ArticulationBody wheelYawLBack;
    public ArticulationBody wheelYawRBack;

    public ArticulationBody wheelVelLFront;
    public ArticulationBody wheelVelRFront;
    public ArticulationBody wheelVelLBack;
    public ArticulationBody wheelVelRBack;

    public Camera eyeCamera;
    public Camera thirdPersonCamera;
    public Camera frontCamera;
    public Camera topCamera;

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

        target.GetComponent<TargetPositionRandomizer>().RandomizeWithRespectTo(m_chain.hips.transform);
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
            if (!bodyPart.name.ToLower().Contains("wheel"))
            {
                CollectObservationBodyPart(bodyPart, sensor);
            }
        }
    }

    public void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Alpha1))
        {
            eyeCamera.transform.gameObject.SetActive(true);
            thirdPersonCamera.transform.gameObject.SetActive(false);
            frontCamera.transform.gameObject.SetActive(false);
            topCamera.transform.gameObject.SetActive(false);
        }

        if (Input.GetKey(KeyCode.Alpha2))
        {
            eyeCamera.transform.gameObject.SetActive(false);
            thirdPersonCamera.transform.gameObject.SetActive(true);
            frontCamera.transform.gameObject.SetActive(false);
            topCamera.transform.gameObject.SetActive(false);
        }

        if (Input.GetKey(KeyCode.Alpha3))
        {
            eyeCamera.transform.gameObject.SetActive(false);
            thirdPersonCamera.transform.gameObject.SetActive(false);
            frontCamera.transform.gameObject.SetActive(true);
            topCamera.transform.gameObject.SetActive(false);
        }

        if (Input.GetKey(KeyCode.Alpha4))
        {
            eyeCamera.transform.gameObject.SetActive(false);
            thirdPersonCamera.transform.gameObject.SetActive(false);
            frontCamera.transform.gameObject.SetActive(false);
            topCamera.transform.gameObject.SetActive(true);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;

        ExecuteBehavior(continuousActions, actionBuffers.DiscreteActions[0]);

        var resetSignal = actionBuffers.DiscreteActions[1];
        if (resetSignal == 1)
        {
            EndEpisode();
        }
    }

    private void ExecuteBehavior(ActionSegment<float> continuousActions, int i)
    {
        switch (i)
        {
            case 1:
                SimplifiedControl(continuousActions);
                break;
            case 0:
                LowLevelControl(continuousActions);
                break;
        }
    }

    private void SimplifiedControl(ActionSegment<float> continuousActions)
    {
        var forward = continuousActions[0];
        var turn = continuousActions[1];
        var rotate = continuousActions[2];
        if (rotate != 0)
        {
            continuousActions[0] = rotate;
            continuousActions[1] = rotate;
            continuousActions[2] = -rotate;
            continuousActions[3] = -rotate;
            continuousActions[4] = 1;
            continuousActions[5] = -1;
            continuousActions[6] = -1;
            continuousActions[7] = 1;
        }

        if (rotate == 0)
        {
            continuousActions[0] = forward;
            continuousActions[1] = forward;
            continuousActions[2] = forward;
            continuousActions[3] = forward;
            continuousActions[4] = 0.4f * turn;
            continuousActions[5] = 0.4f * -turn;
            continuousActions[6] = 0.4f * turn;
            continuousActions[7] = 0.4f * -turn;
        }

        LowLevelControl(continuousActions);
    }

    private void LowLevelControl(ActionSegment<float> continuousActions)
    {
        int i = -1;
        var forwardLF = continuousActions[++i];
        var forwardLB = continuousActions[++i];
        var forwardRF = continuousActions[++i];
        var forwardRB = continuousActions[++i];
        var turnLF = continuousActions[++i];
        var turnLB = continuousActions[++i];
        var turnRF = continuousActions[++i];
        var turnRB = continuousActions[++i];

        wheelVelLFront.SetDriveTargetVelocity(ArticulationDriveAxis.X, forwardLF * 400);
        wheelVelRFront.SetDriveTargetVelocity(ArticulationDriveAxis.X, forwardRF * 400);
        wheelVelLBack.SetDriveTargetVelocity(ArticulationDriveAxis.X, forwardLB * 400);
        wheelVelRBack.SetDriveTargetVelocity(ArticulationDriveAxis.X, forwardRB * 400);

        m_chain.DriveControllers[wheelYawLFront].SetDriveTargets(turnLF, 0, 0);
        m_chain.DriveControllers[wheelYawRFront].SetDriveTargets(turnRF, 0, 0);
        m_chain.DriveControllers[wheelYawLBack].SetDriveTargets(turnLB, 0, 0);
        m_chain.DriveControllers[wheelYawRBack].SetDriveTargets(turnRB, 0, 0);

        m_chain.DriveControllers[m_chain.spine].SetDriveTargets(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        m_chain.DriveControllers[m_chain.chest].SetDriveTargets(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        m_chain.DriveControllers[m_chain.head].SetDriveTargets(continuousActions[++i], continuousActions[++i], 0);

        m_chain.DriveControllers[m_chain.armL].SetDriveTargets(continuousActions[++i], continuousActions[++i], 0);
        m_chain.DriveControllers[m_chain.forearmL].SetDriveTargets(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.handL].SetDriveTargets(0, continuousActions[++i], 0);

        m_chain.DriveControllers[m_chain.armR].SetDriveTargets(continuousActions[++i], continuousActions[++i], 0);
        m_chain.DriveControllers[m_chain.forearmR].SetDriveTargets(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.handR].SetDriveTargets(0, continuousActions[++i], 0);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        if (Input.GetKey("w"))
        {
            actions[0] = 1;
        }
        else if (Input.GetKey("s"))
        {
            actions[0] = -1;
        }
        else
        {
            actions[0] = 0;
        }

        if (Input.GetKey("a"))
        {
            actions[1] = -1;
        }
        else if (Input.GetKey("d"))
        {
            actions[1] = 1;
        }
        else
        {
            actions[1] = 0;
        }

        if (Input.GetKey("q"))
        {
            actions[2] = -1;
        }
        else if (Input.GetKey("e"))
        {
            actions[2] = 1;
        }
        else
        {
            actions[2] = 0;
        }

        var actionsOutDiscreteActions = actionsOut.DiscreteActions;
        actionsOutDiscreteActions[0] = 1;
        actionsOutDiscreteActions[1] = Input.GetKey("space") ? 1 : 0;
    }
}