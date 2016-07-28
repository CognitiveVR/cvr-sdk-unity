using UnityEngine;
using CognitiveVR;
using System.Collections;
using System.Collections.Generic;

//goes somewhere in the scene
//this should have the http request components on it
namespace CognitiveVR
{
    public class CognitiveVR_Manager : MonoBehaviour
    {
        //fps
        int samples = 30;
        List<float> framerates = new List<float>();

        //chaperone
        string chaperoneGUID;

        string hmdpresentGUID;

        void Start()
        {
            CognitiveVR.InitParams initParams = CognitiveVR.InitParams.create
            (
                customerId: CognitiveVR_Preferences.Instance.CustomerID
            );
            CognitiveVR.Core.init(initParams, InitCallback);
            GameObject.DontDestroyOnLoad(gameObject);
        }

        void InitCallback(Error initError)
        {
            Debug.Log("CognitiveVR Initialize. Result: " + initError);
            if (initError != Error.Success) { return; }

            //USER STEAM ID
            //if you are using steamworks.net (https://steamworks.github.io/installation/) you can use this code to pass in your user's steam id
            //Steamworks.SteamAPI.Init(); //doesn't have to be called here, but Steamworks must be Initialized before you call GetSteamID()
            //EntityInfo user = CognitiveVR.EntityInfo.createUserInfo(Steamworks.SteamUser.GetSteamID().ToString());
            //Core.registerUser(user, delegate (Error error) { });

            CognitiveVR.Plugins.Session.Transaction().begin();

            if (CognitiveVR.CognitiveVR_Preferences.Instance.TrackBatteryLevel) SendBatteryLevel();
            if (CognitiveVR_Preferences.Instance.TrackChaperoneVisible)
            {
                //TODO register if chaperone is visible or not on initialization
            }
        }

        void OnLevelWasLoaded()
        {
            //TODO send player snapshots option
        }

        bool hasDelayed;
        void OnApplicationQuit()
        {
            bool doDelayQuit = false;
            if (CognitiveVR_Preferences.Instance.sendDataOnQuit)
            {
                doDelayQuit = true;
            }
            if (CognitiveVR_Preferences.Instance.SessionEndOnApplicationQuit)
            {
                doDelayQuit = true;
                CognitiveVR.Plugins.Session.Transaction().setProperty("duration",Time.time).end();
            }
            if (CognitiveVR_Preferences.Instance.TotalTimePlayedCollectionUpdate)
            {
                //CognitiveVR.Instrumentation.updateCollection("TimePlayed", 5, Time.time, false);
            }
            if (CognitiveVR_Preferences.Instance.TrackBatteryLevel)
            {
                doDelayQuit = true;
                SendBatteryLevel();
            }
            //TODO send player snapshots on HMD removed

            if (doDelayQuit && !hasDelayed)
            {
                Application.CancelQuit();
                StartCoroutine(DelayQuit());
            }
        }

        float updateInterval = 3.0f;
        float timeleft;
        float accum;
        int frames;
        bool lowFramerate;
        string fpsTransactionID;

        void Update()
        {
            if (CognitiveVR.CognitiveVR_Preferences.Instance.TrackLowFramerateThreshold || CognitiveVR_Preferences.Instance.TrackComfort) UpdateFramerate();
            if (CognitiveVR_Preferences.Instance.TrackComfort) UpdateHMDRotation();

#if CVR_STEAMVR
            UpdateSteamVREvents();
#endif
        }

#if CVR_STEAMVR
        void UpdateSteamVREvents()
        {
            var system = Valve.VR.OpenVR.System;
            if (system != null)
            {
                var vrEvent = new Valve.VR.VREvent_t();
                var size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Valve.VR.VREvent_t));
                for (int i = 0; i < 64; i++)
                {
                    if (!system.PollNextEvent(ref vrEvent, size))
                        break;
                    if (CognitiveVR_Preferences.Instance.HMDProximity)
                    {
                        if ((Valve.VR.EVREventType)vrEvent.eventType == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted)
                        {
                            mdpresentGUID = System.Guid.NewGuid().ToString();
                            Instrumentation.Transaction("HMDPresent", hmdpresentGUID).properties("present",true).properties("starttime",Time.time).begin();
                        }
                        if ((Valve.VR.EVREventType)vrEvent.eventType == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded)
                        {
                            Instrumentation.Transaction("HMDPresent", hmdpresentGUID).properties("present",false).properties("endtime",Time.time-10f).end();
                        }
                    }
                    if (CognitiveVR_Preferences.Instance.TrackChaperoneVisible)
                    {
                        if ((Valve.VR.EVREventType)vrEvent.eventType == Valve.VR.EVREventType.VREvent_ChaperoneDataHasChanged)
                        {
                            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
                            {
                                chaperoneGUID = System.Guid.NewGuid().ToString();
                                Instrumentation.Transaction("chaperone", chaperoneGUID).begin();
                            }
                            else
                            {
                                Instrumentation.Transaction("chaperone", chaperoneGUID).end();
                            }
                        }
                    }
                }
            }
        }
