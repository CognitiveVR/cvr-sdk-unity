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

public class ButtonState
{
    public int ButtonPercent = 0;
    public float X = 0;
    public float Y = 0;
    public bool IncludeXY = false;

    public ButtonState(int buttonPercent, float x = 0, float y = 0, bool includexy = false)
    {
        ButtonPercent = buttonPercent;
        X = x;
        Y = y;
        IncludeXY = includexy;
    }

    public ButtonState(ButtonState source)
    {
        ButtonPercent = source.ButtonPercent;
        IncludeXY = source.IncludeXY;
        X = source.X;
        Y = source.Y;
    }

    //compare as if simply a container for data
    public override bool Equals(object obj)
    {
        var s = (ButtonState)obj;

        if (!IncludeXY)
        {
            return s.ButtonPercent == ButtonPercent;
        }
        else
        {
            return s.ButtonPercent == ButtonPercent && Mathf.Approximately(s.X, X) && Mathf.Approximately(s.Y, Y);
        }
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public void Copy(ButtonState source)
    {
        ButtonPercent = source.ButtonPercent;
        IncludeXY = source.IncludeXY;
        X = source.X;
        Y = source.Y;
    }
}

#if CVR_STEAMVR
[RequireComponent(typeof(DynamicObject))]
#endif
public class ControllerInputTracker : MonoBehaviour
{
    bool isRight;

    public bool RecordInputsAsEvents = false;
    public bool RecordInputsAsDynamicSnapshots = true;
    public float UpdateRate = 0.5f;
    float nextUpdateTime;
    //records analogue inputs at this interval

    void Start ()
    {
        wait = new WaitForSeconds(UpdateRate);
        Init();
    }

#if CVR_STEAMVR

    DynamicObject dynamic;

    SteamVR_Controller.Device ControllerDevice;
    static SteamVR_ControllerManager controllerManager;
    bool interactionSystemImplementation;
    Valve.VR.InteractionSystem.Hand hand;

    void Init()
    {
        dynamic = GetComponent<DynamicObject>();
        SteamVR_TrackedObject o = GetComponent<SteamVR_TrackedObject>();
        if (o != null)
        {
            if (controllerManager == null)
            {
                controllerManager = FindObjectOfType<SteamVR_ControllerManager>();
            }
            if (controllerManager.left == gameObject)
                isRight = false;
            else
                isRight = true;

            ControllerDevice = SteamVR_Controller.Input((int)o.index);
            StartCoroutine(UpdateTick());
        }
    }

    public void OnHandInitialized()
    {
        Debug.Log("contrller tracker hand OnInitialized");

        hand = GetComponent<Valve.VR.InteractionSystem.Hand>();
        if (hand)
        {
            ControllerDevice = hand.controller;
            interactionSystemImplementation = true;
        }
    }

    //updates for interaction hand implementation
    private void Update()
    {
        if (Time.time > nextUpdateTime)
        {
            RecordAnalogInputs(); //should this go at the end? double inputs on triggers
            nextUpdateTime = Time.time + UpdateRate;
        }

        if (ControllerDevice == null)
        {
            return;
        }
        if (interactionSystemImplementation)
        {
            isRight = hand.GuessCurrentHandType() == Valve.VR.InteractionSystem.Hand.HandType.Right;
        }

        //menu
        if (ControllerDevice.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
            OnButtonChanged(dynamic, isRight, "vive_menubtn", true);
        if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_ApplicationMenu))
            OnButtonChanged(dynamic, isRight, "vive_menubtn", false);

        //home ?? doesn't record event correctly
        //if (ControllerDevice.GetPressDown(EVRButtonId.k_EButton_Dashboard_Back))
        //    OnButtonChanged(dynamic, isRight, "vive_homebtn", true);
        //if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_Dashboard_Back))
        //    OnButtonChanged(dynamic, isRight, "vive_homebtn", false);

