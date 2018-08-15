using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//goes on a mesh collider. found during gaze
//either on a visible surface or hidden to record the uvs for a skybox

namespace CognitiveVR
{
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(DynamicObject))]
    public class MediaComponent : MonoBehaviour
    {
        public string MediaSource;
        public bool IsVideo
        {
            get
            {
                return VideoPlayer != null;
            }
        }
        public UnityEngine.Video.VideoPlayer VideoPlayer;

        private void Start()
        {
            //not every frame + only if initialization is fine. MUST HAVE VALID SCENEID
            CognitiveVR.CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_TickEvent;
        }

        bool wasPrepared = true;
        bool WasPlaying = false;
        long lastFrame = 0;

        private void CognitiveVR_Manager_TickEvent()
        {
            if (IsVideo) { return; }
            if (WasPlaying)
            {
                if (!VideoPlayer.isPlaying)
                {
                    if (VideoPlayer.frame == 0)
                    {
                        //stopped event
                        Instrumentation.SendCustomEvent("cvr.media.stop", new Dictionary<string, object>() { { "videoTime", lastFrame }, { "mediaId", MediaSource } },transform.position);
                    }
                    else
                    {
                        //paused event
                        Instrumentation.SendCustomEvent("cvr.media.pause", new Dictionary<string, object>() { { "videoTime", VideoPlayer.frame }, { "mediaId", MediaSource } }, transform.position);
                    }
                    WasPlaying = false;
                }
                lastFrame = VideoPlayer.frame;
            }
            else
            {
                if (VideoPlayer.isPlaying)
                {
                    //play event
                    Instrumentation.SendCustomEvent("cvr.media.play", new Dictionary<string, object>() { { "videoTime", VideoPlayer.frame }, { "mediaId", MediaSource } }, transform.position);
                    WasPlaying = true;
                }
            }
            //register to prepare_complete to see if video has finished buffering
            //how to tell if video starts buffering again?

            if (wasPrepared)
            {
                if (!VideoPlayer.isPrepared) //started buffering. possibly stopped
                {
                    wasPrepared = false;
                    Instrumentation.SendCustomEvent("cvr.media.videoBuffer", new Dictionary<string, object>() { { "videoTime", VideoPlayer.frame }, { "mediaId", MediaSource } }, transform.position);
                }
            }
            else
            {
                if (VideoPlayer.isPrepared) //finishing buffering
                {
                    wasPrepared = true;
                }
            }
        }
    }
}