#endif //steamvr

        void SendBatteryLevel()
        {
            Instrumentation.updateDeviceState(new Dictionary<string, object> { { "batterylevel", GetBatteryLevel() } } );
        }

        public static float GetBatteryLevel()
        {
#if CVR_OCULUS
            //TODO return oculus battery level
#endif

            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        if (null != unityPlayer)
                        {
                            using (AndroidJavaObject currActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                            {
                                if (null != currActivity)
                                {
                                    using (AndroidJavaObject intentFilter = new AndroidJavaObject("android.content.IntentFilter", new object[]{ "android.intent.action.BATTERY_CHANGED" }))
                                    {
                                        using (AndroidJavaObject batteryIntent = currActivity.Call<AndroidJavaObject>("registerReceiver", new object[]{null,intentFilter}))
                                        {
                                            int level = batteryIntent.Call<int>("getIntExtra", new object[]{"level",-1});
                                            int scale = batteryIntent.Call<int>("getIntExtra", new object[]{"scale",-1});
 
                                            // Error checking that probably isn't needed but I added just in case.
                                            if (level == -1 || scale == -1)
                                            {
                                                return 50f;
                                            }
                                            return ((float)level / (float)scale) * 100.0f; 
                                        }
                                 
                                    }
                                }
                            }
                        }
                    }
                } catch (System.Exception ex){}
         }
        
         return 100f;
        }

        Quaternion lastRotation;
        PlayerTracker HMD;
        float accumRotation;
        float rotTimeLeft;
        int rotFrames;
        float lastRps;

        void UpdateHMDRotation()
        {
            if (HMD == null) { HMD = FindObjectOfType<PlayerTracker>(); }

            rotTimeLeft -= Time.deltaTime;
            accumRotation += Quaternion.Angle(HMD.transform.rotation, lastRotation)/Time.deltaTime;
            lastRotation = HMD.transform.rotation;
            ++rotFrames;

            // Interval ended - update GUI text and start new interval
            if (rotTimeLeft <= 0.0)
            {
                lastRps = accumRotation / rotFrames;
                //	DebugConsole.Log(format,level);
                rotTimeLeft = updateInterval;
                accumRotation = 0.0F;
                rotFrames = 0;

                //Debug.Log(rps);

                Instrumentation.Transaction("comfort", fpsTransactionID).setProperty("fps", lastFps).setProperty("rps", lastRps).begin();
            }
        }

        float lastFps;
        void UpdateFramerate()
        {
            timeleft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            ++frames;

            // Interval ended - update GUI text and start new interval
            if (timeleft <= 0.0)
            {
                lastFps = accum / frames;
                //	DebugConsole.Log(format,level);
                timeleft = updateInterval;
                accum = 0.0F;
                frames = 0;

                if (lastFps < CognitiveVR.CognitiveVR_Preferences.Instance.LowFramerateThreshold && !lowFramerate)
                {
                    lowFramerate = true;
                    fpsTransactionID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("performance", fpsTransactionID).setProperty("fps", lastFps).begin();
                }
                else if (lastFps > CognitiveVR.CognitiveVR_Preferences.Instance.LowFramerateThreshold && lowFramerate)
                {
                    lowFramerate = false;
                    Instrumentation.Transaction("performance", fpsTransactionID).end();
                }
            }
        }

        IEnumerator DelayQuit()
        {
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
    }
}