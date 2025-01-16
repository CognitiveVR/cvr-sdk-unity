using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    //temporary object for holding overrides and settings for exitpoll question set
    [System.Serializable]
    public class ExitPollParameters
    {
        public string Hook;

        //hides irrelevent options in inspector
        public ExitPollManager.SpawnType ExitpollSpawnType = ExitPollManager.SpawnType.PlayerRelativeSpace;

        //parenting pointer
        public Transform PointerParentOverride;

        //spawning or setting pointer
        public ExitPollManager.PointerType PointerType;
        public GameObject PointerOverride;
        public float PointerLineWidth = 0.01f;
        public bool UseDefaultGradient = true;
        public Gradient PointerGradient;

        public GameObject BoolPanelOverride;
        public GameObject HappyPanelOverride;
        public GameObject ThumbsPanelOverride;
        public GameObject MultiplePanelOverride;
        public GameObject ScalePanelOverride;
        public GameObject VoicePanelOverride;

        public UnityEngine.Events.UnityEvent OnBegin; //called when the exitpoll starts and a valid question set is displayed
        public UnityEngine.Events.UnityEvent OnComplete; //called when all the questions are answered or skipped
        public UnityEngine.Events.UnityEvent OnClose; //called when the panel closes, regardless of reason


        //from exitpoll set
        //these should be called whenever oncomplete or onend are called
        public System.Action<bool> EndAction;

        public bool RotateToStayOnScreen = true;
        public bool UseOverridePosition = false;
        public Vector3 OverridePosition;
        public bool UseOverrideRotation = false;
        public Quaternion OverrideRotation = Quaternion.identity;
        public bool UseAttachTransform = false;
        public Transform AttachTransform;

        public bool StickWindow;

        public LayerMask PanelLayerMask;// = LayerMask.GetMask("Default", "World", "Ground");
        //the prefered distance to display an exit 
        public float DisplayDistance = 3;
        public bool LockYPosition = true;

        //the minimum distance to display an exit poll. below this value will cancel the exit poll and continue with gameplay
        public float MinimumDisplayDistance = 0.2f;
    }
}