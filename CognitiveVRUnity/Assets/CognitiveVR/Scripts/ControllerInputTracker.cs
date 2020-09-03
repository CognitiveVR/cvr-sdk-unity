using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;
#if CVR_MAGICLEAP
using UnityEngine.XR.MagicLeap;
#endif
#if CVR_STEAMVR || CVR_STEAMVR2
using Valve.VR;
#endif
#if CVR_XR
using UnityEngine.XR;
#endif

//adds controller input properties to dynamic object snapshot for display on sceneexplorer

//TODO XR framework inputs
//magic leap touchpad, trigger, hand pose, buttons
//snapdragon touchpad, buttons
//steamvr1 buttons, touchpad, trigger, grip
//oculus buttons, trigger, joytstick, grip
//leapmotion hand events

namespace CognitiveVR
{

#if CVR_STEAMVR
[RequireComponent(typeof(DynamicObject))]
#endif
public class ControllerInputTracker : MonoBehaviour
{
    [CognitiveVR.Components.ClampSetting(0.1f)]
    public float UpdateRate = 0.1f;
    float nextUpdateTime;
    //records analogue inputs at this interval

    void Start ()
    {
        Init();
    }

#if CVR_STEAMVR
    
    List<ButtonState> CurrentButtonStates = new List<ButtonState>();

    bool isRight;
    DynamicObject dynamic;
    SteamVR_Controller.Device ControllerDevice;

    void Init()
    {
        dynamic = GetComponent<DynamicObject>();
        if (dynamic.IsRight)
            isRight = true;
        else
            isRight = false;
        StartCoroutine(SlowInit());
    }

    IEnumerator SlowInit()
    {
        while(ControllerDevice == null)
        {
            yield return new WaitForSeconds(1);
            SteamVR_TrackedObject o = GetComponent<SteamVR_TrackedObject>();
            if (o != null)
            {
                ControllerDevice = SteamVR_Controller.Input((int)o.index);
            }
            else
            {
                var hand = GetComponent<Valve.VR.InteractionSystem.Hand>();
                if (hand != null)
                {
                    ControllerDevice = hand.controller;
                }
            }
        }
    }

    //updates for interaction hand implementation
    private void Update()
    {
        if (ControllerDevice == null)
        {
            return;
        }

        //menu
        if (ControllerDevice.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
        {
            OnButtonChanged(dynamic, isRight, "vive_menubtn", true, CurrentButtonStates);
        }
        if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_ApplicationMenu))
        {
            OnButtonChanged(dynamic, isRight, "vive_menubtn", false, CurrentButtonStates);
        }

        //home ?? doesn't record event correctly
        //if (ControllerDevice.GetPressDown(EVRButtonId.k_EButton_Dashboard_Back))
        //    OnButtonChanged(dynamic, isRight, "vive_homebtn", true);
        //if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_Dashboard_Back))
        //    OnButtonChanged(dynamic, isRight, "vive_homebtn", false);

        //grip
        if (ControllerDevice.GetPressDown(EVRButtonId.k_EButton_Grip))
        {
            OnButtonChanged(dynamic, isRight, "vive_grip", true, CurrentButtonStates);
        }
        if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_Grip))
        {
            OnButtonChanged(dynamic, isRight, "vive_grip", false, CurrentButtonStates);
        }

        {
            //touchpad touched/pressed
            if (ControllerDevice.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                CurrentTouchpadState = TouchpadState.Press;
                var touchpadaxis = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
                var x = touchpadaxis.x;
                var y = touchpadaxis.y;
                int force = 100;
                Vector3 currentVector = new Vector3(x, y, force);
                OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis, CurrentButtonStates);
                LastTouchpadVector = currentVector;
            }
            else if (ControllerDevice.GetTouchDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                CurrentTouchpadState = TouchpadState.Touch;
                var touchpadaxis = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
                var x = touchpadaxis.x;
                var y = touchpadaxis.y;
                int force = 50;
                Vector3 currentVector = new Vector3(x, y, force);
                OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis, CurrentButtonStates);
                LastTouchpadVector = currentVector;
            }
            else if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                CurrentTouchpadState = TouchpadState.Touch;
                var touchpadaxis = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
                var x = touchpadaxis.x;
                var y = touchpadaxis.y;

                int force = 0;
                if (ControllerDevice.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis0))
                    force = 50;                
                Vector3 currentVector = new Vector3(x, y, force);
                OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis, CurrentButtonStates);
                LastTouchpadVector = currentVector;
            }
            else if (ControllerDevice.GetTouchUp(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                CurrentTouchpadState = TouchpadState.None;
                var touchpadaxis = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
                var x = touchpadaxis.x;
                var y = touchpadaxis.y;
                int force = 0;
                Vector3 currentVector = new Vector3(x, y, force);
                OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis, CurrentButtonStates);
                LastTouchpadVector = currentVector;
            }
        }

        //trigger clicked
        if (ControllerDevice.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
        {
            if (LastTrigger != 100)
            {
                var triggeramount = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
                int currentTrigger = (int)(triggeramount * 100);
                LastTrigger = currentTrigger;
                OnButtonChanged(dynamic, isRight, "vive_trigger", true, CurrentButtonStates);
            }
        }
        else if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
        {
            if (LastTrigger != 0)
            {
                LastTrigger = 0;
                OnButtonChanged(dynamic, isRight, "vive_trigger", false, CurrentButtonStates);
            }
        }

        if (Time.time > nextUpdateTime)
        {
            RecordAnalogInputs(); //should this go at the end? double inputs on triggers
            nextUpdateTime = Time.time + UpdateRate;
        }

        if (CurrentButtonStates.Count > 0)
        {
            List<ButtonState> copy = new List<ButtonState>(CurrentButtonStates.Count);

            for (int i = 0; i < CurrentButtonStates.Count; i++)
            {
                copy.Add(CurrentButtonStates[i]); //move the reference over to the copy
            }
            CurrentButtonStates.Clear();
            DynamicManager.RecordControllerEvent(dynamic.DataId, copy);
        }
    }

    enum TouchpadState
    {
        None,
        Touch,
        Press
    }

    TouchpadState CurrentTouchpadState;
    Vector3 LastTouchpadVector;
    float minMagnitude = 0.05f;
    int LastTrigger;

    //check for (float)triggers, (vector2)touchpads, etc
    public void RecordAnalogInputs()
    {
        if (CurrentTouchpadState != TouchpadState.None)
        {
            var touchpadaxis = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
            var x = touchpadaxis.x;
            var y = touchpadaxis.y;
            int force = CurrentTouchpadState == TouchpadState.None ? 0 : CurrentTouchpadState == TouchpadState.Touch ? 50 : 100;
            Vector3 currentVector = new Vector3(x, y, force);
            if (Vector3.Magnitude(LastTouchpadVector-currentVector)>minMagnitude)
            {
                var touchpadstate = CurrentButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "vive_touchpad"; });
                if (touchpadstate != null)
                {
                    touchpadstate.X = x;
                    touchpadstate.Y = y;
                }
                else
                {
                    OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis, CurrentButtonStates);
                }    
                
                LastTouchpadVector = currentVector;
            }
        }


        var buttonstate = CurrentButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "vive_trigger"; });
        if (buttonstate != null)
        {
            var triggeramount = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
            int currentTrigger = (int)(triggeramount * 100);
            if (LastTrigger != currentTrigger)
            {
                buttonstate.ButtonPercent = currentTrigger;
                LastTrigger = currentTrigger;
            }
        }
        else
        {
            var triggeramount = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
            int currentTrigger = (int)(triggeramount * 100);
            if (LastTrigger != currentTrigger)
            {
                OnSingleChanged(dynamic, isRight, "vive_trigger", currentTrigger, CurrentButtonStates);
                LastTrigger = currentTrigger;
            }
        }
    }

