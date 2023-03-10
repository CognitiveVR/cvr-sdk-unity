using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//TODO IMPROVEMENT - offline data batches should increment when session is active then loses internect connection

namespace Cognitive3D
{
    namespace ActiveSession
    {
        [AddComponentMenu("Cognitive3D/Active Session View/Meta Canvas")]
        public class MetaCanvas : MonoBehaviour
        {
            public Text CurrentSessionLength;
            public Text SessionName;
            public Text SceneId;
            public Text OfflineBatches;

            void Awake()
            {
                Cognitive3D_Manager.OnSessionBegin += Core_InitEvent;
                Cognitive3D_Manager.OnLevelLoaded += Core_LevelLoadedEvent;
            }

            private void Core_LevelLoadedEvent(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode, bool newSceneId)
            {
                SceneId.text = Cognitive3D_Manager.TrackingSceneId;
            }

            private void Core_InitEvent()
            {
                Cognitive3D_Manager.OnSessionBegin -= Core_InitEvent;
                SessionName.text = Cognitive3D_Manager.SessionID;
                SceneId.text = Cognitive3D_Manager.TrackingSceneId;
            }

            int lastSecondTime = 0;
            int lastBatchStorage = 0;
            int currentBatchStorage = 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
            private void Update()
            {
                if (Cognitive3D_Manager.IsInitialized)
                {
                    double sessionTimeSec = (Util.Timestamp(Time.frameCount) - Cognitive3D_Manager.SessionTimeStamp);
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

                currentBatchStorage = Cognitive3D_Manager.GetLocalStorageBatchCount();
                if (currentBatchStorage != lastBatchStorage)
                {
                    OfflineBatches.text = (currentBatchStorage / 2).ToString();
                    currentBatchStorage = lastBatchStorage;
                }
            }

            private void OnDestroy()
            {
                Cognitive3D_Manager.OnSessionBegin -= Core_InitEvent;
                Cognitive3D_Manager.OnLevelLoaded -= Core_LevelLoadedEvent;
            }
        }
    }
}