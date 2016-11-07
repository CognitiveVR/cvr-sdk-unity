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
                Vector3 pos = CognitiveVR_Manager.GetControllerPosition(0);

                hit = Physics.CheckSphere(pos, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
                if (hit && string.IsNullOrEmpty(controller0GUID))
                {
                    Util.logDebug("controller collision");
                    controller0GUID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("cvr.collision", controller0GUID).setProperty("device", "controller 0").begin();
                }
                else if (!hit && !string.IsNullOrEmpty(controller0GUID))
                {
                    Instrumentation.Transaction("cvr.collision", controller0GUID).setProperty("device", "controller 0").end();
                    controller0GUID = string.Empty;
                }
            }


#if CVR_STEAMVR
            if (CognitiveVR_Manager.GetController(1) != null)
#endif
            {
                Vector3 pos = CognitiveVR_Manager.GetControllerPosition(1);



                hit = Physics.CheckSphere(pos, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
                if (hit && string.IsNullOrEmpty(controller1GUID))
                {
                    Util.logDebug("controller collision");
                    controller1GUID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("cvr.collision", controller1GUID).setProperty("device", "controller 1").begin();
                }
                else if (!hit && !string.IsNullOrEmpty(controller1GUID))
                {
                    Instrumentation.Transaction("cvr.collision", controller1GUID).setProperty("device", "controller 1").end();
                    controller1GUID = string.Empty;
                }
            }
        }

        public static string GetDescription()
        {
            return "Sends transactions when either controller collides in the game world\nCollision layers are set in CognitiveVR_Preferences\nRequires SteamVR controllers or Oculus Touch controllers";
        }
    }
}