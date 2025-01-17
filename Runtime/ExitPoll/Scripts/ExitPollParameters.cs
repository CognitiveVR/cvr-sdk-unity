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

        // Tracking Space
        public ExitPollManager.SpawnType ExitpollSpawnType = ExitPollManager.SpawnType.PlayerRelativeSpace;

        // Pointer Settings
        public ExitPollManager.PointerType PointerType;
        public ExitPollManager.PointerInputButton PointerActivationButton;
        public GameObject HMDPointerPrefab;
        public GameObject PointerControllerPrefab;
        public float PointerLineWidth = 0.01f;
        public Gradient PointerGradient = new Gradient { colorKeys = new GradientColorKey[] 
            { 
                new GradientColorKey(new Color(0.286f, 0.106f, 0.631f, 1f), 0f), // Purple
                new GradientColorKey(new Color(0.055f, 0.416f, 0.624f, 1f), 0.5f), // Blue
                new GradientColorKey(new Color(0.039f, 0.557f, 0.259f, 1f), 1f) // Green
            } 
        };

        public GameObject BoolPanelOverride;
        public GameObject HappyPanelOverride;
        public GameObject ThumbsPanelOverride;
        public GameObject MultiplePanelOverride;
        public GameObject ScalePanelOverride;
        public GameObject VoicePanelOverride;

        public Material PanelTextMaterial;
        public Material PanelErrorTextMaterial;
        public Material PanelBackgroundMaterial;

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