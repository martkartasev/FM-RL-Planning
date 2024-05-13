using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ArticulationChainComponent : MonoBehaviour
{
    public List<ArticulationBody> bodyParts;
    public Dictionary<ArticulationBody, DriveController> DriveControllers;
    public ArticulationBody hips;
    public ArticulationBody spine;
    public ArticulationBody chest;
    public ArticulationBody head;
    public ArticulationBody armL;
    public ArticulationBody forearmL;
    public ArticulationBody handL;
    public ArticulationBody armR;
    public ArticulationBody forearmR;
    public ArticulationBody handR;
    public ArticulationBody root => bodyParts[0];

    public void Awake()
    {
        bodyParts = new List<ArticulationBody>();
        bodyParts.Add(hips);
        bodyParts.Add(spine);
        bodyParts.Add(chest);
        bodyParts.Add(head);
        bodyParts.Add(armL);
        bodyParts.Add(forearmL);
        bodyParts.Add(handL);
        bodyParts.Add(armR);
        bodyParts.Add(forearmR);
        bodyParts.Add(handR);


        DriveControllers = bodyParts.Select(bp => (bp, new DriveController(bp)))
            .ToDictionary(tuple => tuple.bp, tuple => tuple.Item2);
    }


    public void Restart(Vector3 position, Quaternion rotation)
    {
        bodyParts[0].TeleportRoot(position, rotation);

        foreach (var bodyPart in DriveControllers.Values)
        {
            bodyPart.ResetArticulationBody();
        }
    }

    public class DriveController
    {
        public DriveParameters XParameters;
        public DriveParameters YParameters;
        public DriveParameters ZParameters;
        public readonly ArticulationBody articulationBody;

        public DriveController(ArticulationBody articulationBody)
        {
            this.articulationBody = articulationBody;
            XParameters = DriveParameters.CreateParameters(articulationBody.xDrive);
            YParameters = DriveParameters.CreateParameters(articulationBody.yDrive);
            ZParameters = DriveParameters.CreateParameters(articulationBody.zDrive);
            this.articulationBody.GetJointForces(new List<float>());
        }

        public void SetDriveTargets(float x, float y, float z)
        {
            articulationBody.SetDriveTarget(ArticulationDriveAxis.X, ComputeNormalizedDriveTarget(XParameters, x));
            articulationBody.SetDriveTarget(ArticulationDriveAxis.Y, ComputeNormalizedDriveTarget(YParameters, y));
            articulationBody.SetDriveTarget(ArticulationDriveAxis.Z, ComputeNormalizedDriveTarget(ZParameters, z));
        }

        public void ResetArticulationBody()
        {
            switch (articulationBody.dofCount)
            {
                case 1:
                    articulationBody.jointPosition = new ArticulationReducedSpace(0f);
                    articulationBody.jointForce = new ArticulationReducedSpace(0f);
                    articulationBody.jointVelocity = new ArticulationReducedSpace(0f);
                    break;
                case 2:
                    articulationBody.jointPosition = new ArticulationReducedSpace(0f, 0f);
                    articulationBody.jointForce = new ArticulationReducedSpace(0f, 0f);
                    articulationBody.jointVelocity = new ArticulationReducedSpace(0f, 0f);
                    break;
                case 3:
                    articulationBody.jointPosition = new ArticulationReducedSpace(0f, 0f, 0f);
                    articulationBody.jointForce = new ArticulationReducedSpace(0f, 0f, 0f);
                    articulationBody.jointVelocity = new ArticulationReducedSpace(0f, 0f, 0f);
                    break;
            }

            articulationBody.velocity = Vector3.zero;
            articulationBody.angularVelocity = Vector3.zero;
        }

        public void SetDriveStrength(float x)
        {
            SetDriveStrengths(x, x, x);
        }

        public void SetDriveStrengths(float x, float y, float z)
        {
            articulationBody.SetDriveForceLimit(ArticulationDriveAxis.X, ComputeNormalizedDriveStrength(XParameters, x));
            articulationBody.SetDriveForceLimit(ArticulationDriveAxis.Y, ComputeNormalizedDriveStrength(YParameters, y));
            articulationBody.SetDriveForceLimit(ArticulationDriveAxis.Z, ComputeNormalizedDriveStrength(ZParameters, z));
        }


        public float ComputeNormalizedDriveTarget(DriveParameters drive, float actionValue)
        {
            return drive.lowerLimit + (actionValue + 1) / 2 * (drive.upperLimit - drive.lowerLimit);
        }

        public float ComputeNormalizedDriveStrength(DriveParameters drive, float actionValue)
        {
            return (actionValue + 1f) * 0.5f * drive.forceLimit;
        }
    }

    public struct DriveParameters
    {
        public float upperLimit;
        public float lowerLimit;
        public float stiffness;
        public float damping;
        public float forceLimit;

        public static DriveParameters CreateParameters(ArticulationDrive drive)
        {
            return new DriveParameters
            {
                upperLimit = drive.upperLimit,
                lowerLimit = drive.lowerLimit,
                stiffness = drive.stiffness,
                damping = drive.damping,
                forceLimit = drive.forceLimit,
            };
        }
    }
}