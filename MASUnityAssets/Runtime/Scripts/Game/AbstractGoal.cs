using UnityEngine;

namespace Scripts.Game
{
    public abstract class AbstractGoal : Goal
    {
        public abstract bool CheckAchieved(GameObject objectToCheck);

        protected bool achieved;
        protected float completionTime;
        protected float startTime;

        public bool IsAchieved()
        {
            return achieved;
        }

        public void RestartTimer()
        {
            startTime = Time.time;  
            //TODO Need to rewrite so this stuff is managed in a interface method
            achieved = false;
        }

        public float CurrentTime()
        {
            if (achieved) return completionTime;
            return Time.time - startTime;
        }
    }
}