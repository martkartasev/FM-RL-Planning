using System;
using UnityEngine;

public interface IRewarder
{
    float Reward();
    public void Init();
}

public class Rewarder : IRewarder
{
    private float startDistance;
    private float lastDistance;
    private System.Func<float> getDistance;

    public Rewarder(System.Func<float> getDistance)
    {
        this.getDistance = getDistance;
        Init();
    }

    public void Init()
    {
        startDistance = getDistance();
        lastDistance = startDistance;
    }

    public float Reward()
    {
        float distance = getDistance();
        float reward = (lastDistance - distance) / startDistance;
        lastDistance = distance;
        return reward;
    }
}

public class ClosenessRewarder : IRewarder
{
    private float startDistance;
    private System.Func<float> getDistance;
    private readonly float startDistanceMultiplier;

    public ClosenessRewarder(System.Func<float> getDistance, float startMultiplier = 1.0f)
    {
        this.startDistanceMultiplier = startMultiplier;
        this.getDistance = getDistance;
        Init();
    }

    public void Init()
    {
        startDistance = getDistance() * startDistanceMultiplier;
    }

    public float Reward()
    {
        return Mathf.Max((startDistance - getDistance()) / startDistance, 0.0f);
    }
}

public class OnlyImprovingRewarder : IRewarder
{
    private float startDistance;
    private float bestDistance;
    private System.Func<float> getDistance;
    private readonly float startDistanceMultiplier;

    public OnlyImprovingRewarder(System.Func<float> getDistance, float startMultiplier = 1.0f)
    {
        this.startDistanceMultiplier = startMultiplier;
        this.getDistance = getDistance;
        Init();
    }

    public void Init()
    {
        startDistance = this.getDistance() * startDistanceMultiplier;
        bestDistance = this.startDistance;
    }

    public float Reward()
    {
        float distance = getDistance();
        if (distance < bestDistance)
        {
            float reward = (bestDistance - distance) / startDistance;
            bestDistance = distance;
            return reward;
        }

        return 0f;
    }
}