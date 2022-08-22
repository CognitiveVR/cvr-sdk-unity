using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CognitiveVR.External;

namespace CognitiveVR
{
    namespace ActiveSession
    {
        public class MetaCanvas : MonoBehaviour
        {
            public Text CurrentSessionLength;
            public Text SessionName;
            public Text SceneId;
            public Text OfflineBatches;

            void Start()
            {
                Core.InitEvent += Core_InitEvent;
                Core.LevelLoadedEvent += Core_LevelLoadedEvent;
            }

            private void Core_LevelLoadedEvent(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode, bool newSceneId)
            {
                SceneId.text = Core.TrackingSceneId;
            }

            private void Core_InitEvent(Error initError)
            {
                Core.InitEvent -= Core_InitEvent;
                if (initError == Error.None)
                    SessionName.text = Core.SessionID;
            }

            int lastSecondTime = 0;
            int lastBatchStorage = 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
            private void Update()
            {
                if (Core.IsInitialized)
                {
                    double sessionTimeSec = (Util.Timestamp(Time.frameCount) - Core.SessionTimeStamp);
                    if ((int)sessionTimeSec != lastSecondTime)
                    {
                        sb.Length = 0;
                        System.TimeSpan ts = new System.TimeSpan(0, 0, (int)sessionTimeSec);
                        sb.Append(ts.Hours.ToString("00"));
                        sb.Append(":");
                        sb.Append(ts.Minutes.ToString("00"));
                        sb.Append(":");
                        sb.Append(ts.Seconds.ToString("00"));
                        CurrentSessionLength.text = sb.ToString();
                        lastSecondTime = (int)sessionTimeSec;
                    }
                }

                int currentBatchStorage = Core.GetLocalStorageBatchCount();
                if (currentBatchStorage != lastBatchStorage)
                {
                    OfflineBatches.text = (currentBatchStorage / 2).ToString();
                    currentBatchStorage = lastBatchStorage;
                }
            }

            private void OnDestroy()
            {
                Core.InitEvent -= Core_InitEvent;
                Core.LevelLoadedEvent -= Core_LevelLoadedEvent;
            }
        }
    }
}