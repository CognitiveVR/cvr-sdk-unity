using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cognitive3D.ActiveSession
{
    [AddComponentMenu("Cognitive3D/Active Session View/Data Batch Canvas")]
    public class DataBatchCanvas : MonoBehaviour
    {
        public Text EventSendText;
        public Text GazeSendText;
        public Text FixationSendText;
        public Text DynamicSendText;
        public Text SensorSendText;

        void Start()
        {
            if (Cognitive3D_Manager.IsInitialized) { Core_InitEvent(); }
            Cognitive3D_Manager.OnSessionBegin += Core_InitEvent;
            Cognitive3D_Manager.OnPreSessionEnd += Core_EndSessionEvent;
        }

        private void Core_InitEvent()
        {
            CustomEvent.OnCustomEventSend += CustomEvent_OnCustomEventSend;
            GazeCore.OnGazeSend += GazeCore_OnGazeSend;
            FixationRecorder.OnFixationSend += FixationCore_OnFixationSend;
            DynamicManager.OnDynamicObjectSend += DynamicManager_OnDynamicObjectSend;
            SensorRecorder.OnSensorSend += SensorRecorder_OnSensorSend;
        }

        float EventTimeSinceSend = -1;
        float GazeTimeSinceSend = -1;
        float FixationTimeSinceSend = -1;
        float DynamicTimeSinceSend = -1;
        float SensorTimeSinceSend = -1;

        public Color noDataColor;
        public Color normalColor;
        public Color justSentColor;
        string neverSentString = "No data";
        string justSentString = "Just sent!";

        float timeSinceLastTick;
        int frame = 0;
        private void Update()
        {
            timeSinceLastTick += Time.deltaTime;
            frame++;
            if (frame < 10) { return; }
            frame = 0;

            //TODO IMPROVEMENT split 'no data available' and 'not sent yet' on internal state

            #region Events
            if (EventTimeSinceSend < 0)
            {
                //no data
                EventSendText.color = noDataColor;
                EventSendText.text = neverSentString;
            }
            else
            {
                //just sent or send X seconds ago
                UpdateText(EventSendText, ref EventTimeSinceSend);
            }
            #endregion

            #region Gaze
            if (GazeTimeSinceSend < 0)
            {
                GazeSendText.color = noDataColor;
                GazeSendText.text = neverSentString;
            }
            else
            {
                UpdateText(GazeSendText, ref GazeTimeSinceSend);
            }
            #endregion

            #region Fixations
            if (FixationTimeSinceSend < 0)
            {
                FixationSendText.color = noDataColor;
                FixationSendText.text = neverSentString;
            }
            else
            {
                UpdateText(FixationSendText, ref FixationTimeSinceSend);
            }
            #endregion

            #region Dynamics
            if (DynamicTimeSinceSend < 0)
            {
                DynamicSendText.color = noDataColor;
                DynamicSendText.text = neverSentString;
            }
            else
            {
                UpdateText(DynamicSendText, ref DynamicTimeSinceSend);
            }
            #endregion

            #region Sensors
            if (SensorTimeSinceSend < 0)
            {
                SensorSendText.color = noDataColor;
                SensorSendText.text = neverSentString;
            }
            else
            {
                UpdateText(SensorSendText, ref SensorTimeSinceSend);
            }
            #endregion

            timeSinceLastTick = 0;
        }

        static string[] sendTimeArray = new string[] {
              " ", "1s ago", "2s ago", "3s ago", "4s ago", "5s ago", "6s ago", "7s ago", "8s ago", "9s ago"
            , "10s ago", "11s ago", "12s ago", "13s ago", "14s ago", "15s ago", "16s ago", "17s ago", "18s ago", "19s ago"
            , "20s ago", "21s ago", "22s ago", "23s ago", "24s ago", "25s ago", "26s ago", "27s ago", "28s ago", "29s ago"
            , "30s ago", ">30s ago"
        };

        void UpdateText(Text t, ref float sendtime)
        {
            if (sendtime < 1)
            {
                sendtime += timeSinceLastTick;
                t.color = justSentColor;
                t.text = justSentString;
            }
            else
            {
                sendtime += timeSinceLastTick;
                t.color = normalColor;

                int floorInt = Mathf.FloorToInt(sendtime);
                if (floorInt < sendTimeArray.Length)
                    t.text = sendTimeArray[floorInt];
                else
                    t.text = sendTimeArray[31];
            }
        }

        private void SensorRecorder_OnSensorSend(bool ignored)
        {
            SensorTimeSinceSend = 0;
        }

        private void DynamicManager_OnDynamicObjectSend(bool ignored)
        {
            DynamicTimeSinceSend = 0;
        }

        private void FixationCore_OnFixationSend(bool ignored)
        {
            FixationTimeSinceSend = 0;
        }

        private void GazeCore_OnGazeSend(bool ignored)
        {
            GazeTimeSinceSend = 0;
        }

        private void CustomEvent_OnCustomEventSend(bool ignored)
        {
            EventTimeSinceSend = 0;
        }

        private void OnDestroy()
        {
            CustomEvent.OnCustomEventSend -= CustomEvent_OnCustomEventSend;
            GazeCore.OnGazeSend -= GazeCore_OnGazeSend;
            FixationRecorder.OnFixationSend -= FixationCore_OnFixationSend;
            DynamicManager.OnDynamicObjectSend -= DynamicManager_OnDynamicObjectSend;
            SensorRecorder.OnSensorSend -= SensorRecorder_OnSensorSend;
        }

        private void Core_EndSessionEvent()
        {
            CustomEvent.OnCustomEventSend -= CustomEvent_OnCustomEventSend;
            GazeCore.OnGazeSend -= GazeCore_OnGazeSend;
            FixationRecorder.OnFixationSend -= FixationCore_OnFixationSend;
            DynamicManager.OnDynamicObjectSend -= DynamicManager_OnDynamicObjectSend;
            SensorRecorder.OnSensorSend -= SensorRecorder_OnSensorSend;
        }
    }
}