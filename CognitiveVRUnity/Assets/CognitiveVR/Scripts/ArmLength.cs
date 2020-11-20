using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if CVR_STEAMVR || CVR_STEAMVR2
using Valve.VR;
#endif

/// <summary>
/// samples distances the the HMD to the player's arm. max is assumed to be roughly player arm length
/// this only starts tracking when the player has pressed a button/trigger/grip
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/Arm Length")]
    public class ArmLength : CognitiveVRAnalyticsComponent
    {
        [ClampSetting(5,100)]
        [Tooltip("Number of samples taken. The max is assumed to be maximum arm length")]
        public int SampleCount = 50;
        [ClampSetting(0.1f)]
        public float Interval = 1;

        [ClampSetting(0,50)]
        [Tooltip("Distance from HMD to average shoulder height")]
        public float EyeToShoulderHeight = 0.186f; //meters

#pragma warning disable 649
        //if the left controller isn't null and has had trigger input
        bool leftControllerTracking;
        //if the right controller isn't null and has had trigger input
        bool rightControllerTracking;
#pragma warning restore 649
        GameplayReferences.ControllerInfo tempInfo = null;

#if CVR_STEAMVR
        
        SteamVR_Controller.Device leftController;
        SteamVR_Controller.Device rightController;

        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);

            Core.UpdateEvent += CognitiveVR_Manager_UpdateEvent;
        }

        bool anyControllerTracking = false;
        private void CognitiveVR_Manager_UpdateEvent(float deltaTime)
        {
            if (GameplayReferences.HMD == null) { Core.UpdateEvent -= CognitiveVR_Manager_UpdateEvent; return; }
            //get left controller device
            if (leftController == null && GameplayReferences.GetControllerInfo(false, out tempInfo))
            {
                var leftObject = tempInfo.transform.GetComponent<SteamVR_TrackedObject>();
                if (leftObject != null)
                {
                    leftController = SteamVR_Controller.Input((int)leftObject.index);
                }
            }

            //get right controller device
            if (rightController == null && GameplayReferences.GetControllerInfo(true, out tempInfo))
            {
                var rightObject = tempInfo.transform.GetComponent<SteamVR_TrackedObject>();
                if (rightObject != null)
                {
                    rightController = SteamVR_Controller.Input((int)rightObject.index);
                }
            }

            if (!rightControllerTracking && rightController != null && rightController.GetHairTriggerDown())
            {
                //start coroutine if not started already
                rightControllerTracking = true;
                if (!anyControllerTracking)
                {
                    anyControllerTracking = true;
                    StartCoroutine(Tick());
                }
            }
            
            if (!leftControllerTracking && leftController != null && leftController.GetHairTriggerDown())
            {
                //start coroutine if not started already
                leftControllerTracking = true;
                if (!anyControllerTracking)
                {
                    anyControllerTracking = true;
                    StartCoroutine(Tick());
                }
            }

            //if both controllers are actively tracking distance, stop this callback to check for controllers that become active
            if (leftControllerTracking && rightControllerTracking)
            {
                Core.UpdateEvent -= CognitiveVR_Manager_UpdateEvent;
            }
        }
#endif

#if CVR_STEAMVR2

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            if (GameplayReferences.HMD == null) { return; }
            rightControllerTracking = true;
            leftControllerTracking = true;
            StartCoroutine(Tick());
        }
#endif

#if CVR_PICONEO2EYE

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            if (GameplayReferences.HMD == null) { return; }
            //IMPROVEMENT - wait for participant input from controllers
            rightControllerTracking = true;
            leftControllerTracking = true;
            StartCoroutine(Tick());
        }
#endif

#if CVR_OCULUS
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
            Core.UpdateEvent += CognitiveVR_Manager_OnUpdate;
        }

        private void CognitiveVR_Manager_OnUpdate(float deltaTime)
        {
            if (OVRInput.GetDown(OVRInput.Button.Any))
            {
                Core.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
                if (GameplayReferences.HMD == null) { return; }
                rightControllerTracking = true;
                leftControllerTracking = true;
                StartCoroutine(Tick());
            }
        }
#endif

        IEnumerator Tick()
        {
            int samples = 0;
            float maxSqrDistance = 0;

            while (samples < SampleCount)
            {
                yield return new WaitForSeconds(Interval);
                
                //if left controller is active, record max distance
                if (leftControllerTracking && GameplayReferences.GetControllerInfo(false, out tempInfo))
                {
                    if (tempInfo.connected && tempInfo.visible)
                    {
                        maxSqrDistance = Mathf.Max(maxSqrDistance, Vector3.SqrMagnitude(tempInfo.transform.position - (GameplayReferences.HMD.position - GameplayReferences.HMD.up * EyeToShoulderHeight)));
                    }
                }

                //if right controller is active, record max distance
                if (rightControllerTracking && GameplayReferences.GetControllerInfo(true, out tempInfo))
                {
                    if (tempInfo.connected && tempInfo.visible)
                    {
                        maxSqrDistance = Mathf.Max(maxSqrDistance, Vector3.SqrMagnitude(tempInfo.transform.position - (GameplayReferences.HMD.position - GameplayReferences.HMD.up * EyeToShoulderHeight)));
                        //Debug.DrawLine(GameplayReferences.HMD.position, GameplayReferences.HMD.position - GameplayReferences.HMD.up * EyeToShoulderHeight,Color.red,1);
                        //Debug.DrawLine(tempInfo.transform.position, GameplayReferences.HMD.position - GameplayReferences.HMD.up * EyeToShoulderHeight,Color.blue,1);
                    }
                }

                samples++;
            }

            if (maxSqrDistance > 0)
            {
                //send arm length
                float distance = Mathf.Sqrt(maxSqrDistance);
                //dashboard expects centimeters
                Core.SetParticipantProperty("armlength", distance * 100);
            }
        }

        public override string GetDescription()
        {
#if CVR_STEAMVR || CVR_STEAMVR2 || CVR_PICONEO2EYE
            return "Samples distances from the HMD to the player's controller. Max is assumed to be roughly player arm length. This only starts tracking when the player has pressed the Steam Controller Trigger";
#elif CVR_OCULUS
            return "Samples distances from the HMD to the player's controller. Max is assumed to be roughly player arm length. This only starts tracking when the player has pressed any button";
#else
            return "Current platform does not support this component";
#endif
        }

        public override bool GetWarning()
        {
#if CVR_STEAMVR || CVR_STEAMVR2 || CVR_VARJO || CVR_OCULUS || CVR_PICONEO2EYE
            return false;
#else
            return true;
#endif
        }

        void OnDestroy()
        {
#if CVR_OCULUS
            Core.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
#endif
        }


    }
}