        //grip
        if (ControllerDevice.GetPressDown(EVRButtonId.k_EButton_Grip))
            OnButtonChanged(dynamic, isRight, "vive_grip", true);
        if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_Grip))
            OnButtonChanged(dynamic, isRight, "vive_grip", false);

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
                OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis);
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
                OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis);
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
                OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis);
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
                OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis);
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
                OnButtonChanged(dynamic, isRight, "vive_trigger", true);
            }
        }
        if (ControllerDevice.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
        {
            if (LastTrigger != 0)
            {
                OnButtonChanged(dynamic, isRight, "vive_trigger", false);
                LastTrigger = 0;
            }
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
        if (ControllerDevice == null) { return; }

        if (interactionSystemImplementation)
        {
            isRight = hand.GuessCurrentHandType() == Valve.VR.InteractionSystem.Hand.HandType.Right;
        }

        var touchpadaxis = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
        var x = touchpadaxis.x;
        var y = touchpadaxis.y;
        int force = CurrentTouchpadState == TouchpadState.None ? 0 : CurrentTouchpadState == TouchpadState.Touch ? 50 : 100;
        Vector3 currentVector = new Vector3(x, y, force);
        if (Vector3.Magnitude(LastTouchpadVector-currentVector)>minMagnitude)
        {
            OnVectorChanged(dynamic, isRight, "vive_touchpad", force, touchpadaxis);
            LastTouchpadVector = currentVector;
        }

        var triggeramount = ControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
        int currentTrigger = (int)(triggeramount * 100);
        if (LastTrigger != currentTrigger)
        {
            OnSingleChanged(dynamic, isRight, "vive_trigger", currentTrigger);
            LastTrigger = currentTrigger;
        }
    }

#elif CVR_OCULUS

    public DynamicObject LeftHand;
    public DynamicObject RightHand;

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
        if (OVRInput.GetDown(OVRInput.Button.One,OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_abtn", true);
        if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_abtn", false);

        //right hand b
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_bbtn", true);
        if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_bbtn", false);

        //left hand X
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_xbtn", true);
        if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_xbtn", false);

        //left hand y
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_ybtn", true);
        if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_ybtn", false);

        //left thumbrest
        if (OVRInput.GetDown(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_thumbrest", true);
        if (OVRInput.GetUp(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_thumbrest", false);

        //right thumbrest
        if (OVRInput.GetDown(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_thumbrest", true);
        if (OVRInput.GetUp(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_thumbrest", false);

        //start
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
            OnButtonChanged(RightHand, true, "rift_start", true);
        if (OVRInput.GetUp(OVRInput.Button.Start, OVRInput.Controller.LTouch))
            OnButtonChanged(RightHand, true, "rift_start", false);

        //trigger buttons
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_trigger", true);
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_trigger", true);
        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_trigger", false);
        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_trigger", false);

        //grip
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_grip", true);
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_grip", true);
        if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_grip", false);
        if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_grip", false);

        //thumbstick buttons
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_joystick", true);
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_joystick", true);
        if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
            OnButtonChanged(LeftHand, false, "rift_joystick", false);
        if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
            OnButtonChanged(RightHand, true, "rift_joystick", false);
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
                OnVectorChanged(LeftHand, false, "rift_joystick", force, touchpadaxis);
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
                OnVectorChanged(RightHand, true, "rift_joystick", force, touchpadaxis);
                RightHandVector = currentVector;
            }
        }

        //triggers
        int currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch) * 100);
        if (LeftTrigger != currentTrigger)
        {
            OnSingleChanged(LeftHand, false, "rift_trigger", currentTrigger);
            LeftTrigger = currentTrigger;
        }
        currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) * 100);
        if (RightTrigger != currentTrigger)
        {
            OnSingleChanged(RightHand, true, "rift_trigger", currentTrigger);
            RightTrigger = currentTrigger;
        }

        //grips
        currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch) * 100);
        if (LeftGrip != currentTrigger)
        {
            OnSingleChanged(LeftHand, false, "rift_grip", currentTrigger);
            LeftGrip = currentTrigger;
        }
        currentTrigger = (int)(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch) * 100);
        if (RightGrip != currentTrigger)
        {
            OnSingleChanged(RightHand, true, "rift_grip", currentTrigger);
            RightGrip = currentTrigger;
        }
    }

#elif CVR_MAGICLEAP

    private ControllerConnectionHandler _controllerConnectionHandler;
    void Init()
    {
        //hands
        MLHands.Start();

        //controller
        MLInput.OnControllerButtonUp += HandleOnButtonUp;
        MLInput.OnControllerButtonDown += HandleOnButtonDown;
        _controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();
    }

    private void HandleOnButtonDown(byte controllerId, MLInputControllerButton button)
    {
        if (_controllerConnectionHandler.IsControllerValid() && _controllerConnectionHandler.ConnectedController.Id == controllerId &&
            button == MLInputControllerButton.Bumper)
        {
            OnButtonChanged(true,"bumper",true);
        }
    }

    private void HandleOnButtonUp(byte controllerId, MLInputControllerButton button)
    {
        if (_controllerConnectionHandler.IsControllerValid() && _controllerConnectionHandler.ConnectedController.Id == controllerId &&
            button == MLInputControllerButton.Bumper)
        {
            OnButtonChanged(true,"bumper",false);
        }
    }

    void RecordAnalogInputs()
    {
        //controller
        MLInputController controller = _controllerConnectionHandler.ConnectedController;
        OnSingleChanged(true,"trigger",controller.TriggerValue);
        OnVectorChanged(true,"touchpad",(int)controller.Touch1PosAndForce.z, controller.Touch1PosAndForce.x, controller.Touch1PosAndForce.y);

        //hands
        //confidence over 60%, then greater than 40%
    }

    void OnDestroy()
    {
        MLInput.OnControllerButtonDown -= HandleOnButtonDown;
        MLInput.OnControllerButtonUp -= HandleOnButtonUp;
    }
#elif CVR_SNAPDRAGON

    void Init()
    {
        //only supports a single controller for now!
    }

    private void Update()
    {
        //thumbstick button
        if (SvrInput.Controller.GetButtonDown(SvrController.svrControllerButton.PrimaryThumbstick))
            OnButtonChanged(true, "thumbstick", true);
        if (SvrInput.Controller.GetButtonUp(SvrController.svrControllerButton.PrimaryThumbstick))
            OnButtonChanged(true, "thumbstick", false);

        //trigger
        if (SvrInput.Controller.GetButtonDown(SvrController.svrControllerButton.PrimaryIndexTrigger))
            OnButtonChanged(true, "trigger", true);
        if (SvrInput.Controller.GetButtonUp(SvrController.svrControllerButton.PrimaryIndexTrigger))
            OnButtonChanged(true, "trigger", false);
    }

    public void RecordAnalogInputs()
    {
        var vector = SvrInput.Controller.GetAxis2D(SvrController.svrControllerAxis2D.PrimaryThumbstick);
        OnVectorChanged(true, "touchpad", SvrInput.Controller.GetButton(SvrController.svrControllerButton.PrimaryThumbstick) ? 100 : 0, vector);
    }

#elif CVR_STEAMVR2

    void Init()
    {
        foreach (var v in (Valve.VR.SteamVR_Action_In[])Valve.VR.SteamVR_Input_References.instance.actionObjects)
        {
            if (v.GetType() == typeof(Valve.VR.SteamVR_Action_Boolean))
            {
                v.AddOnChangeListener(OnActionInputChangeBoolLeft, Valve.VR.SteamVR_Input_Sources.LeftHand);
                v.AddOnChangeListener(OnActionInputChangeBoolRight, Valve.VR.SteamVR_Input_Sources.RightHand);
            }
            if (v.GetType() == typeof(Valve.VR.SteamVR_Action_Single))
            {
                v.AddOnChangeListener(OnActionInputChangeSingleLeft, Valve.VR.SteamVR_Input_Sources.LeftHand);
                v.AddOnChangeListener(OnActionInputChangeSingleRight, Valve.VR.SteamVR_Input_Sources.RightHand);
            }
            if (v.GetType() == typeof(Valve.VR.SteamVR_Action_Vector2))
            {
                v.AddOnChangeListener(OnActionInputChangeVector2Left, Valve.VR.SteamVR_Input_Sources.LeftHand);
                v.AddOnChangeListener(OnActionInputChangeVector2Right, Valve.VR.SteamVR_Input_Sources.RightHand);
            }
        }
    }

    void OnActionInputChangeBoolRight(Valve.VR.SteamVR_Action_In actionIn)
    {

    }
    void OnActionInputChangeBoolLeft(Valve.VR.SteamVR_Action_In actionIn)
    {
        
    }

    void OnActionInputChangeSingleLeft(Valve.VR.SteamVR_Action_In actionIn)
    {

    }
    void OnActionInputChangeSingleRight(Valve.VR.SteamVR_Action_In actionIn)
    {

    }
    void OnActionInputChangeVector2Left(Valve.VR.SteamVR_Action_In actionIn)
    {

    }
    void OnActionInputChangeVector2Right(Valve.VR.SteamVR_Action_In actionIn)
    {

    }
    void RecordAnalogInputs(){}
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

    void OnButtonChanged(DynamicObject dynamic, bool right, string name, bool down)
    {
        var v = new Dictionary<string, ButtonState>();
        v.Add(name, new ButtonState(down ? 100 : 0));
        var snap = dynamic.NewSnapshot().UpdateTransform();
        snap.Buttons = v;
    }

    //writes for 0-100 inputs (triggers)
    void OnSingleChanged(DynamicObject dynamic, bool right, string name, int single)
    {
        var v = new Dictionary<string, ButtonState>();
        v.Add(name, new ButtonState(single));
        var snap = dynamic.NewSnapshot().UpdateTransform();
        snap.Buttons = v;
    }

    void OnVectorChanged(DynamicObject dynamic, bool right, string name, int input, float x, float y)
    {
        var v = new Dictionary<string, ButtonState>();
        v.Add(name, new ButtonState(input, x, y, true));
        var snap = dynamic.NewSnapshot().UpdateTransform();
        snap.Buttons = v;
    }

    //writes for normalized inputs (touchpads)
    void OnVectorChanged(DynamicObject dynamic, bool right, string name, int input, Vector2 vector)
    {
        var v = new Dictionary<string, ButtonState>();
        v.Add(name, new ButtonState(input,vector.x,vector.y,true));
        var snap = dynamic.NewSnapshot().UpdateTransform();
        snap.Buttons = v;
    }
}
