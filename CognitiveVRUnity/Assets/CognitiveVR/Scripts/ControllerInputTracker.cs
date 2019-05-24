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

//adds controller input properties to dynamic object snapshot for display on sceneexplorer

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
        wait = new WaitForSeconds(UpdateRate);
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
        SteamVR_TrackedObject o = GetComponent<SteamVR_TrackedObject>();
        if (o != null)
        {
            ControllerDevice = SteamVR_Controller.Input((int)o.index);
            StartCoroutine(UpdateTick());
        }
    }

    //updates for interaction hand implementation
    private void Update()
    {
        if (ControllerDevice == null)
        {
            return;
        }
        if (Time.time > nextUpdateTime)
        {
            RecordAnalogInputs(); //should this go at the end? double inputs on triggers
            nextUpdateTime = Time.time + UpdateRate;
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
            if (ControllerDevice.GetTouchDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
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
            if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_SteamVR_Touchpad))
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
            if (ControllerDevice.GetTouchUp(EVRButtonId.k_EButton_SteamVR_Touchpad))
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
                var buttonstate = CurrentButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "vive_trigger"; });
                if (buttonstate != null)
                {
                    buttonstate.ButtonPercent = currentTrigger;
                }
                else
                {
                    OnButtonChanged(dynamic, isRight, "vive_trigger", true, CurrentButtonStates);
                }
            }
        }
        if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
        {
            if (LastTrigger != 0)
            {
                LastTrigger = 0;
                var buttonstate = CurrentButtonStates.Find(delegate (ButtonState obj) { return obj.ButtonName == "vive_trigger"; });
                if (buttonstate != null)
                {
                    buttonstate.ButtonPercent = 0;
                }
                else
                {
                    OnButtonChanged(dynamic, isRight, "vive_trigger", false, CurrentButtonStates);
                }
            }
        }

        if (CurrentButtonStates.Count > 0)
        {
            List<ButtonState> copy = new List<ButtonState>(CurrentButtonStates.Count);

            for (int i = 0; i < CurrentButtonStates.Count; i++)
            {
                copy.Add(CurrentButtonStates[i]); //move the reference over to the copy
            }
            CurrentButtonStates.Clear();
            DynamicManager.RecordControllerEvent(dynamic.Data, copy);
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
        var touchpadaxis = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
        var x = touchpadaxis.x;
        var y = touchpadaxis.y;
        int force = CurrentTouchpadState == TouchpadState.None ? 0 : CurrentTouchpadState == TouchpadState.Touch ? 50 : 100;
        Vector3 currentVector = new Vector3(x, y, force);
        if (Vector3.Magnitude(LastTouchpadVector-currentVector)>minMagnitude)
        {
            OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis, CurrentButtonStates);
            LastTouchpadVector = currentVector;
        }

        var triggeramount = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
        int currentTrigger = (int)(triggeramount * 100);
        if (LastTrigger != currentTrigger)
        {
            OnSingleChanged(dynamic, isRight, "vive_trigger", currentTrigger, CurrentButtonStates);
            LastTrigger = currentTrigger;
        }
    }