#elif CVR_OCULUS

    enum OculusControllerType
    {
        RiftTouch,
        QuestTouch,
        Go
    }
    OculusControllerType controllerType;

    public DynamicObject LeftHand;
    public DynamicObject RightHand;

    List<ButtonState> CurrentLeftButtonStates = new List<ButtonState>();
    List<ButtonState> CurrentRightButtonStates = new List<ButtonState>();
        
    Vector3 LeftHandVector;
    Vector3 RightHandVector;
    float minMagnitude = 0.05f;
    int LeftTrigger;
    int RightTrigger;

    int LeftGrip;
    int RightGrip;

    void Init()
    {
        var avatar = GetComponent<OVRCameraRig>();
        LeftHand = avatar.leftHandAnchor.GetComponent<DynamicObject>();
        RightHand = avatar.rightHandAnchor.GetComponent<DynamicObject>();
#if UNITY_ANDROID
        controllerType = OculusControllerType.QuestTouch;
#else
        controllerType = OculusControllerType.RiftTouch;
#endif
    }

        //have to do polling every frame to capture inputs
        private void Update()
        {
            if (controllerType == OculusControllerType.RiftTouch)
            {
                UpdateRiftTouch();
                if (Time.time > nextUpdateTime)
                {
                    RecordRiftAnalogInputs();
                    nextUpdateTime = Time.time + UpdateRate;
                }
            }
            else if (controllerType == OculusControllerType.QuestTouch)
            {
                UpdateQuestTouch();
                if (Time.time > nextUpdateTime)
                {
                    RecordQuestAnalogInputs();
                    nextUpdateTime = Time.time + UpdateRate;
                }
            }

            if (CurrentRightButtonStates.Count > 0)
            {
                List<ButtonState> copy = new List<ButtonState>(CurrentRightButtonStates.Count);
                for (int i = 0; i < CurrentRightButtonStates.Count; i++)
                {
                    copy.Add(CurrentRightButtonStates[i]);
                }
                CurrentRightButtonStates.Clear();

                DynamicManager.RecordControllerEvent(RightHand.DataId, copy);
            }
            if (CurrentLeftButtonStates.Count > 0)
            {
                List<ButtonState> copy = new List<ButtonState>(CurrentLeftButtonStates.Count);
                for (int i = 0; i < CurrentLeftButtonStates.Count; i++)
                {
                    copy.Add(CurrentLeftButtonStates[i]);
                }
                CurrentLeftButtonStates.Clear();

                DynamicManager.RecordControllerEvent(LeftHand.DataId, copy);
            }
        }

        void UpdateRiftTouch()
        {

            //right hand a
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_abtn", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_abtn", false, CurrentRightButtonStates);

            //right hand b
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_bbtn", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_bbtn", false, CurrentRightButtonStates);

            //left hand X
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_xbtn", true, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_xbtn", false, CurrentLeftButtonStates);

            //left hand y
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_ybtn", true, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_ybtn", false, CurrentLeftButtonStates);

            //left thumbrest
            if (OVRInput.GetDown(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_thumbrest", true, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_thumbrest", false, CurrentLeftButtonStates);

            //right thumbrest
            if (OVRInput.GetDown(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_thumbrest", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_thumbrest", false, CurrentRightButtonStates);

            //start
            if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
                OnButtonChanged(RightHand, true, "rift_start", true, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.Start, OVRInput.Controller.LTouch))
                OnButtonChanged(RightHand, true, "rift_start", false, CurrentLeftButtonStates);

            //trigger buttons
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_trigger", true, CurrentLeftButtonStates);
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_trigger", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_trigger", false, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_trigger", false, CurrentRightButtonStates);

            //grip
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_grip", true, CurrentLeftButtonStates);
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_grip", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_grip", false, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_grip", false, CurrentRightButtonStates);


            //thumbstick buttons
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_joystick", true, CurrentLeftButtonStates);
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_joystick", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "rift_joystick", false, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "rift_joystick", false, CurrentRightButtonStates);

        }

        void UpdateQuestTouch()
        {
            //right hand a
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "abtn", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "abtn", false, CurrentRightButtonStates);

            //right hand b
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "bbtn", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "bbtn", false, CurrentRightButtonStates);

            //left hand X
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "xbtn", true, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "xbtn", false, CurrentLeftButtonStates);

            //left hand y
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "ybtn", true, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "ybtn", false, CurrentLeftButtonStates);

            //left thumbrest
            if (OVRInput.GetDown(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "thumbrest", true, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "thumbrest", false, CurrentLeftButtonStates);

            //right thumbrest
            if (OVRInput.GetDown(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "thumbrest", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "thumbrest", false, CurrentRightButtonStates);

            //start
            if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
                OnButtonChanged(RightHand, true, "start", true, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.Start, OVRInput.Controller.LTouch))
                OnButtonChanged(RightHand, true, "start", false, CurrentLeftButtonStates);

            //trigger buttons
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "trigger", true, CurrentLeftButtonStates);
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "trigger", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "trigger", false, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "trigger", false, CurrentRightButtonStates);

            //grip
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "grip", true, CurrentLeftButtonStates);
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "grip", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "grip", false, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "grip", false, CurrentRightButtonStates);


            //thumbstick buttons
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "joystick", true, CurrentLeftButtonStates);
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "joystick", true, CurrentRightButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
                OnButtonChanged(LeftHand, false, "joystick", false, CurrentLeftButtonStates);
            if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
                OnButtonChanged(RightHand, true, "joystick", false, CurrentRightButtonStates);

        }

        //polling
        //check for (float)triggers, (vector2)touchpads, etc
        public void RecordRiftAnalogInputs()
        {
            //joysticks
            {
                var touchpadaxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
                var x = touchpadaxis.x;
                var y = touchpadaxis.y;
                int force = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch) ? 100 : 0;
                Vector3 currentVector = new Vector3(x, y, force);
                if (Vector3.Magnitude(LeftHandVector - currentVector) > minMagnitude)
                {
                    var joystick = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "rift_joystick"; });
                    if (joystick != null)
                    {
                        joystick.X = x;
                        joystick.Y = y;
                    }
                    else
                    {
                        OnVectorChanged(LeftHand, false, "rift_joystick", force, touchpadaxis, CurrentLeftButtonStates);
                    }

                    LeftHandVector = currentVector;
                }
            }

            {
                var touchpadaxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
                var x = touchpadaxis.x;
                var y = touchpadaxis.y;
                int force = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch) ? 100 : 0;
                Vector3 currentVector = new Vector3(x, y, force);
                if (Vector3.Magnitude(RightHandVector - currentVector) > minMagnitude)
                {
                    var joystick = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "rift_joystick"; });
                    if (joystick != null)
                    {
                        joystick.X = x;
                        joystick.Y = y;
                    }
                    else
                    {
                        OnVectorChanged(RightHand, true, "rift_joystick", force, touchpadaxis, CurrentRightButtonStates);
                    }
                    RightHandVector = currentVector;
                }
            }

            //triggers
            int currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch) * 100);
            if (LeftTrigger != currentTrigger)
            {
                var trigger = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "rift_trigger"; });
                if (trigger != null)
                {
                    trigger.ButtonPercent = currentTrigger;
                }
                else
                {
                    OnSingleChanged(LeftHand, false, "rift_trigger", currentTrigger, CurrentLeftButtonStates);
                }
                LeftTrigger = currentTrigger;
            }
            currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) * 100);
            if (RightTrigger != currentTrigger)
            {
                var trigger = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "rift_trigger"; });
                if (trigger != null)
                {
                    trigger.ButtonPercent = currentTrigger;
                }
                else
                {
                    OnSingleChanged(RightHand, true, "rift_trigger", currentTrigger, CurrentRightButtonStates);
                }
                RightTrigger = currentTrigger;
            }

            //grips
            currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch) * 100);
            if (LeftGrip != currentTrigger)
            {
                var grip = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "rift_grip"; });
                if (grip != null)
                {
                    grip.ButtonPercent = currentTrigger;
                }
                else
                {
                    OnSingleChanged(LeftHand, false, "rift_grip", currentTrigger, CurrentLeftButtonStates);
                }
                LeftGrip = currentTrigger;
            }
            currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch) * 100);
            if (RightGrip != currentTrigger)
            {
                var grip = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "rift_grip"; });
                if (grip != null)
                {
                    grip.ButtonPercent = currentTrigger;
                }
                else
                {
                    OnSingleChanged(RightHand, true, "rift_grip", currentTrigger, CurrentRightButtonStates);
                }
                RightGrip = currentTrigger;
            }
        }

        //polling
        //check for (float)triggers, (vector2)touchpads, etc
        public void RecordQuestAnalogInputs()
        {
            //joysticks
            {
                var touchpadaxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
                var x = touchpadaxis.x;
                var y = touchpadaxis.y;
                int force = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch) ? 100 : 0;
                Vector3 currentVector = new Vector3(x, y, force);
                if (Vector3.Magnitude(LeftHandVector - currentVector) > minMagnitude)
                {
                    var joystick = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "joystick"; });
                    if (joystick != null)
                    {
                        joystick.X = x;
                        joystick.Y = y;
                    }
                    else
                    {
                        OnVectorChanged(LeftHand, false, "joystick", force, touchpadaxis, CurrentLeftButtonStates);
                    }

                    LeftHandVector = currentVector;
                }
            }

            {
                var touchpadaxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
                var x = touchpadaxis.x;
                var y = touchpadaxis.y;
                int force = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch) ? 100 : 0;
                Vector3 currentVector = new Vector3(x, y, force);
                if (Vector3.Magnitude(RightHandVector - currentVector) > minMagnitude)
                {
                    var joystick = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "joystick"; });
                    if (joystick != null)
                    {
                        joystick.X = x;
                        joystick.Y = y;
                    }
                    else
                    {
                        OnVectorChanged(RightHand, true, "joystick", force, touchpadaxis, CurrentRightButtonStates);
                    }
                    RightHandVector = currentVector;
                }
            }

            //triggers
            int currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch) * 100);
            if (LeftTrigger != currentTrigger)
            {
                var trigger = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "trigger"; });
                if (trigger != null)
                {
                    trigger.ButtonPercent = currentTrigger;
                }
                else
                {
                    OnSingleChanged(LeftHand, false, "trigger", currentTrigger, CurrentLeftButtonStates);
                }
                LeftTrigger = currentTrigger;
            }
            currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) * 100);
            if (RightTrigger != currentTrigger)
            {
                var trigger = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "trigger"; });
                if (trigger != null)
                {
                    trigger.ButtonPercent = currentTrigger;
                }
                else
                {
                    OnSingleChanged(RightHand, true, "trigger", currentTrigger, CurrentRightButtonStates);
                }
                RightTrigger = currentTrigger;
            }

            //grips
            currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch) * 100);
            if (LeftGrip != currentTrigger)
            {
                var grip = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "grip"; });
                if (grip != null)
                {
                    grip.ButtonPercent = currentTrigger;
                }
                else
                {
                    OnSingleChanged(LeftHand, false, "grip", currentTrigger, CurrentLeftButtonStates);
                }
                LeftGrip = currentTrigger;
            }
            currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch) * 100);
            if (RightGrip != currentTrigger)
            {
                var grip = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "grip"; });
                if (grip != null)
                {
                    grip.ButtonPercent = currentTrigger;
                }
                else
                {
                    OnSingleChanged(RightHand, true, "grip", currentTrigger, CurrentRightButtonStates);
                }
                RightGrip = currentTrigger;
            }
        }

#elif CVR_MAGICLEAP
    
    List<ButtonState> CurrentButtonStates = new List<ButtonState>();
    private ControllerConnectionHandler _controllerConnectionHandler;
    DynamicObject controllerDynamic;
    void Init()
    {
        //hands
        MLHands.Start();

        //controller
        MLInput.OnControllerButtonUp += HandleOnButtonUp;
        MLInput.OnControllerButtonDown += HandleOnButtonDown;
        _controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();
        if (_controllerConnectionHandler != null)
        {
            controllerDynamic = GetComponent<DynamicObject>();
        }
    }

    private void HandleOnButtonDown(byte controllerId, MLInputControllerButton button)
    {
        if (controllerDynamic == null) { return; }
        if (_controllerConnectionHandler.IsControllerValid() && _controllerConnectionHandler.ConnectedController.Id == controllerId &&
            button == MLInputControllerButton.Bumper)
        {
            OnButtonChanged(controllerDynamic, true, "bumper",true,CurrentButtonStates);
        }
    }

    private void HandleOnButtonUp(byte controllerId, MLInputControllerButton button)
    {
        if (controllerDynamic == null) { return; }
        if (_controllerConnectionHandler.IsControllerValid() && _controllerConnectionHandler.ConnectedController.Id == controllerId &&
            button == MLInputControllerButton.Bumper)
        {
            OnButtonChanged(controllerDynamic, true, "bumper",false,CurrentButtonStates);
        }
    }

    void RecordAnalogInputs()
    {
        if (controllerDynamic == null) { return; }
        //controller
        MLInputController controller = _controllerConnectionHandler.ConnectedController;
        OnSingleChanged(controllerDynamic, true, "trigger",controller.TriggerValue,CurrentButtonStates);
        OnVectorChanged(controllerDynamic, true, "touchpad",(int)controller.Touch1PosAndForce.z, controller.Touch1PosAndForce.x, controller.Touch1PosAndForce.y,CurrentButtonStates);

        //hands
        //confidence over 60%, then greater than 40%
    }

    void OnDestroy()
    {
        MLInput.OnControllerButtonDown -= HandleOnButtonDown;
        MLInput.OnControllerButtonUp -= HandleOnButtonUp;
    }
#elif CVR_SNAPDRAGON

        List<ButtonState> CurrentButtonStates = new List<ButtonState>();
    DynamicObject controllerDynamic;
    void Init()
    {
        //only supports a single controller for now!
        if (SvrInput.Controller != null)
            controllerDynamic = SvrInput.Controller.GetComponent<DynamicObject>();
    }

    private void Update()
    {
        if(controllerDynamic == null){return;}
        //thumbstick button
        if (SvrInput.Controller.GetButtonDown(SvrController.svrControllerButton.PrimaryThumbstick))
        {
            OnButtonChanged(controllerDynamic, true, "thumbstick", true, CurrentButtonStates);
        }
        if (SvrInput.Controller.GetButtonUp(SvrController.svrControllerButton.PrimaryThumbstick))
        {
            OnButtonChanged(controllerDynamic, true, "thumbstick", false, CurrentButtonStates);
        }

        //trigger
        if (SvrInput.Controller.GetButtonDown(SvrController.svrControllerButton.PrimaryIndexTrigger))
        {
            OnButtonChanged(controllerDynamic, true, "trigger", true, CurrentButtonStates);
        }
        if (SvrInput.Controller.GetButtonUp(SvrController.svrControllerButton.PrimaryIndexTrigger))
        {
            OnButtonChanged(controllerDynamic, true, "trigger", false, CurrentButtonStates);
        }
        if (CurrentButtonStates.Count > 0)
        {
            List<ButtonState> copy = new List<ButtonState>(CurrentButtonStates.Count);
            for(int i = 0; i<CurrentButtonStates.Count;i++)
            {
                copy[i].Copy(CurrentButtonStates[i]);
            }
            CurrentButtonStates.Clear();

            DynamicManager.RecordControllerEvent(controllerDynamic.DataId, copy);
        }
    }
    
    float minMagnitude = 0.05f;
        Vector3 touchpadVector;
    public void RecordAnalogInputs()
    {
        if(controllerDynamic == null){return;}
        var vector = SvrInput.Controller.GetAxis2D(SvrController.svrControllerAxis2D.PrimaryThumbstick);
            var x = vector.x;
            var y = vector.y;
            int force = SvrInput.Controller.GetTouch(SvrController.svrControllerTouch.Any) ? 100 : 0;
            Vector3 currentVector = new Vector3(x, y, force);
            if (Vector3.Magnitude(touchpadVector - currentVector) > minMagnitude)
            {
                OnVectorChanged(controllerDynamic, true, "touchpad", SvrInput.Controller.GetButton(SvrController.svrControllerButton.PrimaryThumbstick) ? 100 : 0, vector, CurrentButtonStates);
                touchpadVector = currentVector;
            }
    }

