using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//basic XR Rig movement script based on inputs from the left controller's joystick

namespace Cognitive3D.Demo
{
    public class PlayerMoveXR : MonoBehaviour
    {
		[SerializeField]
        private float Speed = 1;
		
        void FixedUpdate()
        {
            var leftController = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            Vector2 axis = Vector2.zero;
            leftController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out axis);

            Vector3 direction = new Vector3(axis.x, 0, axis.y);
            transform.position += direction * Speed * Time.deltaTime;
        }
    }
}