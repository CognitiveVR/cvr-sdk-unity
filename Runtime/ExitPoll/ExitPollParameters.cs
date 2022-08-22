using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    //temporary object for holding overrides and settings for exitpoll question set
    [System.Serializable]
    public class ExitPollParameters
    {
        public string Hook;

        //hides irrelevent options in inspector
        public ExitPoll.SpawnType ExitpollSpawnType = ExitPoll.SpawnType.PlayerRelative;

        //parenting pointer
        public ExitPoll.PointerSource PointerParent;
        public Transform PointerParentOverride;

        //spawning or setting pointer
        public ExitPoll.PointerType PointerType;
        public GameObject PointerOverride;

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

        public bool UseTimeout = false;
        public float Timeout = 3;
        public LayerMask PanelLayerMask;// = LayerMask.GetMask("Default", "World", "Ground");
        //the prefered distance to display an exit 
        public float DisplayDistance = 3;
        public bool LockYPosition = true;

        //the minimum distance to display an exit poll. below this value will cancel the exit poll and continue with gameplay
        public float MinimumDisplayDistance = 0.2f;


        #region Functions

        public ExitPollParameters SetEndAction(System.Action<bool> endAction)
        {
            EndAction = endAction;
            return this;
        }

        public ExitPollParameters AddEndAction(System.Action<bool> endAction)
        {
            if (EndAction == null)
            {
                EndAction = endAction;
            }
            else
            {
                EndAction += endAction;
            }
            return this;
        }

        /// <summary>
        /// set a maximum time that a question will be displayed. if this is passed, the question closes automatically
        /// </summary>
        /// <param name="allowTimeout"></param>
        /// <param name="secondsUntilTimeout"></param>
        /// <returns></returns>
        public ExitPollParameters SetTimeout(bool allowTimeout, float secondsUntilTimeout)
        {
            UseTimeout = allowTimeout;
            Timeout = secondsUntilTimeout;
            return this;
        }

        public ExitPollParameters SetDisplayDistance(float preferedDistance, float minimumDistance)
        {
            MinimumDisplayDistance = Mathf.Max(minimumDistance, 0);
            DisplayDistance = Mathf.Max(minimumDistance, preferedDistance);

            return this;
        }

        /// <summary>
        /// Set the layers the Exit Poll panel will avoid
        /// </summary>
        /// <param name="layers"></param>
        /// <returns></returns>
        public ExitPollParameters SetPanelLayerMask(params string[] layers)
        {
            PanelLayerMask = LayerMask.GetMask(layers);
            return this;
        }
        
        /// <summary>
        /// set a scene gameobject to calculating pointing at exitpoll buttons
        /// </summary>
        /// <param name="visible"></param>
        /// <returns></returns>
        public ExitPollParameters SetControllerPointer(GameObject controller)
        {
            PointerParent = ExitPoll.PointerSource.RightHand;
            PointerOverride = controller;
            return this;
        }

        /// <summary>
        /// Use to HMD Y position instead of spawning the poll directly ahead of the player
        /// </summary>
        /// <param name="useLockYPosition"></param>
        /// <returns></returns>
        public ExitPollParameters SetLockYPosition(bool useLockYPosition)
        {
            LockYPosition = useLockYPosition;
            return this;
        }
        
        /// <summary>
        /// If this window is not in the player's line of sight, rotate around the player toward their facing
        /// </summary>
        /// <param name="useRotateToOnscreen"></param>
        /// <returns></returns>
        public ExitPollParameters SetRotateToStayOnScreen(bool useRotateToOnscreen)
        {
            RotateToStayOnScreen = useRotateToOnscreen;
            return this;
        }

        public ExitPollParameters SetPosition(Vector3 overridePosition)
        {
            OverridePosition = overridePosition;
            return this;
        }

        public ExitPollParameters SetRotation(Quaternion overrideRotation)
        {
            OverrideRotation = overrideRotation;
            return this;
        }

        /// <summary>
        /// Update the position of the Exit Poll prefab if the player teleports
        /// </summary>
        /// <param name="useStickyWindow"></param>
        /// <returns></returns>
        public ExitPollParameters SetStickyWindow(bool useStickyWindow)
        {
            StickWindow = useStickyWindow;
            return this;
        }

        #endregion

        public ExitPollSet Begin()
        {
            var exitpollset = new ExitPollSet();
            exitpollset.BeginExitPoll(this);
            return exitpollset;
        }
    }
}