#elif CVR_STEAMVR2

        List<ButtonState> CurrentButtonStates = new List<ButtonState>();
        
        public DynamicObject dynamic;

        public SteamVR_Input_Sources Hand_InputSource;

        public SteamVR_Action_Boolean gripAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("cvr_input", "grip");
        public SteamVR_Action_Single triggerAction = SteamVR_Input.GetAction<SteamVR_Action_Single>("cvr_input", "trigger");
        public SteamVR_Action_Boolean menuAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("cvr_input", "menu");
        public SteamVR_Action_Vector2 touchpadAction = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("cvr_input", "touchpad");
        public SteamVR_Action_Boolean touchAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("cvr_input", "touchpad_touch");
        public SteamVR_Action_Boolean pressAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("cvr_input", "touchpad_press");
        public SteamVR_ActionSet CVR_ActionSet = SteamVR_Input.GetActionSet("CVR_Input");

        int Trigger;
        int TouchForce;
        Vector2 lastAxis;
        float sqrMag = 0.05f;

        void OnEnable()
        {
            //register actions
            if (gripAction != null)
                gripAction.AddOnChangeListener(OnGripActionChange, Hand_InputSource);
            if (touchAction != null)
                touchAction.AddOnChangeListener(OnTouchActionChange, Hand_InputSource);
            if (pressAction != null)
                pressAction.AddOnChangeListener(OnPressActionChange, Hand_InputSource);
            if (menuAction != null)
                menuAction.AddOnChangeListener(OnMenuActionChange, Hand_InputSource);
            if (CVR_ActionSet != null)
                CVR_ActionSet.Activate(Hand_InputSource);
        }

        private void OnGripActionChange(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
        {
            OnButtonChanged(dynamic, Hand_InputSource == SteamVR_Input_Sources.RightHand, "vive_grip", newState, CurrentButtonStates);
        }

        private void OnTouchActionChange(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
        {
            var buttonstate = CurrentButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "vive_touchpad"; });
            if (buttonstate != null)
            {
                if (newState)
                    TouchForce = 50;
                else if (pressAction.state)
                    TouchForce = 100;
                else
                    TouchForce = 0;
                buttonstate.ButtonPercent = TouchForce;
                lastAxis.x = buttonstate.X;
                lastAxis.y = buttonstate.Y;
            }
            else
            {
                var axis = touchpadAction.GetAxis(Hand_InputSource);
                if (newState)
                    TouchForce = 50;
                else if (pressAction.state)
                    TouchForce = 100;
                else
                    TouchForce = 0;
                OnVectorChanged(dynamic, Hand_InputSource == SteamVR_Input_Sources.RightHand, "vive_touchpad", TouchForce, axis, CurrentButtonStates);
                lastAxis = axis;
            }
        }

        private void OnPressActionChange(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
        {
            var buttonstate = CurrentButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "vive_touchpad"; });
            if (buttonstate != null)
            {
                if (newState)
                    TouchForce = 100;
                else if (touchAction.state)
                    TouchForce = 50;
                else
                    TouchForce = 0;
                buttonstate.ButtonPercent = TouchForce;
                lastAxis.x = buttonstate.X;
                lastAxis.y = buttonstate.Y;
            }
            else
            {
                var axis = touchpadAction.GetAxis(Hand_InputSource);
                if (newState)
                    TouchForce = 100;
                else if (touchAction.state)
                    TouchForce = 50;
                else
                    TouchForce = 0;
                OnVectorChanged(dynamic, Hand_InputSource == SteamVR_Input_Sources.RightHand, "vive_touchpad", TouchForce, axis, CurrentButtonStates);
                lastAxis = axis;
            }
        }

        private void OnMenuActionChange(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
        {
            OnButtonChanged(dynamic, Hand_InputSource == SteamVR_Input_Sources.RightHand, "vive_menubtn", newState, CurrentButtonStates);
        }

        void OnDisable()
        {
            //remove actions
            if (gripAction != null)
                gripAction.RemoveOnChangeListener(OnGripActionChange, Hand_InputSource);
            if (touchAction != null)
                touchAction.RemoveOnChangeListener(OnTouchActionChange, Hand_InputSource);
            if (pressAction != null)
                pressAction.RemoveOnChangeListener(OnPressActionChange, Hand_InputSource);
            if (menuAction != null)
                menuAction.RemoveOnChangeListener(OnMenuActionChange, Hand_InputSource);
            if (CVR_ActionSet != null)
                CVR_ActionSet.Deactivate(Hand_InputSource);
        }

        void Init()
        {
            
        }

        private void LateUpdate()
        {
            //assuming controller updates happen before/in update loop?

            if (Time.time > nextUpdateTime)
            {
                RecordAnalogInputs();
                nextUpdateTime = Time.time + UpdateRate;
            }

            if (CurrentButtonStates.Count > 0)
            {
            	List<ButtonState> copy = new List<ButtonState>(CurrentButtonStates.Count);
            	for(int i = 0; i< CurrentButtonStates.Count;i++)
            	{
            		copy.Add(CurrentButtonStates[i]);
            	}
                CurrentButtonStates.Clear();
            	DynamicManager.RecordControllerEvent(dynamic.DataId, copy);
            }
        }

        void RecordAnalogInputs()
        {
            float trigger = triggerAction.GetAxis(Hand_InputSource);
            int tempTrigger = (int)(trigger * 100);
            if (Trigger != tempTrigger)
            {
                var buttonstate = CurrentButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "vive_touchpad"; });
                if (buttonstate != null)
                {
                    buttonstate.ButtonPercent = tempTrigger;
                }
                else
                {
                    OnSingleChanged(dynamic, Hand_InputSource == SteamVR_Input_Sources.RightHand, "vive_trigger", tempTrigger, CurrentButtonStates);
                }
                Trigger = tempTrigger;
            }

            if (TouchForce != 0)
            {
                var axis = touchpadAction.GetAxis(Hand_InputSource);

                if (Vector3.SqrMagnitude(axis - lastAxis) > sqrMag)
                {
                    var buttonstate = CurrentButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "vive_touchpad"; });
                    if (buttonstate != null)
                    {
                        buttonstate.X = axis.x;
                        buttonstate.Y = axis.y;
                    }
                    else
                    {
                        OnVectorChanged(dynamic, Hand_InputSource == SteamVR_Input_Sources.RightHand, "vive_touchpad", TouchForce, axis, CurrentButtonStates);
                    }

                    lastAxis = axis;
                }
            }
        }
