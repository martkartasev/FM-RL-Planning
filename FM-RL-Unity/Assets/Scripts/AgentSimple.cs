using System;
using System.Runtime.CompilerServices;
using DefaultNamespace;
using Grpc.Core;
using Ik;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;
using Vector3 = UnityEngine.Vector3;

public class AgentSimple : Agent
{
    [Header("Goal")] public Transform goal;
    [Header("Target Position")] public Transform targetRedPosition;
    public Transform targetYellowPosition;
    public Transform targetBluePosition;

    [FormerlySerializedAs("targetPositionL")]
    public Transform positionArmL;

    [FormerlySerializedAs("targetPositionR")]
    public Transform positionArmR;

    public Transform positionRampTopR;
    public Transform positionRampBottomR;
    public Transform positionRampTopL;
    public Transform positionRampBottomL;
    public Transform positionBridgeNear;
    public Transform positionBridgeFar;

    public ArticulationChainComponent m_chain;

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
    private KinematicsProvider.KinematicsProviderClient client;
    private TaskAwaiter<IKResponse> angles_L;
    private TaskAwaiter<IKResponse> angles_R;

    public MoveToSkill moveToSkill;
    public PickSkill pickSkill;

    public float spineValue;
    public float handLValue;
    public float handRValue;
    public float forwardValue;
    public float turnValue;

    public override void Initialize()
    {
        m_chain = GetComponent<ArticulationChainComponent>();

        var channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
        client = new KinematicsProvider.KinematicsProviderClient(channel);

        angles_R = client.CalculateInverseKinematicsRightAsync(new IKRequest
        {
            Target = PrepareEndEffectorGrpcQuery(positionArmR.position),
            CurrentJoints = {m_chain.armR_yaw.xDrive.target, m_chain.armR_pitch.xDrive.target, m_chain.forearmR.xDrive.target, m_chain.handR.xDrive.target}
        }).GetAwaiter();

        angles_L = client.CalculateInverseKinematicsLeftAsync(new IKRequest
        {
            Target = PrepareEndEffectorGrpcQuery(positionArmL.position),
            CurrentJoints = {m_chain.armL_yaw.xDrive.target, m_chain.armL_pitch.xDrive.target, m_chain.forearmL.xDrive.target, m_chain.handL.xDrive.target}
        }).GetAwaiter();
    }
    
