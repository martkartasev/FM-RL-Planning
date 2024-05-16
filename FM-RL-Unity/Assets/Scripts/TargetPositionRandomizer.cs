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
    private ArticulationBody body;

    public void Start()
    {
        body = GetComponent<ArticulationBody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void Randomize()
    {
        var transformPosition = initialPosition + transform.right * Random.Range(-0.5f, 0.5f) + transform.forward * Random.Range(-0.2f, 0.2f);
        if (body != null)
        {
            body.TeleportRoot(transformPosition, initialRotation);
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.rotation = initialRotation;
            transform.position = transformPosition;
        }
    }

    public void RandomizeWithRespectTo(Transform transform)
    {
        var position = transform.position + transform.forward * Random.Range(0.6f, 1.0f) + transform.up * Random.Range(0.8f, 1.2f) + transform.right * Random.Range(-0.5f, 0.5f);
        if (body != null)
        {
            body.TeleportRoot(position, initialRotation);
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        else
        {
            this.transform.rotation = Quaternion.Euler(0, 0, 0);
            this.transform.position = position;
        }
    }
}