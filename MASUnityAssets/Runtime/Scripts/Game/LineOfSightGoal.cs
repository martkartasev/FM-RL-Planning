using UnityEngine;

namespace Scripts.Game
{
    public class LineOfSightGoal : AbstractGoal
    {
        public readonly Vector3 targetPosition;
        private readonly float maxTargetDistance;

        public LineOfSightGoal(Vector3 position, float maxDistance)
        {
            targetPosition = position;
            maxTargetDistance = maxDistance;
        }

        public override bool CheckAchieved(GameObject objectToCheck)
        {
            if ((objectToCheck.transform.position - targetPosition).magnitude < maxTargetDistance)
            {
                var objectPosition = objectToCheck.transform.position;

                var direction = targetPosition - objectPosition;
                direction.Scale(new(1, 0, 1));

                var obscured = Physics.Raycast(objectPosition + Vector3.up, direction, out RaycastHit hit, direction.magnitude);
                if (!obscured)
                {
                    completionTime = Time.time - startTime;
                    achieved = true;
                }
            }

            return achieved;
        }
    }
}