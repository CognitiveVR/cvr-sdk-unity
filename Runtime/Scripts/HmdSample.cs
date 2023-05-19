using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D;


public class HmdSample
{
    public float timestamp;
    public Vector3 headAngle;

    public HmdSample(float timestamp, Vector3 headAngle)
    {
        this.timestamp = timestamp;
        this.headAngle = headAngle;
    }
}