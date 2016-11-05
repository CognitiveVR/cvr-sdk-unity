using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when either the controller collides with something in the game world
/// collision layers are set in CognitiveVR_Preferences
/// </summary>

namespace CognitiveVR
{
    public class ControllerCollisionTracker : CognitiveVRAnalyticsComponent
    {
        string controller0GUID;
        string controller1GUID;

#if CVR_OCULUS
        OVRCameraRig _cameraRig;
        OVRCameraRig CameraRig
        {
            get
            {
                if (_cameraRig == null)
                {
                    _cameraRig = FindObjectOfType<OVRCameraRig>();
                }
                return _cameraRig;
            }
        }
#endif

        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.OnTick += CognitiveVR_Manager_OnTick;
        }

        private void CognitiveVR_Manager_OnTick()
        {
            bool hit;

#if CVR_STEAMVR
            if (CognitiveVR_Manager.GetController(0) != null)
#endif
            {
                Vector3 pos = GetControllerPosition(0);

                hit = Physics.CheckSphere(pos, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
                if (hit && string.IsNullOrEmpty(controller0GUID))
                {
                    Util.logDebug("controller collision");
                    controller0GUID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("cvr.collision", controller0GUID).setProperty("device", "controller 0").begin();
                }
                else if (!hit && !string.IsNullOrEmpty(controller0GUID))
                {
                    Instrumentation.Transaction("cvr.collision", controller0GUID).end();
                    controller0GUID = string.Empty;
                }
            }


#if CVR_STEAMVR
            if (CognitiveVR_Manager.GetController(1) != null)
#endif
            {
                Vector3 pos = GetControllerPosition(1);

                hit = Physics.CheckSphere(pos, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
                if (hit && string.IsNullOrEmpty(controller1GUID))
                {
                    Util.logDebug("controller collision");
                    controller1GUID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("cvr.collision", controller1GUID).setProperty("device", "controller 1").begin();
                }
                else if (!hit && !string.IsNullOrEmpty(controller1GUID))
                {
                    Instrumentation.Transaction("cvr.collision", controller1GUID).end();
                    controller1GUID = string.Empty;
                }
            }
        }

        Vector3 GetControllerPosition(int index)
        {

#if CVR_OCULUS
            if (index == 0)
            {
                if (CameraRig != null)
                    return CameraRig.rightHandAnchor.position;
            }
            else if (index == 1)
            {
                if (CameraRig != null)
                    return CameraRig.leftHandAnchor.position;
            }
#elif CVR_STEAMVR
            return CognitiveVR_Manager.GetController(index).position;
#endif
            return Vector3.zero;
        }

        public static string GetDescription()
        {
            return "Sends transactions when either controller collides in the game world\nCollision layers are set in CognitiveVR_Preferences\nOnly SteamVR controllers are currently supported";
        }
    }
}