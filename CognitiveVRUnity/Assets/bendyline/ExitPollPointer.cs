using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//takes a curve and samples a limited number of points
//these are available as 'points'

public class ExitPollPointer : MonoBehaviour {


    private void Start()
    {
        path = new Vector3[section+1];
        points = new Vector3[section+1];
        _t = transform;
        line.positionCount = section;
    }

    //==================RAW CURVE STUFF

    Vector3[] pts = new Vector3[4] { Vector3.zero, Vector3.forward * 1, Vector3.forward * 2, Vector3.forward * 3 };

    //TODO if this curve isn't being update, cache these points
    private Vector3[] EvaluatePoints(int sectionCount)
    {
        //path.Clear();
        //path = new List<Vector3>(sectionCount + 1);
        for (int i = 0; i <= sectionCount; i++)
        {
            float normalDist = i / (float)sectionCount;

            float omNormalDist = 1f - normalDist;
            float omNormalDistSqr = omNormalDist * omNormalDist;
            float normalDistSqr = normalDist * normalDist;

            path[i] = new Vector3(pts[0].x * (omNormalDistSqr * omNormalDist) +
                pts[1].x * (3f * omNormalDistSqr * normalDist) +
                pts[2].x * (3f * omNormalDist * normalDistSqr) +
                pts[3].x * (normalDistSqr * normalDist),
                pts[0].y * (omNormalDistSqr * omNormalDist) +
                pts[1].y * (3f * omNormalDistSqr * normalDist) +
                pts[2].y * (3f * omNormalDist * normalDistSqr) +
                pts[3].y * (normalDistSqr * normalDist),
                pts[0].z * (omNormalDistSqr * omNormalDist) +
                pts[1].z * (3f * omNormalDistSqr * normalDist) +
                pts[2].z * (3f * omNormalDist * normalDistSqr) +
                pts[3].z * (normalDistSqr * normalDist));


            //path[i] = GetPoint(i / (float)sectionCount);
        }
        return path;
    }

    private Vector3 GetPoint(float normalDist)
    {
        float omNormalDist = 1f - normalDist;
        float omNormalDistSqr = omNormalDist * omNormalDist;
        float normalDistSqr = normalDist * normalDist;

        return new Vector3(pts[0].x * (omNormalDistSqr * omNormalDist) +
            pts[1].x * (3f * omNormalDistSqr * normalDist) +
            pts[2].x * (3f * omNormalDist * normalDistSqr) +
            pts[3].x * (normalDistSqr * normalDist),
            pts[0].y * (omNormalDistSqr * omNormalDist) +
            pts[1].y * (3f * omNormalDistSqr * normalDist) +
            pts[2].y * (3f * omNormalDist * normalDistSqr) +
            pts[3].y * (normalDistSqr * normalDist),
            pts[0].z * (omNormalDistSqr * omNormalDist) +
            pts[1].z * (3f * omNormalDistSqr * normalDist) +
            pts[2].z * (3f * omNormalDist * normalDistSqr) +
            pts[3].z * (normalDistSqr * normalDist));

        /*return
            pts[0] * (omNormalDistSqr * omNormalDist) +
            pts[1] * (3f * omNormalDistSqr * normalDist) +
            pts[2] * (3f * omNormalDist * normalDistSqr) +
            pts[3] * (normalDistSqr * normalDist);*/
    }

    Vector3[] path;

    //========================= SAMPLE AND SET LINE POINTS

    Vector3[] points;
    public LineRenderer line;
    
    public int section = 10;

    public void Rebuild()
    {
        points = EvaluatePoints(section);
        line.SetPositions(points);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 100);

        if (points == null) { return; }
        for (int j = 0; j < points.Length-1; j++)
        {
            Gizmos.DrawLine(points[j], points[j + 1]);
        }
    }


    //======================= DECIDING WHERE CURVE SHOULD TARGET

    public Transform Target { get; set; }
    public float ForwardPower = 2;

    public float Stickiness = 0.95f;
    Transform _t;

    void Update()
    {
        Vector3 pos = _t.position;
        Vector3 forward = _t.forward;
        if (Target == null) //straighten over time
        {
            pts[0] = pos;
            pts[1] = pos + forward * ForwardPower;
            pts[2] = pos + forward * ForwardPower;
            pts[3] = pos + forward * ForwardPower;

            Rebuild();
            return;
        }

        pts[0] = pos;
        pts[1] = pos + forward * ForwardPower;
        pts[2] = Target.position;
        pts[3] = Target.position;

        Rebuild();
    }
}