#elif CVR_VIVEWAVE
    
    //this should go on the adaptive controller prefab, the controllers in the scene or whatever the player spawns
    //add all inputs to wave button list

    List<ButtonState> CurrentButtonStates = new List<ButtonState>();
    
    bool isRight;
    DynamicObject dynamic;

    bool initialized;
    WaveVR_Controller.EDeviceType devicetype = WaveVR_Controller.EDeviceType.Head;

    //called from start
    void Init()
    {
        initialized = true;
        dynamic = GetComponent<DynamicObject>();

        devicetype = GetComponent<WaveVR_ControllerPoseTracker>().Type;
        wvr.WVR_DeviceType t = WaveVR.Instance.controllerLeft.type; //left/right
            
        if (WaveVR_Controller.Input(devicetype).DeviceType == t)
        {
            //this is the left controller
            isRight = false;
        }
        else
        {
            isRight = true;
        }
        List<WaveVR_ButtonList.EButtons> _buttons = new List<WaveVR_ButtonList.EButtons>();

        foreach (var v in (WaveVR_ButtonList.EButtons[])System.Enum.GetValues(typeof(WaveVR_ButtonList.EButtons)))
        {
            _buttons.Add(v);
        }

        // button list of Dominant hand.
        WaveVR_ButtonList.Instance.SetupButtonList(devicetype, _buttons);
    }

    //updates for interaction hand implementation
    private void Update()
    {
        if (initialized == false)
        {
            return;
        }

        //menu
        if (WaveVR_Controller.Input(devicetype).GetPressDown(wvr.WVR_InputId.WVR_InputId_Alias1_Menu))
        {
            OnButtonChanged(dynamic, isRight, "menubtn", true, CurrentButtonStates);
        }
        if (WaveVR_Controller.Input(devicetype).GetPressUp(wvr.WVR_InputId.WVR_InputId_Alias1_Menu))
        {
            OnButtonChanged(dynamic, isRight, "menubtn", false, CurrentButtonStates);
        }

        //grip
        if (WaveVR_Controller.Input(devicetype).GetPressDown(wvr.WVR_InputId.WVR_InputId_Alias1_Grip))
        {
            OnButtonChanged(dynamic, isRight, "gripbtn", true, CurrentButtonStates);
        }
        if (WaveVR_Controller.Input(devicetype).GetPressUp(wvr.WVR_InputId.WVR_InputId_Alias1_Grip))
        {
            OnButtonChanged(dynamic, isRight, "gripbtn", false, CurrentButtonStates);
        }
        
        //touchpad touched/pressed
        if (WaveVR_Controller.Input(devicetype).GetPressDown(wvr.WVR_InputId.WVR_InputId_Alias1_Touchpad))
        {
            CurrentTouchpadState = TouchpadState.Press;
            var touchpadaxis = WaveVR_Controller.Input(devicetype).GetAxis(wvr.WVR_InputId.WVR_InputId_Alias1_Touchpad);
            var x = touchpadaxis.x;
            var y = touchpadaxis.y;
            int force = 100;
            Vector3 currentVector = new Vector3(x, y, force);
            OnVectorChanged(dynamic, isRight, "touchpad", force, touchpadaxis, CurrentButtonStates);
            LastTouchpadVector = currentVector;
        }
        else if (WaveVR_Controller.Input(devicetype).GetTouchDown(wvr.WVR_InputId.WVR_InputId_Alias1_Touchpad))
        {
            CurrentTouchpadState = TouchpadState.Touch;
            var touchpadaxis = WaveVR_Controller.Input(devicetype).GetAxis(wvr.WVR_InputId.WVR_InputId_Alias1_Touchpad);
            var x = touchpadaxis.x;
            var y = touchpadaxis.y;
            int force = 50;
            Vector3 currentVector = new Vector3(x, y, force);
            OnVectorChanged(dynamic, isRight, "touchpad", force, touchpadaxis, CurrentButtonStates);
            LastTouchpadVector = currentVector;
        }
        else if (WaveVR_Controller.Input(devicetype).GetPressUp(wvr.WVR_InputId.WVR_InputId_Alias1_Touchpad))
        {
            CurrentTouchpadState = TouchpadState.Touch;
            var touchpadaxis = WaveVR_Controller.Input(devicetype).GetAxis(wvr.WVR_InputId.WVR_InputId_Alias1_Touchpad);
            var x = touchpadaxis.x;
            var y = touchpadaxis.y;

            int force = 0;
            if (WaveVR_Controller.Input(devicetype).GetTouch(wvr.WVR_InputId.WVR_InputId_Alias1_Touchpad))
                force = 50;                
            Vector3 currentVector = new Vector3(x, y, force);
            OnVectorChanged(dynamic, isRight, "touchpad", force, touchpadaxis, CurrentButtonStates);
            LastTouchpadVector = currentVector;
        }
        else if (WaveVR_Controller.Input(devicetype).GetTouchUp(wvr.WVR_InputId.WVR_InputId_Alias1_Touchpad))
        {
            CurrentTouchpadState = TouchpadState.None;
            var touchpadaxis = WaveVR_Controller.Input(devicetype).GetAxis(wvr.WVR_InputId.WVR_InputId_Alias1_Touchpad);
            var x = touchpadaxis.x;
            var y = touchpadaxis.y;
            int force = 0;
            Vector3 currentVector = new Vector3(x, y, force);
            OnVectorChanged(dynamic, isRight, "touchpad", force, touchpadaxis, CurrentButtonStates);
            LastTouchpadVector = currentVector;
        }

        //trigger clicked
        if (WaveVR_Controller.Input(devicetype).GetPressDown(wvr.WVR_InputId.WVR_InputId_Alias1_Trigger))
        {
            if (LastTrigger != 100)
            {
                var triggeramount = WaveVR_Controller.Input(devicetype).GetAxis(wvr.WVR_InputId.WVR_InputId_Alias1_Trigger).x;
                int currentTrigger = (int)(triggeramount * 100);
                LastTrigger = currentTrigger;
                OnButtonChanged(dynamic, isRight, "trigger", true, CurrentButtonStates);
            }
        }
        else if (WaveVR_Controller.Input(devicetype).GetPressUp(wvr.WVR_InputId.WVR_InputId_Alias1_Trigger))
        {
            if (LastTrigger != 0)
            {
                LastTrigger = 0;
                OnButtonChanged(dynamic, isRight, "trigger", false, CurrentButtonStates);
            }
        }

        if (Time.time > nextUpdateTime)
        {
            RecordAnalogInputs(); //should this go at the end? double inputs on triggers
            nextUpdateTime = Time.time + UpdateRate;
        }

        if (CurrentButtonStates.Count > 0)
        {
            List<ButtonState> copy = new List<ButtonState>(CurrentButtonStates.Count);

            for (int i = 0; i < CurrentButtonStates.Count; i++)
            {
                copy.Add(CurrentButtonStates[i]); //move the reference over to the copy
            }
            CurrentButtonStates.Clear();
            DynamicManager.RecordControllerEvent(dynamic.DataId, copy);
        }
    }

    enum TouchpadState
    {
        None,
        Touch,
        Press
    }

    TouchpadState CurrentTouchpadState;
    Vector3 LastTouchpadVector;
    float minMagnitude = 0.05f;
    int LastTrigger;

    //check for (float)triggers, (vector2)touchpads, etc
    public void RecordAnalogInputs()
    {
        if (CurrentTouchpadState != TouchpadState.None)
        {
            //var touchpadaxis = ControllerDevice.GetAxis(Varjo.Valve.VR.EVRButtonId.k_EButton_Axis0);
            var touchpadaxis = WaveVR_Controller.Input(devicetype).GetAxis(wvr.WVR_InputId.WVR_InputId_Alias1_Touchpad);
            var x = touchpadaxis.x;
            var y = touchpadaxis.y;
            int force = CurrentTouchpadState == TouchpadState.None ? 0 : CurrentTouchpadState == TouchpadState.Touch ? 50 : 100;
            Vector3 currentVector = new Vector3(x, y, force);
            if (Vector3.Magnitude(LastTouchpadVector-currentVector)>minMagnitude)
            {
                var touchpadstate = CurrentButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "touchpad"; });
                if (touchpadstate != null)
                {
                    touchpadstate.X = x;
                    touchpadstate.Y = y;
                }
                else
                {
                    OnVectorChanged(dynamic, isRight, "touchpad", force, touchpadaxis, CurrentButtonStates);
                }    
                
                LastTouchpadVector = currentVector;
            }
        }


        var buttonstate = CurrentButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "trigger"; });
        if (buttonstate != null)
        {
            var triggeramount = WaveVR_Controller.Input(devicetype).GetAxis(wvr.WVR_InputId.WVR_InputId_Alias1_Trigger).x;
            int currentTrigger = (int)(triggeramount * 100);
            if (LastTrigger != currentTrigger)
            {
                buttonstate.ButtonPercent = currentTrigger;
                LastTrigger = currentTrigger;
            }
        }
        else
        {
            var triggeramount = WaveVR_Controller.Input(devicetype).GetAxis(wvr.WVR_InputId.WVR_InputId_Alias1_Trigger).x;
            int currentTrigger = (int)(triggeramount * 100);
            if (LastTrigger != currentTrigger)
            {
                OnSingleChanged(dynamic, isRight, "trigger", currentTrigger, CurrentButtonStates);
                LastTrigger = currentTrigger;
            }
        }
    }
#elif CVR_WINDOWSMR

        //one input tracker for both controllers??
        //yes

        public DynamicObject LeftHand;
        public DynamicObject RightHand;

        List<ButtonState> CurrentLeftButtonStates = new List<ButtonState>();
        List<ButtonState> CurrentRightButtonStates = new List<ButtonState>();

        Vector3 LeftJoystickVector;
        Vector3 RightJoystickVector;
        Vector3 LeftTouchpadVector;
        Vector3 RightTouchpadVector;
        float minMagnitude = 0.05f;
        int LeftTrigger;
        int RightTrigger;

        enum TouchpadState
        {
            None,
            Touch,
            Press
        }

        TouchpadState LeftTouchpadState;
        TouchpadState RightTouchpadState;
        TouchpadState LeftJoystickState;
        TouchpadState RightJoystickState;

        void Init()
        {

        }

        private void Update()
        {
            //grip left
            if (Input.GetKeyDown(KeyCode.JoystickButton4))
                OnButtonChanged(LeftHand, false, "wmr_grip", true, CurrentLeftButtonStates);
            if (Input.GetKeyUp(KeyCode.JoystickButton4))
                OnButtonChanged(LeftHand, false, "wmr_grip", false, CurrentLeftButtonStates);

            //grip right
            if (Input.GetKeyDown(KeyCode.JoystickButton5))
                OnButtonChanged(RightHand, true, "wmr_grip", true, CurrentRightButtonStates);
            if (Input.GetKeyUp(KeyCode.JoystickButton5))
                OnButtonChanged(RightHand, true, "wmr_grip", false, CurrentRightButtonStates);

            //menu left
            if (Input.GetKeyDown(KeyCode.JoystickButton6))
                OnButtonChanged(LeftHand, false, "wmr_menubtn", true, CurrentLeftButtonStates);
            if (Input.GetKeyUp(KeyCode.JoystickButton6))
                OnButtonChanged(LeftHand, false, "wmr_menubtn", false, CurrentLeftButtonStates);

            //menu right
            if (Input.GetKeyDown(KeyCode.JoystickButton7))
                OnButtonChanged(RightHand, true, "wmr_menubtn", true, CurrentRightButtonStates);
            if (Input.GetKeyUp(KeyCode.JoystickButton7))
                OnButtonChanged(RightHand, true, "wmr_menubtn", false, CurrentRightButtonStates);

            //left touchpad
            Vector2 leftTouchVector = new Vector2(Input.GetAxis("LeftTouchpadH"), Input.GetAxis("LeftTouchpadV"));
            if (Input.GetKeyDown(KeyCode.JoystickButton18))
            {
                OnVectorChanged(LeftHand, false, "wmr_touchpad", 50, leftTouchVector, CurrentLeftButtonStates);
                LeftTouchpadState = TouchpadState.Touch;
            }
            if (Input.GetKeyDown(KeyCode.JoystickButton16))
            {
                OnVectorChanged(LeftHand, false, "wmr_touchpad", 100, leftTouchVector, CurrentLeftButtonStates);
                LeftTouchpadState = TouchpadState.Press;
            }
            if (Input.GetKeyUp(KeyCode.JoystickButton18))
            {
                OnVectorChanged(LeftHand, false, "wmr_touchpad", 0, leftTouchVector, CurrentLeftButtonStates);
                LeftTouchpadState = TouchpadState.None;
            }
            if (Input.GetKeyUp(KeyCode.JoystickButton16))
            {
                if (Input.GetKey(KeyCode.JoystickButton18))
                {
                    OnVectorChanged(LeftHand, false, "wmr_touchpad", 50, leftTouchVector, CurrentLeftButtonStates);
                    LeftTouchpadState = TouchpadState.Touch;
                }
                else
                {
                    OnVectorChanged(LeftHand, false, "wmr_touchpad", 0, leftTouchVector, CurrentLeftButtonStates);
                    LeftTouchpadState = TouchpadState.None;
                }
            }

            //right touchpad
            Vector2 rightTouchVector = new Vector2(Input.GetAxis("RightTouchpadH"), Input.GetAxis("RightTouchpadV"));
            if (Input.GetKeyDown(KeyCode.JoystickButton19))
            {
                OnVectorChanged(RightHand, true, "wmr_touchpad", 50, rightTouchVector, CurrentRightButtonStates);
                RightTouchpadState = TouchpadState.Touch;
            }
            if (Input.GetKeyDown(KeyCode.JoystickButton17))
            {
                OnVectorChanged(RightHand, true, "wmr_touchpad", 100, rightTouchVector, CurrentRightButtonStates);
                RightTouchpadState = TouchpadState.Press;
            }
            if (Input.GetKeyUp(KeyCode.JoystickButton19))
            {
                OnVectorChanged(RightHand, true, "wmr_touchpad", 0, rightTouchVector, CurrentRightButtonStates);
                RightTouchpadState = TouchpadState.None;
            }
            if (Input.GetKeyUp(KeyCode.JoystickButton17))
            {
                if (Input.GetKey(KeyCode.JoystickButton19))
                {
                    OnVectorChanged(RightHand, true, "wmr_touchpad", 50, rightTouchVector, CurrentRightButtonStates);
                    RightTouchpadState = TouchpadState.Touch;
                }
                else
                {
                    OnVectorChanged(RightHand, true, "wmr_touchpad", 0, rightTouchVector, CurrentRightButtonStates);
                    RightTouchpadState = TouchpadState.None;
                }
            }

            //left joystick
            Vector2 leftJoystickVector = new Vector2(Input.GetAxis("LeftJoystickH"), Input.GetAxis("LeftJoystickV"));
            if (Input.GetKeyDown(KeyCode.JoystickButton8))
            {
                OnVectorChanged(LeftHand, false, "wmr_joystick", 100, leftJoystickVector, CurrentLeftButtonStates);
                LeftJoystickState = TouchpadState.Press;
            }
            if (Input.GetKeyUp(KeyCode.JoystickButton8))
            {
                OnVectorChanged(LeftHand, false, "wmr_joystick", 0, leftJoystickVector, CurrentLeftButtonStates);
                LeftJoystickState = TouchpadState.None;
            }

            //right joystick
            Vector2 rightJoystickVector = new Vector2(Input.GetAxis("RightJoystickH"), Input.GetAxis("RightJoystickV"));
            if (Input.GetKeyDown(KeyCode.JoystickButton9))
            {
                OnVectorChanged(RightHand, true, "wmr_joystick", 100, rightJoystickVector, CurrentRightButtonStates);
                RightJoystickState = TouchpadState.Press;
            }
            if (Input.GetKeyUp(KeyCode.JoystickButton9))
            {
                OnVectorChanged(RightHand, true, "wmr_joystick", 0, rightJoystickVector, CurrentRightButtonStates);
                RightJoystickState = TouchpadState.None;
            }

            //left trigger
            if (Input.GetKeyDown(KeyCode.JoystickButton14))
                OnSingleChanged(LeftHand, false, "wmr_trigger", 100, CurrentLeftButtonStates);
            if (Input.GetKeyUp(KeyCode.JoystickButton14))
            {
                float trigger = Input.GetAxis("LeftTrigger");
                OnSingleChanged(LeftHand, false, "wmr_trigger", (int)(trigger * 100), CurrentLeftButtonStates);
            }

            //right trigger
            if (Input.GetKeyDown(KeyCode.JoystickButton15))
                OnSingleChanged(RightHand, true, "wmr_trigger", 100, CurrentRightButtonStates);
            if (Input.GetKeyUp(KeyCode.JoystickButton15))
            {
                float trigger = Input.GetAxis("RightTrigger");
                OnSingleChanged(RightHand, true, "wmr_trigger", (int)(trigger * 100), CurrentRightButtonStates);
            }

            if (Time.time > nextUpdateTime)
            {
                RecordAnalogInputs();
                nextUpdateTime = Time.time + UpdateRate;
            }
            if (CurrentRightButtonStates.Count > 0)
            {
                List<ButtonState> copy = new List<ButtonState>(CurrentRightButtonStates.Count);
                for (int i = 0; i < CurrentRightButtonStates.Count; i++)
                {
                    copy.Add(CurrentRightButtonStates[i]);
                }
                CurrentRightButtonStates.Clear();

                DynamicManager.RecordControllerEvent(RightHand.DataId, copy);
            }
            if (CurrentLeftButtonStates.Count > 0)
            {
                List<ButtonState> copy = new List<ButtonState>(CurrentLeftButtonStates.Count);
                for (int i = 0; i < CurrentLeftButtonStates.Count; i++)
                {
                    copy.Add(CurrentLeftButtonStates[i]);
                }
                CurrentLeftButtonStates.Clear();

                DynamicManager.RecordControllerEvent(LeftHand.DataId, copy);
            }
        }

        void RecordAnalogInputs()
        {
            //joysticks
            {
                Vector2 leftJoystickVector = new Vector2(Input.GetAxis("LeftJoystickH"), Input.GetAxis("LeftJoystickV"));
                var x = leftJoystickVector.x;
                var y = leftJoystickVector.y;
                int force = LeftJoystickState == TouchpadState.Press ? 100 : 0;

                Vector3 currentVector = new Vector3(x, y, force);
                if (Vector3.Magnitude(LeftJoystickVector - currentVector) > minMagnitude)
                {
                    var joystick = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "wmr_joystick"; });
                    if (joystick != null)
                    {
                        joystick.X = x;
                        joystick.Y = y;
                    }
                    else
                    {
                        OnVectorChanged(LeftHand, false, "wmr_joystick", force, leftJoystickVector, CurrentLeftButtonStates);
                    }
                    LeftJoystickVector = currentVector;
                }
            }

            {
                Vector2 rightJoystickVector = new Vector2(Input.GetAxis("RightJoystickH"), Input.GetAxis("RightJoystickV"));
                var x = rightJoystickVector.x;
                var y = rightJoystickVector.y;
                int force = RightJoystickState == TouchpadState.Press ? 100 : 0;
                Vector3 currentVector = new Vector3(x, y, force);
                if (Vector3.Magnitude(RightJoystickVector - currentVector) > minMagnitude)
                {
                    var joystick = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "wmr_joystick"; });
                    if (joystick != null)
                    {
                        joystick.X = x;
                        joystick.Y = y;
                    }
                    else
                    {
                        OnVectorChanged(RightHand, true, "wmr_joystick", force, rightJoystickVector, CurrentRightButtonStates);
                    }
                    RightJoystickVector = currentVector;
                }
            }

            //touchpad
            {
                Vector2 leftTouchpadVector = new Vector2(Input.GetAxis("LeftTouchpadH"), Input.GetAxis("LeftTouchpadV"));
                var x = leftTouchpadVector.x;
                var y = leftTouchpadVector.y;
                int force = LeftTouchpadState == TouchpadState.Press ? 100 : LeftTouchpadState == TouchpadState.Touch ? 50 : 0;

                Vector3 currentVector = new Vector3(x, y, force);
                if (Vector3.Magnitude(LeftTouchpadVector - currentVector) > minMagnitude)
                {
                    var joystick = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "wmr_touchpad"; });
                    if (joystick != null)
                    {
                        joystick.X = x;
                        joystick.Y = y;
                    }
                    else
                    {
                        OnVectorChanged(LeftHand, false, "wmr_touchpad", force, leftTouchpadVector, CurrentLeftButtonStates);
                    }
                    LeftTouchpadVector = currentVector;
                }
            }
            {
                Vector2 rightTouchpadVector = new Vector2(Input.GetAxis("RightTouchpadH"), Input.GetAxis("RightTouchpadV"));
                var x = rightTouchpadVector.x;
                var y = rightTouchpadVector.y;
                int force = RightTouchpadState == TouchpadState.Press ? 100 : RightTouchpadState == TouchpadState.Touch ? 50 : 0;

                Vector3 currentVector = new Vector3(x, y, force);
                if (Vector3.Magnitude(RightTouchpadVector - currentVector) > minMagnitude)
                {
                    var joystick = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "wmr_touchpad"; });
                    if (joystick != null)
                    {
                        joystick.X = x;
                        joystick.Y = y;
                    }
                    else
                    {
                        OnVectorChanged(RightHand, true, "wmr_touchpad", force, rightTouchpadVector, CurrentRightButtonStates);
                    }
                    RightTouchpadVector = currentVector;
                }
            }

            //triggers
            {
                int currentTrigger = (int)(Input.GetAxis("LeftTrigger") * 100);
                if (LeftTrigger != currentTrigger)
                {
                    var trigger = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "wmr_trigger"; });
                    if (trigger != null)
                    {
                        trigger.ButtonPercent = currentTrigger;
                    }
                    else
                    {
                        OnSingleChanged(LeftHand, false, "wmr_trigger", currentTrigger, CurrentLeftButtonStates);
                    }
                    LeftTrigger = currentTrigger;
                }
            }
            {
                int currentTrigger = (int)(Input.GetAxis("RightTrigger") * 100);
                if (RightTrigger != currentTrigger)
                {
                    var trigger = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "wmr_trigger"; });
                    if (trigger != null)
                    {
                        trigger.ButtonPercent = currentTrigger;
                    }
                    else
                    {
                        OnSingleChanged(RightHand, true, "wmr_trigger", currentTrigger, CurrentRightButtonStates);
                    }
                    RightTrigger = currentTrigger;
                }
            }
        }