#elif CVR_OCULUS

        public DynamicObject LeftHand;
    public DynamicObject RightHand;

        List<ButtonState> CurrentLeftButtonStates = new List<ButtonState>();
        List<ButtonState> CurrentRightButtonStates = new List<ButtonState>();

    void Init()
    {
        //TODO singleton for oculus controllers, since they act as 1 combined controller
        var avatar = GetComponent<OVRCameraRig>();
        LeftHand = avatar.leftHandAnchor.GetComponent<DynamicObject>();
        RightHand = avatar.rightHandAnchor.GetComponent<DynamicObject>();
    }

    //have to do polling every frame to capture inputs
    private void Update()
    {
        if (Time.time > nextUpdateTime)
        {
            RecordAnalogInputs();
            nextUpdateTime = Time.time + UpdateRate;
        }

            //right hand a
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_abtn", true, CurrentRightButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_abtn", false, CurrentRightButtonStates);
            }

            //right hand b
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_bbtn", true, CurrentRightButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_bbtn", false, CurrentRightButtonStates);
            }

            //left hand X
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_xbtn", true, CurrentLeftButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_xbtn", false, CurrentLeftButtonStates);
            }

            //left hand y
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_ybtn", true, CurrentLeftButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_ybtn", false, CurrentLeftButtonStates);
            }

            //left thumbrest
            if (OVRInput.GetDown(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_thumbrest", true, CurrentLeftButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_thumbrest", false, CurrentLeftButtonStates);
            }

            //right thumbrest
            if (OVRInput.GetDown(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_thumbrest", true, CurrentRightButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_thumbrest", false, CurrentRightButtonStates);
            }

            //start
            if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(RightHand, true, "rift_start", true, CurrentLeftButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.Start, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(RightHand, true, "rift_start", false, CurrentLeftButtonStates);
            }

            //trigger buttons
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_trigger", true, CurrentLeftButtonStates);
            }
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_trigger", true, CurrentRightButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_trigger", false, CurrentLeftButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_trigger", false, CurrentRightButtonStates);
            }

            //grip
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_grip", true, CurrentLeftButtonStates);
            }
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_grip", true, CurrentRightButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_grip", false, CurrentLeftButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_grip", false, CurrentRightButtonStates);
            }


            //thumbstick buttons
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_joystick", true, CurrentLeftButtonStates);
            }
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_joystick", true, CurrentRightButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
            {
                OnButtonChanged(LeftHand, false, "rift_joystick", false, CurrentLeftButtonStates);
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
            {
                OnButtonChanged(RightHand, true, "rift_joystick", false, CurrentRightButtonStates);
            }
        if (CurrentRightButtonStates.Count > 0)
        {
            List<ButtonState> copy = new List<ButtonState>(CurrentRightButtonStates.Count);
            for(int i = 0; i< CurrentRightButtonStates.Count;i++)
            {
                copy.Add(CurrentRightButtonStates[i]);
            }
            CurrentRightButtonStates.Clear();

            DynamicManager.RecordControllerEvent(RightHand.Data, copy);
        }
        if (CurrentLeftButtonStates.Count > 0)
        {
            List<ButtonState> copy = new List<ButtonState>(CurrentLeftButtonStates.Count);
            for(int i = 0; i< CurrentLeftButtonStates.Count;i++)
            {
                copy.Add(CurrentLeftButtonStates[i]);
            }
            CurrentLeftButtonStates.Clear();

            DynamicManager.RecordControllerEvent(LeftHand.Data, copy);
        }
    }

    Vector3 LeftHandVector;
    Vector3 RightHandVector;
    float minMagnitude = 0.05f;
    int LeftTrigger;
    int RightTrigger;

    int LeftGrip;
    int RightGrip;

    //polling
    //check for (float)triggers, (vector2)touchpads, etc
    public void RecordAnalogInputs()
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
                OnVectorChanged(LeftHand, false, "rift_joystick", force, touchpadaxis, CurrentLeftButtonStates);
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
                OnVectorChanged(RightHand, true, "rift_joystick", force, touchpadaxis, CurrentRightButtonStates);
                RightHandVector = currentVector;
            }
        }

        //triggers
        int currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch) * 100);
        if (LeftTrigger != currentTrigger)
        {
            OnSingleChanged(LeftHand, false, "rift_trigger", currentTrigger, CurrentLeftButtonStates);
            LeftTrigger = currentTrigger;
        }
        currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) * 100);
        if (RightTrigger != currentTrigger)
        {
            OnSingleChanged(RightHand, true, "rift_trigger", currentTrigger, CurrentRightButtonStates);
            RightTrigger = currentTrigger;
        }

        //grips
        currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch) * 100);
        if (LeftGrip != currentTrigger)
        {
            OnSingleChanged(LeftHand, false, "rift_grip", currentTrigger, CurrentLeftButtonStates);
            LeftGrip = currentTrigger;
        }
        currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch) * 100);
        if (RightGrip != currentTrigger)
        {
            OnSingleChanged(RightHand, true, "rift_grip", currentTrigger, CurrentRightButtonStates);
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

            DynamicManager.RecordControllerEvent(controllerDynamic.Data, copy);
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
        public SteamVR_ActionSet CVR_ActionSet = SteamVR_Input.GetActionSet("cvr_input");

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
        }

        void Init()
        {
            CVR_ActionSet.Activate(Hand_InputSource,1000,false);
        }

        private void Update()
        {
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
            	DynamicManager.RecordControllerEvent(dynamic.Data, copy);
            }
        }

        void RecordAnalogInputs()
        {
            float trigger = triggerAction.GetAxis(Hand_InputSource);
            int tempTrigger = (int)(trigger * 100);
            if (Trigger != tempTrigger)
            {
                Trigger = tempTrigger;
                OnSingleChanged(dynamic, Hand_InputSource == SteamVR_Input_Sources.RightHand, "vive_trigger", Trigger, CurrentButtonStates);
            }

            if (TouchForce != 0)
            {
                var axis = touchpadAction.GetAxis(Hand_InputSource);

                if (Vector3.SqrMagnitude(axis - lastAxis) > sqrMag)
                {
                    lastAxis = axis;
                    OnVectorChanged(dynamic, Hand_InputSource == SteamVR_Input_Sources.RightHand, "vive_touchpad", TouchForce, axis, CurrentButtonStates);
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

        YieldInstruction wait;
    IEnumerator UpdateTick()
    {
        while (true)
        {
            yield return wait;
            RecordAnalogInputs();
        }
    }

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