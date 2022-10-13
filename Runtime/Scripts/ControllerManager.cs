using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ControllerManager : MonoBehaviour
{
    List<InputDevice> devices;

    // Start is called before the first frame update
    void Start()
    {
        devices = new List<InputDevice>();
    }

    // Update is called once per frame
    void Update()
    {
        InputDevices.GetDevices(devices);
        if (devices.Count == 0)
        {
            Debug.Log("BAD TEST: No Devices found");
        }
        else
        {
            foreach (InputDevice currentDevice in devices)
            {
                Debug.Log("In Controller Manager, the input device is: " + currentDevice.name);
            }
        }
    }
}