#elif CVR_PICONEO2EYE

        //one input tracker for both controllers??
        //yes

        public DynamicObject LeftHand;
        public DynamicObject RightHand;

        List<ButtonState> CurrentLeftButtonStates = new List<ButtonState>();
        List<ButtonState> CurrentRightButtonStates = new List<ButtonState>();

        Vector3 LeftJoystickVector;
        Vector3 RightJoystickVector;
        float minMagnitude = 0.05f;
        int LeftTrigger;
        int RightTrigger;

        enum TouchpadState
        {
            None,
            Touch,
            Press
        }

        TouchpadState LeftJoystickState;
        TouchpadState RightJoystickState;

        void Init()
        {

        }

        private void Update()
        {
            //grip left
            if (Pvr_ControllerManager.controllerlink.Controller0.Left.PressedDown)
                OnButtonChanged(LeftHand, false, "pico_grip", true, CurrentLeftButtonStates);
            if (Pvr_ControllerManager.controllerlink.Controller0.Left.PressedUp)
                OnButtonChanged(LeftHand, false, "pico_grip", false, CurrentLeftButtonStates);

            //grip right
            if (Pvr_ControllerManager.controllerlink.Controller1.Right.PressedDown)
                OnButtonChanged(RightHand, true, "pico_grip", true, CurrentRightButtonStates);
            if (Pvr_ControllerManager.controllerlink.Controller1.Right.PressedUp)
                OnButtonChanged(RightHand, true, "pico_grip", false, CurrentRightButtonStates);

            //x left
            if (Pvr_ControllerManager.controllerlink.Controller0.X.PressedDown)
                OnButtonChanged(LeftHand, false, "pico_xbtn", true, CurrentLeftButtonStates);
            if (Pvr_ControllerManager.controllerlink.Controller0.X.PressedUp)
                OnButtonChanged(LeftHand, false, "pico_xbtn", false, CurrentLeftButtonStates);

            //y left
            if (Pvr_ControllerManager.controllerlink.Controller0.Y.PressedDown)
                OnButtonChanged(LeftHand, false, "pico_ybtn", true, CurrentLeftButtonStates);
            if (Pvr_ControllerManager.controllerlink.Controller0.Y.PressedUp)
                OnButtonChanged(LeftHand, false, "pico_ybtn", false, CurrentLeftButtonStates);

            //menu left
            if (Pvr_ControllerManager.controllerlink.Controller0.App.PressedDown)
                OnButtonChanged(LeftHand, false, "pico_menubtn", true, CurrentLeftButtonStates);
            if (Pvr_ControllerManager.controllerlink.Controller0.App.PressedUp)
                OnButtonChanged(LeftHand, false, "pico_menubtn", false, CurrentLeftButtonStates);

            //a right
            if (Pvr_ControllerManager.controllerlink.Controller1.A.PressedDown)
                OnButtonChanged(RightHand, true, "pico_abtn", true, CurrentRightButtonStates);
            if (Pvr_ControllerManager.controllerlink.Controller1.A.PressedUp)
                OnButtonChanged(RightHand, true, "pico_abtn", false, CurrentRightButtonStates);

            //b right
            if (Pvr_ControllerManager.controllerlink.Controller1.B.PressedDown)
                OnButtonChanged(RightHand, true, "pico_bbtn", true, CurrentRightButtonStates);
            if (Pvr_ControllerManager.controllerlink.Controller1.B.PressedUp)
                OnButtonChanged(RightHand, true, "pico_bbtn", false, CurrentRightButtonStates);

            //menu right
            if (Pvr_ControllerManager.controllerlink.Controller1.App.PressedDown)
                OnButtonChanged(RightHand, true, "pico_menubtn", true, CurrentRightButtonStates);
            if (Pvr_ControllerManager.controllerlink.Controller1.App.PressedUp)
                OnButtonChanged(RightHand, true, "pico_menubtn", false, CurrentRightButtonStates);

            //left joystick

            Vector2 leftJoystickVector = Pvr_UnitySDKAPI.Controller.UPvr_GetAxis2D(0);

            if (Pvr_ControllerManager.controllerlink.Controller0.Touch.PressedDown)
            {
                OnVectorChanged(LeftHand, false, "pico_joystick", 100, leftJoystickVector, CurrentLeftButtonStates);
                LeftJoystickState = TouchpadState.Press;
            }
            if (Pvr_ControllerManager.controllerlink.Controller0.Touch.PressedUp)
            {
                OnVectorChanged(LeftHand, false, "pico_joystick", 0, leftJoystickVector, CurrentLeftButtonStates);
                LeftJoystickState = TouchpadState.None;
            }

            //right joystick
            Vector2 rightJoystickVector = Pvr_UnitySDKAPI.Controller.UPvr_GetAxis2D(1);
            if (Pvr_ControllerManager.controllerlink.Controller1.Touch.PressedDown)
            {
                OnVectorChanged(RightHand, true, "pico_joystick", 100, rightJoystickVector, CurrentRightButtonStates);
                RightJoystickState = TouchpadState.Press;
            }
            if (Pvr_ControllerManager.controllerlink.Controller1.Touch.PressedUp)
            {
                OnVectorChanged(RightHand, true, "pico_joystick", 0, rightJoystickVector, CurrentRightButtonStates);
                RightJoystickState = TouchpadState.None;
            }

            //left trigger
            int leftTrigger = (int)((Pvr_UnitySDKAPI.Controller.UPvr_GetControllerTriggerValue(0) / 255f) * 100f);
            if (Pvr_ControllerManager.controllerlink.Controller0.Trigger.PressedDown)
            {
                LeftTrigger = leftTrigger;
                OnSingleChanged(LeftHand, false, "pico_trigger", leftTrigger, CurrentLeftButtonStates);
            }
            if (Pvr_ControllerManager.controllerlink.Controller0.Trigger.PressedUp)
            {
                LeftTrigger = leftTrigger;
                OnSingleChanged(LeftHand, false, "pico_trigger", leftTrigger, CurrentLeftButtonStates);
            }

            //right trigger
            int rightTrigger = (int)((Pvr_UnitySDKAPI.Controller.UPvr_GetControllerTriggerValue(1) / 255f) * 100f);
            if (Pvr_ControllerManager.controllerlink.Controller1.Trigger.PressedDown)
            {
                RightTrigger = rightTrigger;
                OnSingleChanged(RightHand, true, "pico_trigger", rightTrigger, CurrentRightButtonStates);
            }
            if (Pvr_ControllerManager.controllerlink.Controller1.Trigger.PressedUp)
            {
                RightTrigger = rightTrigger;
                OnSingleChanged(RightHand, true, "pico_trigger", rightTrigger, CurrentRightButtonStates);
            }

            if (Time.time > nextUpdateTime)
            {
                RecordAnalogInputs();
                nextUpdateTime = Time.time + UpdateRate;
            }
            if (CurrentRightButtonStates.Count > 0)
            {
                List<ButtonState> copy = new List<ButtonState>(CurrentRightButtonStates.Count);
                for (int i = 0; i < CurrentRightButtonStates.Count; i++)
                {
                    copy.Add(CurrentRightButtonStates[i]);
                }
                CurrentRightButtonStates.Clear();

                DynamicManager.RecordControllerEvent(RightHand.DataId, copy);
            }
            if (CurrentLeftButtonStates.Count > 0)
            {
                List<ButtonState> copy = new List<ButtonState>(CurrentLeftButtonStates.Count);
                for (int i = 0; i < CurrentLeftButtonStates.Count; i++)
                {
                    copy.Add(CurrentLeftButtonStates[i]);
                }
                CurrentLeftButtonStates.Clear();

                DynamicManager.RecordControllerEvent(LeftHand.DataId, copy);
            }
        }

        void RecordAnalogInputs()
        {
            //joysticks
            {
                Vector2 leftJoystickVector = Pvr_UnitySDKAPI.Controller.UPvr_GetAxis2D(0);
                var x = leftJoystickVector.x;
                var y = leftJoystickVector.y;
                int force = LeftJoystickState == TouchpadState.Press ? 100 : 0;

                Vector3 currentVector = new Vector3(x, y, force);
                if (Vector3.Magnitude(LeftJoystickVector - currentVector) > minMagnitude)
                {
                    var joystick = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "pico_joystick"; });
                    if (joystick != null)
                    {
                        joystick.X = x;
                        joystick.Y = y;
                    }
                    else
                    {
                        OnVectorChanged(LeftHand, false, "pico_joystick", force, leftJoystickVector, CurrentLeftButtonStates);
                    }
                    LeftJoystickVector = currentVector;
                }
            }

            {
                Vector2 rightJoystickVector = Pvr_UnitySDKAPI.Controller.UPvr_GetAxis2D(1);
                var x = rightJoystickVector.x;
                var y = rightJoystickVector.y;
                int force = RightJoystickState == TouchpadState.Press ? 100 : 0;
                Vector3 currentVector = new Vector3(x, y, force);
                if (Vector3.Magnitude(RightJoystickVector - currentVector) > minMagnitude)
                {
                    var joystick = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "pico_joystick"; });
                    if (joystick != null)
                    {
                        joystick.X = x;
                        joystick.Y = y;
                    }
                    else
                    {
                        OnVectorChanged(RightHand, true, "pico_joystick", force, rightJoystickVector, CurrentRightButtonStates);
                    }
                    RightJoystickVector = currentVector;
                }
            }

            //triggers
            {
                int currentTrigger = (int)((Pvr_UnitySDKAPI.Controller.UPvr_GetControllerTriggerValue(0) / 255f) * 100f);
                if (LeftTrigger != currentTrigger)
                {
                    var trigger = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "pico_trigger"; });
                    if (trigger != null)
                    {
                        trigger.ButtonPercent = currentTrigger;
                    }
                    else
                    {
                        OnSingleChanged(LeftHand, false, "pico_trigger", currentTrigger, CurrentLeftButtonStates);
                    }
                    LeftTrigger = currentTrigger;
                }
            }
            {
                int currentTrigger = (int)((Pvr_UnitySDKAPI.Controller.UPvr_GetControllerTriggerValue(1) / 255f) * 100f);
                if (RightTrigger != currentTrigger)
                {
                    var trigger = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "pico_trigger"; });
                    if (trigger != null)
                    {
                        trigger.ButtonPercent = currentTrigger;
                    }
                    else
                    {
                        OnSingleChanged(RightHand, true, "pico_trigger", currentTrigger, CurrentRightButtonStates);
                    }
                    RightTrigger = currentTrigger;
                }
            }
        }