    public override void OnEpisodeBegin()
    {
        m_chain.Restart(m_chain.hips.transform.parent.TransformPoint(new Vector3(0, 0.1f, 0)), Quaternion.Euler(transform.parent.TransformDirection(Vector3.zero)));
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(m_chain.chest.transform.InverseTransformPoint(goal.transform.position));
        sensor.AddObservation(m_chain.chest.transform.InverseTransformPoint(positionArmL.transform.position));
        sensor.AddObservation(m_chain.chest.transform.InverseTransformPoint(positionArmR.transform.position));

        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(targetRedPosition.position));
        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(targetYellowPosition.position));
        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(targetBluePosition.position));
        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(positionRampBottomL.transform.position));
        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(positionRampTopL.transform.position));
        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(positionRampBottomR.transform.position));
        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(positionRampTopR.transform.position));
        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(positionBridgeFar.transform.position));
        sensor.AddObservation(m_chain.hips.transform.InverseTransformPoint(positionBridgeNear.transform.position));
        
        sensor.AddObservation(moveToSkill.done ? 1 : 0);
        sensor.AddObservation(pickSkill.done ? 1 : 0);
    }

    private void ChangeCameraViewPort(int viewPort)
    {
        if (viewPort == 1)
        {
            eyeCamera.transform.gameObject.SetActive(true);
            thirdPersonCamera.transform.gameObject.SetActive(false);
            frontCamera.transform.gameObject.SetActive(false);
            topCamera.transform.gameObject.SetActive(false);
        }

        if (viewPort == 2)
        {
            eyeCamera.transform.gameObject.SetActive(false);
            thirdPersonCamera.transform.gameObject.SetActive(true);
            frontCamera.transform.gameObject.SetActive(false);
            topCamera.transform.gameObject.SetActive(false);
        }

        if (viewPort == 3)
        {
            eyeCamera.transform.gameObject.SetActive(false);
            thirdPersonCamera.transform.gameObject.SetActive(false);
            frontCamera.transform.gameObject.SetActive(true);
            topCamera.transform.gameObject.SetActive(false);
        }

        if (viewPort == 4)
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

        ExecuteBehavior(continuousActions, actionBuffers.DiscreteActions[0],
            actionBuffers.DiscreteActions[1], 
            actionBuffers.DiscreteActions[2]);
        ChangeCameraViewPort(actionBuffers.DiscreteActions[3]);
        var resetSignal = actionBuffers.DiscreteActions[4];
        if (resetSignal == 1)
        {
            EndEpisode();
        }
    }

    private void ExecuteBehavior(ActionSegment<float> continuousActions, int behaviorType, int moveToTarget, int pickTarget)
    {
        switch (behaviorType)
        {
            case 2:
                MoveToSkillControl(moveToTarget);
                PickSkillControl(pickTarget);
                SimplifiedControl(continuousActions);
                break;
            case 1:
                SimplifiedControl(continuousActions);
                break;
            case 0:
                LowLevelControl(continuousActions);
                break;
        }
    }

    private void MoveToSkillControl(int targetObject)
    {
        var targetTransform = IdentifierToTransform(targetObject);
        if (targetTransform != null)
        {
            if (!moveToSkill.enabled) moveToSkill.enabled = enabled;
            moveToSkill.targetPosition = targetTransform;
        }
        else
        {
            moveToSkill.enabled = false;
        }
    }

    private void PickSkillControl(int targetObject)
    {
        var targetTransform = IdentifierToTransform(targetObject);
        if (targetObject != 50 && targetTransform != null)
        {
            if (!pickSkill.enabled) pickSkill.enabled = enabled;
            pickSkill.target = targetTransform;
        }
        else if (targetObject == 50)
        {
            pickSkill.enabled = false;
        }
    }

    public Transform IdentifierToTransform(int targetObject)
    {
        return targetObject switch
        {
            10 => positionRampTopR,
            9 => positionRampBottomR,
            8 => positionRampTopL,
            7 => positionRampBottomL,
            6 => positionBridgeFar,
            5 => positionBridgeNear,
            4 => targetRedPosition,
            3 => targetBluePosition,
            2 => targetYellowPosition,
            1 => goal,
            _ => null
        };
    }

    private void SimplifiedControl(ActionSegment<float> continuousActions)
    {
        var forward = continuousActions[0];
        var turn = continuousActions[1];
        var rotate = continuousActions[2];
        var armR = new Vector3(continuousActions[3], continuousActions[4], continuousActions[5]);
        var armL = new Vector3(continuousActions[6], continuousActions[7], continuousActions[8]);

        if (turnValue != 0.0f)
        {
            rotate = turnValue;
        }

        if (forwardValue != 0.0f)
        {
            forward = forwardValue;
        }

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

        var rResponse = angles_R.GetResult();
        var driveController = m_chain.DriveControllers[m_chain.armR_yaw];
        continuousActions[8] = driveController.ComputeNormalizedDriveTarget(driveController.XParameters, rResponse.JointTargets[0]);
        driveController = m_chain.DriveControllers[m_chain.armR_pitch];
        continuousActions[9] = driveController.ComputeNormalizedDriveTarget(driveController.XParameters, rResponse.JointTargets[1]);
        driveController = m_chain.DriveControllers[m_chain.forearmR];
        continuousActions[10] = driveController.ComputeNormalizedDriveTarget(driveController.XParameters, rResponse.JointTargets[2]);
        driveController = m_chain.DriveControllers[m_chain.handR];
        continuousActions[11] = driveController.ComputeNormalizedDriveTarget(driveController.XParameters, rResponse.JointTargets[3]);

        var lResponse = angles_L.GetResult();
        driveController = m_chain.DriveControllers[m_chain.armL_yaw];
        continuousActions[12] = driveController.ComputeNormalizedDriveTarget(driveController.XParameters, lResponse.JointTargets[0]);
        driveController = m_chain.DriveControllers[m_chain.armL_pitch];
        continuousActions[13] = driveController.ComputeNormalizedDriveTarget(driveController.XParameters, lResponse.JointTargets[1]);
        driveController = m_chain.DriveControllers[m_chain.forearmL];
        continuousActions[14] = driveController.ComputeNormalizedDriveTarget(driveController.XParameters, lResponse.JointTargets[2]);
        driveController = m_chain.DriveControllers[m_chain.handL];
        continuousActions[15] = driveController.ComputeNormalizedDriveTarget(driveController.XParameters, lResponse.JointTargets[3]);

        if(armR != Vector3.zero) positionArmR.localPosition = armR;
        if(armL != Vector3.zero) positionArmL.localPosition = armL;

        angles_R = client.CalculateInverseKinematicsRightAsync(new IKRequest
        {
            Target = PrepareEndEffectorGrpcQuery(positionArmR.position),
            CurrentJoints = {m_chain.armR_yaw.xDrive.SafeTarget(), m_chain.armR_pitch.xDrive.SafeTarget(), m_chain.forearmR.xDrive.SafeTarget(), m_chain.handR.xDrive.SafeTarget()}
        }).GetAwaiter();
        angles_L = client.CalculateInverseKinematicsLeftAsync(new IKRequest
        {
            Target = PrepareEndEffectorGrpcQuery(positionArmL.position),
            CurrentJoints = {m_chain.armL_yaw.xDrive.SafeTarget(), m_chain.armL_pitch.xDrive.SafeTarget(), m_chain.forearmL.xDrive.SafeTarget(), m_chain.handL.xDrive.SafeTarget()}
        }).GetAwaiter();

        if (spineValue != 0.0f && handLValue != 0.0f && handRValue != 0.0f)
        {
            continuousActions[16] = spineValue;
            continuousActions[11] = handLValue;
            continuousActions[15] = handRValue;
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

        m_chain.DriveControllers[wheelYawLFront].SetDriveTargetsNorm(turnLF, 0, 0);
        m_chain.DriveControllers[wheelYawRFront].SetDriveTargetsNorm(turnRF, 0, 0);
        m_chain.DriveControllers[wheelYawLBack].SetDriveTargetsNorm(turnLB, 0, 0);
        m_chain.DriveControllers[wheelYawRBack].SetDriveTargetsNorm(turnRB, 0, 0);

        m_chain.DriveControllers[m_chain.armR_yaw].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.armR_pitch].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.forearmR].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.handR].SetDriveTargetsNorm(continuousActions[++i], 0, 0);

        m_chain.DriveControllers[m_chain.armL_yaw].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.armL_pitch].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.forearmL].SetDriveTargetsNorm(continuousActions[++i], 0, 0);
        m_chain.DriveControllers[m_chain.handL].SetDriveTargetsNorm(continuousActions[++i], 0, 0);

        m_chain.DriveControllers[m_chain.spine].SetDriveTargetsNorm(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        m_chain.DriveControllers[m_chain.chest].SetDriveTargetsNorm(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        m_chain.DriveControllers[m_chain.head].SetDriveTargetsNorm(continuousActions[++i], continuousActions[++i], 0);
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

        actions[3] = positionArmR.localPosition.x;
        actions[4] = positionArmR.localPosition.y;
        actions[5] = positionArmR.localPosition.z;
        actions[6] = positionArmL.localPosition.x;
        actions[7] = positionArmL.localPosition.y;
        actions[8] = positionArmL.localPosition.z;

        var actionsOutDiscreteActions = actionsOut.DiscreteActions;
        actionsOutDiscreteActions[0] = 1;
        actionsOutDiscreteActions[2] = Input.GetKey("space") ? 1 : 0;

        if (Input.GetKey(KeyCode.Alpha1))
        {
            actionsOutDiscreteActions[1] = 1;
        }

        if (Input.GetKey(KeyCode.Alpha2))
        {
            actionsOutDiscreteActions[1] = 2;
        }

        if (Input.GetKey(KeyCode.Alpha3))
        {
            actionsOutDiscreteActions[1] = 3;
        }

        if (Input.GetKey(KeyCode.Alpha4))
        {
            actionsOutDiscreteActions[1] = 4;
        }
    }


    private Ik.Vector3 PrepareEndEffectorGrpcQuery(Vector3 position)
    {
        var vector3 = new Ik.Vector3();
        var baseVec = m_chain.chest.transform.InverseTransformPoint(position);
        vector3.X = baseVec.x;
        vector3.Y = baseVec.y;
        vector3.Z = baseVec.z;
        return vector3;
    }
}