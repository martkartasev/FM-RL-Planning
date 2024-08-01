using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Agent
{
    public class ArticulationChainComponent : MonoBehaviour
    {
        public List<ArticulationBody> bodyParts;
        public Dictionary<ArticulationBody, DriveController> DriveControllers;
        public ArticulationBody hips;
        public ArticulationBody spine;
        public ArticulationBody chest;
        public ArticulationBody head;
        public ArticulationBody armL_pitch;
        public ArticulationBody armL_yaw;
        public ArticulationBody forearmL;
        public ArticulationBody handL;
        public ArticulationBody armR_pitch;
        public ArticulationBody armR_yaw;
        public ArticulationBody forearmR;
        public ArticulationBody handR;
        public ArticulationBody root => hips;

        public void Awake()
        {
            bodyParts.Insert(0, hips);
            bodyParts.Add(spine);
            bodyParts.Add(chest);
            bodyParts.Add(head);
            bodyParts.Add(armL_pitch);
            bodyParts.Add(armL_yaw);
            bodyParts.Add(forearmL);
            bodyParts.Add(handL);
            bodyParts.Add(armR_pitch);
            bodyParts.Add(armR_yaw);
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
                articulationBody.SetDriveTarget(ArticulationDriveAxis.X, x);
                articulationBody.SetDriveTarget(ArticulationDriveAxis.Y, y);
                articulationBody.SetDriveTarget(ArticulationDriveAxis.Z, z);
            }

            public void SetDriveTargetsNorm(float x, float y, float z)
            {
                articulationBody.SetDriveTarget(ArticulationDriveAxis.X, ComputeFromNormalizedDriveTarget(XParameters, x));
                articulationBody.SetDriveTarget(ArticulationDriveAxis.Y, ComputeFromNormalizedDriveTarget(YParameters, y));
                articulationBody.SetDriveTarget(ArticulationDriveAxis.Z, ComputeFromNormalizedDriveTarget(ZParameters, z));
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
                SetDriveStrengthsNorm(x, x, x);
            }

            public void SetDriveStrengthsNorm(float x, float y, float z)
            {
                articulationBody.SetDriveForceLimit(ArticulationDriveAxis.X, ComputeFromNormalizedDriveStrength(XParameters, x));
                articulationBody.SetDriveForceLimit(ArticulationDriveAxis.Y, ComputeFromNormalizedDriveStrength(YParameters, y));
                articulationBody.SetDriveForceLimit(ArticulationDriveAxis.Z, ComputeFromNormalizedDriveStrength(ZParameters, z));
            }


            public float ComputeNormalizedDriveTarget(DriveParameters drive, float unnormalized)
            {
                return 2 * ((unnormalized - drive.lowerLimit) / (drive.upperLimit - drive.lowerLimit)) -1;
            }

            public float ComputeFromNormalizedDriveTarget(DriveParameters drive, float normalized)
            {
                return drive.lowerLimit + (normalized + 1) / 2 * (drive.upperLimit - drive.lowerLimit);
            }

            public float ComputeFromNormalizedDriveStrength(DriveParameters drive, float normalized)
            {
                return (normalized + 1f) * 0.5f * drive.forceLimit;
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
}