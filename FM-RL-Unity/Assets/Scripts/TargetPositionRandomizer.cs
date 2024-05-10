using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class TargetPositionRandomizer : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void Randomize()
    {
        transform.rotation = initialRotation;
        transform.position = initialPosition + transform.right * Random.Range(-0.5f, 0.5f) + transform.forward * Random.Range(-0.2f, 0.2f);
    }

    public void RandomizeWithRespectTo(Transform transform)
    {
        this.transform.rotation = Quaternion.Euler(0, 0, 0);
        var position = transform.position + transform.forward * Random.Range(0.6f, 1.0f) + transform.up * Random.Range(0.8f, 1.2f) + transform.right * Random.Range(-0.5f, 0.5f);
        this.transform.position = position;
    }
}