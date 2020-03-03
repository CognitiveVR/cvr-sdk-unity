using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO merge this with exitpoll HMD pointer

public class HMDGazePointer : MonoBehaviour
{
    CognitiveVR.GazeReticle gazeReticle;

    void Start()
    {
        gazeReticle = FindObjectOfType<CognitiveVR.GazeReticle>();
    }
	void Update ()
	{
        Vector3 dir = gazeReticle.GetLookDirection();
        UpdateDrawLine(dir);
        Debug.DrawRay(transform.position, dir * 10, Color.red);
    }

    IGazeFocus UpdateDrawLine(Vector3 direction)
    {
        Vector3 pos = transform.position;
        Vector3 forward = direction;

        IGazeFocus button = null;

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(pos, forward, out hit, 10)) //hit a button
        {
            button = hit.collider.GetComponent<IGazeFocus>();
            if (button != null)
            {
                button.SetFocus();
            }
        }
        return button;
    }
}