#elif CVR_XR
        public DynamicObject LeftHand;
        public DynamicObject RightHand;

        List<ButtonState> CurrentLeftButtonStates = new List<ButtonState>();
        List<ButtonState> CurrentRightButtonStates = new List<ButtonState>();

        Dictionary<string, ButtonState> LeftLastFrameButtonStates = new Dictionary<string, ButtonState>();
        Dictionary<string, ButtonState> RightLastFrameButtonStates = new Dictionary<string, ButtonState>();

        Vector3 LeftJoystickVector;
        Vector3 RightJoystickVector;
        Vector3 LeftTouchpadVector;
        Vector3 RightTouchpadVector;
        float minMagnitude = 0.05f;

        void Init()
        {
            if (LeftHand.ControllerType == DynamicObject.ControllerDisplayType.oculusquesttouchleft)
            {
                //thumbrest not included in xr inputs?
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxis.name, new ButtonState("joystick",0,0,0,true));
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxisClick.name, new ButtonState("joystick", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxisTouch.name, new ButtonState("joystick", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.trigger.name, new ButtonState("trigger"));
                LeftLastFrameButtonStates.Add(CommonUsages.triggerButton.name, new ButtonState("trigger"));
                LeftLastFrameButtonStates.Add(CommonUsages.grip.name, new ButtonState("grip"));
                LeftLastFrameButtonStates.Add(CommonUsages.gripButton.name, new ButtonState("grip"));
                LeftLastFrameButtonStates.Add(CommonUsages.menuButton.name, new ButtonState("start"));
                LeftLastFrameButtonStates.Add(CommonUsages.primaryButton.name, new ButtonState("xbtn"));
                LeftLastFrameButtonStates.Add(CommonUsages.secondaryButton.name, new ButtonState("ybtn"));
            }
            if (RightHand.ControllerType == DynamicObject.ControllerDisplayType.oculusquesttouchright)
            {
                //thumbrest not included in xr inputs?
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxis.name, new ButtonState("joystick", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxisClick.name, new ButtonState("joystick", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxisTouch.name, new ButtonState("joystick", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.trigger.name, new ButtonState("trigger"));
                RightLastFrameButtonStates.Add(CommonUsages.triggerButton.name, new ButtonState("trigger"));
                RightLastFrameButtonStates.Add(CommonUsages.grip.name, new ButtonState("grip"));
                RightLastFrameButtonStates.Add(CommonUsages.gripButton.name, new ButtonState("grip"));
                RightLastFrameButtonStates.Add(CommonUsages.menuButton.name, new ButtonState("start"));
                RightLastFrameButtonStates.Add(CommonUsages.primaryButton.name, new ButtonState("abtn"));
                RightLastFrameButtonStates.Add(CommonUsages.secondaryButton.name, new ButtonState("bbtn"));
            }
            if (LeftHand.ControllerType == DynamicObject.ControllerDisplayType.oculustouchleft)
            {
                //thumbrest not included in xr inputs?
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxis.name, new ButtonState("rift_joystick", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxisClick.name, new ButtonState("rift_joystick", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxisTouch.name, new ButtonState("rift_joystick", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.trigger.name, new ButtonState("rift_trigger"));
                LeftLastFrameButtonStates.Add(CommonUsages.triggerButton.name, new ButtonState("rift_trigger"));
                LeftLastFrameButtonStates.Add(CommonUsages.grip.name, new ButtonState("rift_grip"));
                LeftLastFrameButtonStates.Add(CommonUsages.gripButton.name, new ButtonState("rift_grip"));
                LeftLastFrameButtonStates.Add(CommonUsages.menuButton.name, new ButtonState("rift_start"));
                LeftLastFrameButtonStates.Add(CommonUsages.primaryButton.name, new ButtonState("rift_xbtn"));
                LeftLastFrameButtonStates.Add(CommonUsages.secondaryButton.name, new ButtonState("rift_ybtn"));
            }
            if (RightHand.ControllerType == DynamicObject.ControllerDisplayType.oculustouchright)
            {
                //thumbrest not included in xr inputs?
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxis.name, new ButtonState("rift_joystick", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxisClick.name, new ButtonState("rift_joystick", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxisTouch.name, new ButtonState("rift_joystick", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.trigger.name, new ButtonState("rift_trigger"));
                RightLastFrameButtonStates.Add(CommonUsages.triggerButton.name, new ButtonState("rift_trigger"));
                RightLastFrameButtonStates.Add(CommonUsages.grip.name, new ButtonState("rift_grip"));
                RightLastFrameButtonStates.Add(CommonUsages.gripButton.name, new ButtonState("rift_grip"));
                RightLastFrameButtonStates.Add(CommonUsages.menuButton.name, new ButtonState("rift_start"));
                RightLastFrameButtonStates.Add(CommonUsages.primaryButton.name, new ButtonState("rift_abtn"));
                RightLastFrameButtonStates.Add(CommonUsages.secondaryButton.name, new ButtonState("rift_bbtn"));
            }
            if (LeftHand.ControllerType == DynamicObject.ControllerDisplayType.vivecontroller)
            {
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxis.name, new ButtonState("vive_touchpad", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxisClick.name, new ButtonState("vive_touchpad", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxisTouch.name, new ButtonState("vive_touchpad", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.trigger.name, new ButtonState("vive_trigger"));
                LeftLastFrameButtonStates.Add(CommonUsages.triggerButton.name, new ButtonState("vive_trigger"));
                LeftLastFrameButtonStates.Add(CommonUsages.grip.name, new ButtonState("vive_grip"));
                LeftLastFrameButtonStates.Add(CommonUsages.gripButton.name, new ButtonState("vive_grip"));
                LeftLastFrameButtonStates.Add(CommonUsages.primaryButton.name, new ButtonState("vive_menubtn"));
            }
            if (RightHand.ControllerType == DynamicObject.ControllerDisplayType.vivecontroller)
            {
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxis.name, new ButtonState("vive_touchpad", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxisClick.name, new ButtonState("vive_touchpad", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxisTouch.name, new ButtonState("vive_touchpad", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.trigger.name, new ButtonState("vive_trigger"));
                RightLastFrameButtonStates.Add(CommonUsages.triggerButton.name, new ButtonState("vive_trigger"));
                RightLastFrameButtonStates.Add(CommonUsages.grip.name, new ButtonState("vive_grip"));
                RightLastFrameButtonStates.Add(CommonUsages.gripButton.name, new ButtonState("vive_grip"));
                RightLastFrameButtonStates.Add(CommonUsages.primaryButton.name, new ButtonState("vive_menubtn"));
            }
            if (LeftHand.ControllerType == DynamicObject.ControllerDisplayType.windows_mixed_reality_controller_left)
            {
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxis.name, new ButtonState("wmr_touchpad", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxisClick.name, new ButtonState("wmr_touchpad", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.primary2DAxisTouch.name, new ButtonState("wmr_touchpad", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.secondary2DAxis.name, new ButtonState("wmr_joystick", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.secondary2DAxisClick.name, new ButtonState("wmr_joystick", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.secondary2DAxisTouch.name, new ButtonState("wmr_joystick", 0, 0, 0, true));
                LeftLastFrameButtonStates.Add(CommonUsages.trigger.name, new ButtonState("wmr_trigger"));
                LeftLastFrameButtonStates.Add(CommonUsages.triggerButton.name, new ButtonState("wmr_trigger"));
                LeftLastFrameButtonStates.Add(CommonUsages.grip.name, new ButtonState("wmr_grip"));
                LeftLastFrameButtonStates.Add(CommonUsages.gripButton.name, new ButtonState("wmr_grip"));
                LeftLastFrameButtonStates.Add(CommonUsages.menuButton.name, new ButtonState("wmr_menubtn"));
            }
            if (RightHand.ControllerType == DynamicObject.ControllerDisplayType.windows_mixed_reality_controller_right)
            {
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxis.name, new ButtonState("wmr_touchpad", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxisClick.name, new ButtonState("wmr_touchpad", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.primary2DAxisTouch.name, new ButtonState("wmr_touchpad", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.secondary2DAxis.name, new ButtonState("wmr_joystick", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.secondary2DAxisClick.name, new ButtonState("wmr_joystick", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.secondary2DAxisTouch.name, new ButtonState("wmr_joystick", 0, 0, 0, true));
                RightLastFrameButtonStates.Add(CommonUsages.trigger.name, new ButtonState("wmr_trigger"));
                RightLastFrameButtonStates.Add(CommonUsages.triggerButton.name, new ButtonState("wmr_trigger"));
                RightLastFrameButtonStates.Add(CommonUsages.grip.name, new ButtonState("wmr_grip"));
                RightLastFrameButtonStates.Add(CommonUsages.gripButton.name, new ButtonState("wmr_grip"));
                RightLastFrameButtonStates.Add(CommonUsages.menuButton.name, new ButtonState("wmr_menubtn"));
            }
        }

        private void Update()
        {
            var leftHandDevices = new List<UnityEngine.XR.InputDevice>();
            var rightHandDevices = new List<UnityEngine.XR.InputDevice>();

            InputDeviceCharacteristics leftTrackedControllerFilter = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left;
            InputDeviceCharacteristics rightTrackedControllerFilter = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right;

            InputDevices.GetDevicesWithCharacteristics(leftTrackedControllerFilter, leftHandDevices);
            InputDevices.GetDevicesWithCharacteristics(rightTrackedControllerFilter, rightHandDevices);
            
            if (leftHandDevices.Count > 0)
            {
                //menu left
                bool menu;
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.menuButton, out menu) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.menuButton.name))
                {
                    if (LeftLastFrameButtonStates[CommonUsages.menuButton.name].ButtonPercent != (menu ? 100 : 0))
                    {
                        OnButtonChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.menuButton.name].ButtonName, menu, CurrentLeftButtonStates);
                        LeftLastFrameButtonStates[CommonUsages.menuButton.name].ButtonPercent = menu ? 100 : 0;
                    }
                }

                //left primary axis
                Vector2 primaryaxis2d;
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out primaryaxis2d) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.primary2DAxis.name))
                {
                    bool touchPressChanged = false;
                    int axisPower = 0;
                    //check for touch or press. if changed, write entire vector
                    bool touch;
                    if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out touch) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.primary2DAxisTouch.name))
                    {
                        if (LeftLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonPercent != (touch ? 50 : 0))
                        {
                            touchPressChanged = true;
                            LeftLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonPercent = touch ? 50 : 0;
                            axisPower = touch ? 50 : 0;
                        }
                    }
                    bool press;
                    if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxisClick, out press) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.primary2DAxisClick.name))
                    {
                        if (LeftLastFrameButtonStates[CommonUsages.primary2DAxisClick.name].ButtonPercent != (press ? 100 : 0))
                        {
                            touchPressChanged = true;
                            LeftLastFrameButtonStates[CommonUsages.primary2DAxisClick.name].ButtonPercent = press ? 100 : 0;
                            axisPower = press ? 100 : 0;
                        }
                    }
                    if (touchPressChanged)
                    {
                        OnVectorChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.primary2DAxisClick.name].ButtonName, axisPower, primaryaxis2d.x, primaryaxis2d.y, CurrentLeftButtonStates);
                    }
                }

                //left secondary axis
                Vector2 secondaryaxis2d;
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.secondary2DAxis, out secondaryaxis2d) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.secondary2DAxis.name))
                {
                    bool touchPressChanged = false;
                    int axisPower = 0;
                    //check for touch or press. if changed, write entire vector
                    bool touch;
                    if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.secondary2DAxisTouch, out touch) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.secondary2DAxisTouch.name))
                    {
                        if (LeftLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonPercent != (touch ? 50 : 0))
                        {
                            touchPressChanged = true;
                            LeftLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonPercent = touch ? 50 : 0;
                            axisPower = touch ? 50 : 0;
                        }
                    }
                    bool press;
                    if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out press) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.secondary2DAxisClick.name))
                    {
                        if (LeftLastFrameButtonStates[CommonUsages.secondary2DAxisClick.name].ButtonPercent != (press ? 100 : 0))
                        {
                            touchPressChanged = true;
                            LeftLastFrameButtonStates[CommonUsages.secondary2DAxisClick.name].ButtonPercent = press ? 100 : 0;
                            axisPower = press ? 100 : 0;
                        }
                    }
                    if (touchPressChanged)
                    {
                        OnVectorChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.secondary2DAxisClick.name].ButtonName, axisPower, secondaryaxis2d.x, secondaryaxis2d.y, CurrentLeftButtonStates);
                    }
                }

                //left trigger as button
                bool triggerButton;
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.triggerButton, out triggerButton) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.triggerButton.name))
                {
                    if (LeftLastFrameButtonStates[CommonUsages.triggerButton.name].ButtonPercent != (triggerButton ? 100 : 0))
                    {
                        OnButtonChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.triggerButton.name].ButtonName, triggerButton, CurrentLeftButtonStates);
                        LeftLastFrameButtonStates[CommonUsages.triggerButton.name].ButtonPercent = triggerButton ? 100 : 0;
                    }
                }



                //left grip as button
                bool gripButton;
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.gripButton, out gripButton) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.gripButton.name))
                {
                    if (LeftLastFrameButtonStates[CommonUsages.gripButton.name].ButtonPercent != (gripButton ? 100 : 0))
                    {
                        OnButtonChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.gripButton.name].ButtonName, gripButton, CurrentLeftButtonStates);
                        LeftLastFrameButtonStates[CommonUsages.gripButton.name].ButtonPercent = gripButton ? 100 : 0;
                    }
                }

                //left primary button
                bool primaryButton;
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.primaryButton.name))
                {
                    if (LeftLastFrameButtonStates[CommonUsages.primaryButton.name].ButtonPercent != (primaryButton ? 100 : 0))
                    {
                        OnButtonChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.primaryButton.name].ButtonName, primaryButton, CurrentLeftButtonStates);
                        LeftLastFrameButtonStates[CommonUsages.primaryButton.name].ButtonPercent = primaryButton ? 100 : 0;
                    }
                }

                //left secondary button
                bool secondaryButton;
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.secondaryButton.name))
                {
                    if (LeftLastFrameButtonStates[CommonUsages.secondaryButton.name].ButtonPercent != (secondaryButton ? 100 : 0))
                    {
                        OnButtonChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.secondaryButton.name].ButtonName, secondaryButton, CurrentLeftButtonStates);
                        LeftLastFrameButtonStates[CommonUsages.secondaryButton.name].ButtonPercent = secondaryButton ? 100 : 0;
                    }
                }
            }

            if (rightHandDevices.Count > 0)
            {
                //menu right
                bool menu;
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.menuButton, out menu) && RightLastFrameButtonStates.ContainsKey(CommonUsages.menuButton.name))
                {
                    if (RightLastFrameButtonStates[CommonUsages.menuButton.name].ButtonPercent != (menu ? 100 : 0))
                    {
                        OnButtonChanged(RightHand, false, RightLastFrameButtonStates[CommonUsages.menuButton.name].ButtonName, menu, CurrentRightButtonStates);
                        RightLastFrameButtonStates[CommonUsages.menuButton.name].ButtonPercent = menu ? 100 : 0;
                    }
                }

                //right primary axis
                Vector2 primaryaxis2d;
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out primaryaxis2d) && RightLastFrameButtonStates.ContainsKey(CommonUsages.primary2DAxis.name))
                {
                    bool touchPressChanged = false;
                    int axisPower = 0;
                    //check for touch or press. if changed, write entire vector
                    bool touch;
                    if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out touch) && RightLastFrameButtonStates.ContainsKey(CommonUsages.primary2DAxisTouch.name))
                    {
                        if (RightLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonPercent != (touch ? 50 : 0))
                        {
                            touchPressChanged = true;
                            RightLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonPercent = touch ? 50 : 0;
                            axisPower = touch ? 50 : 0;
                        }
                    }
                    bool press;
                    if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxisClick, out press) && RightLastFrameButtonStates.ContainsKey(CommonUsages.primary2DAxisClick.name))
                    {
                        if (RightLastFrameButtonStates[CommonUsages.primary2DAxisClick.name].ButtonPercent != (press ? 100 : 0))
                        {
                            touchPressChanged = true;
                            RightLastFrameButtonStates[CommonUsages.primary2DAxisClick.name].ButtonPercent = press ? 100 : 0;
                            axisPower = press ? 100 : 0;
                        }
                    }
                    if (touchPressChanged)
                    {
                        OnVectorChanged(RightHand, false, RightLastFrameButtonStates[CommonUsages.primary2DAxisClick.name].ButtonName, axisPower, primaryaxis2d.x, primaryaxis2d.y, CurrentRightButtonStates);
                    }
                }

                //right secondary axis
                Vector2 secondaryaxis2d;
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.secondary2DAxis, out secondaryaxis2d) && RightLastFrameButtonStates.ContainsKey(CommonUsages.secondary2DAxis.name))
                {
                    bool touchPressChanged = false;
                    int axisPower = 0;
                    //check for touch or press. if changed, write entire vector
                    bool touch;
                    if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.secondary2DAxisTouch, out touch) && RightLastFrameButtonStates.ContainsKey(CommonUsages.secondary2DAxisTouch.name))
                    {
                        if (RightLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonPercent != (touch ? 50 : 0))
                        {
                            touchPressChanged = true;
                            RightLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonPercent = touch ? 50 : 0;
                            axisPower = touch ? 50 : 0;
                        }
                    }
                    bool press;
                    if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out press) && RightLastFrameButtonStates.ContainsKey(CommonUsages.secondary2DAxisClick.name))
                    {
                        if (RightLastFrameButtonStates[CommonUsages.secondary2DAxisClick.name].ButtonPercent != (press ? 100 : 0))
                        {
                            touchPressChanged = true;
                            RightLastFrameButtonStates[CommonUsages.secondary2DAxisClick.name].ButtonPercent = press ? 100 : 0;
                            axisPower = press ? 100 : 0;
                        }
                    }
                    if (touchPressChanged)
                    {
                        OnVectorChanged(RightHand, false, RightLastFrameButtonStates[CommonUsages.secondary2DAxisClick.name].ButtonName, axisPower, secondaryaxis2d.x, secondaryaxis2d.y, CurrentRightButtonStates);
                    }
                }
                //right trigger button
                bool triggerButton;
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.triggerButton, out triggerButton) && RightLastFrameButtonStates.ContainsKey(CommonUsages.triggerButton.name))
                {
                    if (RightLastFrameButtonStates[CommonUsages.triggerButton.name].ButtonPercent != (triggerButton ? 100 : 0))
                    {
                        OnButtonChanged(RightHand, true, RightLastFrameButtonStates[CommonUsages.triggerButton.name].ButtonName, triggerButton, CurrentRightButtonStates);
                        RightLastFrameButtonStates[CommonUsages.triggerButton.name].ButtonPercent = triggerButton ? 100 : 0;
                    }
                }
                //right grip button
                bool gripButton;
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.gripButton, out gripButton) && RightLastFrameButtonStates.ContainsKey(CommonUsages.gripButton.name))
                {
                    if (RightLastFrameButtonStates[CommonUsages.gripButton.name].ButtonPercent != (gripButton ? 100 : 0))
                    {
                        OnButtonChanged(RightHand, true, RightLastFrameButtonStates[CommonUsages.gripButton.name].ButtonName, gripButton, CurrentRightButtonStates);
                        RightLastFrameButtonStates[CommonUsages.gripButton.name].ButtonPercent = gripButton ? 100 : 0;
                    }
                }
                //right primary button
                bool primaryButton;
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton) && RightLastFrameButtonStates.ContainsKey(CommonUsages.primaryButton.name))
                {
                    if (RightLastFrameButtonStates[CommonUsages.primaryButton.name].ButtonPercent != (primaryButton ? 100 : 0))
                    {
                        OnButtonChanged(RightHand, true, RightLastFrameButtonStates[CommonUsages.primaryButton.name].ButtonName, primaryButton, CurrentRightButtonStates);
                        RightLastFrameButtonStates[CommonUsages.primaryButton.name].ButtonPercent = primaryButton ? 100 : 0;
                    }
                }
                //right secondary button
                bool secondaryButton;
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton) && RightLastFrameButtonStates.ContainsKey(CommonUsages.secondaryButton.name))
                {
                    if (RightLastFrameButtonStates[CommonUsages.secondaryButton.name].ButtonPercent != (secondaryButton ? 100 : 0))
                    {
                        OnButtonChanged(RightHand, true, RightLastFrameButtonStates[CommonUsages.secondaryButton.name].ButtonName, secondaryButton, CurrentRightButtonStates);
                        RightLastFrameButtonStates[CommonUsages.secondaryButton.name].ButtonPercent = secondaryButton ? 100 : 0;
                    }
                }
            }

            if (Time.time > nextUpdateTime)
            {
                nextUpdateTime = Time.time + UpdateRate;
                RecordAnalogInputs();
            }
            if (CurrentRightButtonStates.Count > 0)
            {
                List<ButtonState> copy = new List<ButtonState>(CurrentRightButtonStates.Count);
                for (int i = 0; i < CurrentRightButtonStates.Count; i++)
                {
                    copy.Add(CurrentRightButtonStates[i]);
                }
                CurrentRightButtonStates.Clear();

                DynamicManager.RecordControllerEvent(RightHand.DataId, copy);
            }
            if (CurrentLeftButtonStates.Count > 0)
            {
                List<ButtonState> copy = new List<ButtonState>(CurrentLeftButtonStates.Count);
                for (int i = 0; i < CurrentLeftButtonStates.Count; i++)
                {
                    copy.Add(CurrentLeftButtonStates[i]);
                }
                CurrentLeftButtonStates.Clear();

                DynamicManager.RecordControllerEvent(LeftHand.DataId, copy);
            }
        }

        void RecordAnalogInputs()
        {
            var leftHandDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand, leftHandDevices);
            var rightHandDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandDevices);

            if (leftHandDevices.Count > 0)
            {
                //left primary joystick
                Vector2 leftJoystickVector;
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out leftJoystickVector) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.primary2DAxis.name))
                {
                    int axisPower = Mathf.Max(LeftLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonPercent, LeftLastFrameButtonStates[CommonUsages.primary2DAxisClick.name].ButtonPercent);
                    var x = leftJoystickVector.x;
                    var y = leftJoystickVector.y;

                    Vector3 currentVector = new Vector3(x, y, axisPower);
                    if (Vector3.Magnitude(LeftJoystickVector - currentVector) > minMagnitude)
                    {
                        var joystick = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == LeftLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonName; });
                        if (joystick != null)
                        {
                            joystick.X = x;
                            joystick.Y = y;
                        }
                        else
                        {
                            OnVectorChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonName, axisPower, leftJoystickVector, CurrentLeftButtonStates);
                        }
                        OnVectorChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonName, axisPower, x, y, CurrentLeftButtonStates);
                        LeftJoystickVector = currentVector;
                    }
                }
                //left secondary touchpad
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.secondary2DAxis, out leftJoystickVector) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.secondary2DAxis.name))
                {
                    int axisPower = Mathf.Max(LeftLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonPercent, LeftLastFrameButtonStates[CommonUsages.secondary2DAxisClick.name].ButtonPercent);
                    var x = leftJoystickVector.x;
                    var y = leftJoystickVector.y;

                    Vector3 currentVector = new Vector3(x, y, axisPower);
                    if (Vector3.Magnitude(LeftTouchpadVector - currentVector) > minMagnitude)
                    {
                        var joystick = CurrentLeftButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == LeftLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonName; });
                        if (joystick != null)
                        {
                            joystick.X = x;
                            joystick.Y = y;
                        }
                        else
                        {
                            OnVectorChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonName, axisPower, leftJoystickVector, CurrentLeftButtonStates);
                        }
                        OnVectorChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonName, axisPower, x, y, CurrentLeftButtonStates);
                        LeftTouchpadVector = currentVector;
                    }
                }
                //grip left
                float grip;
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.grip, out grip) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.grip.name))
                {
                    if (LeftLastFrameButtonStates[CommonUsages.grip.name].ButtonPercent != (int)(grip * 100))
                    {
                        OnSingleChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.grip.name].ButtonName, (int)(grip * 100), CurrentLeftButtonStates);
                        LeftLastFrameButtonStates[CommonUsages.grip.name].ButtonPercent = (int)(grip * 100);
                    }
                }
                //trigger left
                float trigger;
                if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.trigger, out trigger) && LeftLastFrameButtonStates.ContainsKey(CommonUsages.trigger.name))
                {
                    if (LeftLastFrameButtonStates[CommonUsages.trigger.name].ButtonPercent != (int)(trigger * 100))
                    {
                        OnSingleChanged(LeftHand, false, LeftLastFrameButtonStates[CommonUsages.trigger.name].ButtonName, (int)(trigger * 100), CurrentLeftButtonStates);
                        LeftLastFrameButtonStates[CommonUsages.trigger.name].ButtonPercent = (int)(trigger * 100);
                    }
                }
            }
            if (rightHandDevices.Count > 0)
            {

                //right primary joystick
                Vector2 rightJoystickVector;
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out rightJoystickVector) && RightLastFrameButtonStates.ContainsKey(CommonUsages.primary2DAxis.name))
                {
                    int axisPower = Mathf.Max(RightLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonPercent, RightLastFrameButtonStates[CommonUsages.primary2DAxisClick.name].ButtonPercent);
                    var x = rightJoystickVector.x;
                    var y = rightJoystickVector.y;

                    Vector3 currentVector = new Vector3(x, y, axisPower);
                    if (Vector3.Magnitude(RightJoystickVector - currentVector) > minMagnitude)
                    {
                        var joystick = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == RightLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonName; });
                        if (joystick != null)
                        {
                            joystick.X = x;
                            joystick.Y = y;
                        }
                        else
                        {
                            OnVectorChanged(RightHand, true, RightLastFrameButtonStates[CommonUsages.primary2DAxisTouch.name].ButtonName, axisPower, rightJoystickVector, CurrentRightButtonStates);
                        }
                        RightJoystickVector = currentVector;
                    }
                }
                //right secondary touchpad
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.secondary2DAxis, out rightJoystickVector) && RightLastFrameButtonStates.ContainsKey(CommonUsages.secondary2DAxis.name))
                {
                    int axisPower = Mathf.Max(RightLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonPercent, RightLastFrameButtonStates[CommonUsages.secondary2DAxisClick.name].ButtonPercent);
                    var x = rightJoystickVector.x;
                    var y = rightJoystickVector.y;

                    Vector3 currentVector = new Vector3(x, y, axisPower);
                    if (Vector3.Magnitude(RightTouchpadVector - currentVector) > minMagnitude)
                    {
                        var joystick = CurrentRightButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == RightLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonName; });
                        if (joystick != null)
                        {
                            joystick.X = x;
                            joystick.Y = y;
                        }
                        else
                        {
                            OnVectorChanged(RightHand, true, RightLastFrameButtonStates[CommonUsages.secondary2DAxisTouch.name].ButtonName, axisPower, rightJoystickVector, CurrentRightButtonStates);
                        }
                        RightTouchpadVector = currentVector;
                    }
                }


                //grip right
                float grip;
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.grip, out grip) && RightLastFrameButtonStates.ContainsKey(CommonUsages.primary2DAxis.name))
                {
                    if (RightLastFrameButtonStates[CommonUsages.grip.name].ButtonPercent != (int)(grip * 100))
                    {
                        OnSingleChanged(RightHand, false, RightLastFrameButtonStates[CommonUsages.grip.name].ButtonName, (int)(grip * 100), CurrentRightButtonStates);
                        RightLastFrameButtonStates[CommonUsages.grip.name].ButtonPercent = (int)(grip * 100);
                    }
                }

                //trigger right
                float trigger;
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.trigger, out trigger) && RightLastFrameButtonStates.ContainsKey(CommonUsages.trigger.name))
                {
                    if (RightLastFrameButtonStates[CommonUsages.trigger.name].ButtonPercent != (int)(trigger * 100))
                    {
                        OnSingleChanged(RightHand, false, RightLastFrameButtonStates[CommonUsages.trigger.name].ButtonName, (int)(trigger * 100), CurrentRightButtonStates);
                        RightLastFrameButtonStates[CommonUsages.trigger.name].ButtonPercent = (int)(trigger * 100);
                    }
                }
            }
        }
#else //NO SDKS that deal with input
        void Init()
    {
    }

    void RecordAnalogInputs()
    {
    }
#endif

    void OnButtonChanged(DynamicObject dynamic, bool right, string name, bool down, List<ButtonState> states)
    {
        states.Add(new ButtonState(name, down ? 100 : 0));
    }

    //writes for 0-100 inputs (triggers)
    void OnSingleChanged(DynamicObject dynamic, bool right, string name, int single,List<ButtonState> states)
    {
        states.Add(new ButtonState(name, single));
    }

    void OnVectorChanged(DynamicObject dynamic, bool right, string name, int input, float x, float y,List<ButtonState> states)
    {
        states.Add(new ButtonState(name, input, x, y, true));
    }

    //writes for normalized inputs (touchpads)
    void OnVectorChanged(DynamicObject dynamic, bool right, string name, int input, Vector2 vector, List<ButtonState> states)
    {
        states.Add(new ButtonState(name, input, vector.x, vector.y, true));
    }
}
}