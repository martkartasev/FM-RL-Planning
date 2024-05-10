using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ArticulationChainComponent : MonoBehaviour
{
    public List<ArticulationBody> bodyParts;
    public Dictionary<ArticulationDrive, DriveParameters> driveParameters;
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


        var valueTuples = bodyParts.SelectMany(bp => new List<(ArticulationDrive, DriveParameters)>()
        {
            (bp.xDrive, DriveParameters.CreateParameters(bp.xDrive)),
            (bp.yDrive, DriveParameters.CreateParameters(bp.yDrive)),
            (bp.zDrive, DriveParameters.CreateParameters(bp.zDrive))
        });
        driveParameters = valueTuples.ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
    }


    public void Restart(Vector3 position, Quaternion rotation)
    {
        bodyParts[0].TeleportRoot(position, rotation);

        foreach (var bodyPart in bodyParts)
        {
            ResetArticulationBody(bodyPart);
        }
    }

    private static void ResetArticulationBody(ArticulationBody bodyPart)
    {
        bodyPart.jointPosition = new ArticulationReducedSpace(0f);
        bodyPart.jointForce = new ArticulationReducedSpace(0f);
        bodyPart.jointVelocity = new ArticulationReducedSpace(0f);
    }

    public float ComputeNormalizedDriveTarget(ArticulationDrive drive, float actionValue)
    {
        return drive.lowerLimit + (actionValue + 1) / 2 * (drive.upperLimit - drive.lowerLimit);
    }

    public float ComputeNormalizedDriveStrength(ArticulationDrive drive, float actionValue)
    {
        return (actionValue + 1f) * 0.5f * driveParameters[drive].forceLimit;
    }

    public struct DriveParameters
    {
        public float stiffness;
        public float damping;
        public float forceLimit;

        public static DriveParameters CreateParameters(ArticulationDrive drive)
        {
            return new DriveParameters
            {
                stiffness = drive.stiffness,
                damping = drive.damping,
                forceLimit = drive.forceLimit,
            };
        }
    }
}