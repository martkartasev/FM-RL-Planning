using UnityEngine;

namespace Scripts.Game
{
    public class GoalColorIndicator : MonoBehaviour
    {
        public Color SuccessColor = Color.blue;
        private Goal goal;

        public void SetGoal(Goal goal)
        {
            this.goal = goal;
        }

        private void FixedUpdate()
        {
            if (goal != null && goal.IsAchieved())
            {
                gameObject.GetComponent<Renderer>().sharedMaterial.color = SuccessColor;
            }
        }
    }
}