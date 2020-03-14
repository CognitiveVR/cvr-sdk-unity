using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO merge this with exitpoll HMD pointer

public class HMDGazePointer : MonoBehaviour
{
	void Update ()
	{
        Ray ray = CognitiveVR.GazeHelper.GetCurrentWorldGazeRay();
        UpdateDrawLine(ray);
        Debug.DrawRay(transform.position, ray.direction * 10, Color.red);
    }

    IGazeFocus UpdateDrawLine(Ray ray)
    {
        IGazeFocus button = null;

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 10)) //hit a button
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
