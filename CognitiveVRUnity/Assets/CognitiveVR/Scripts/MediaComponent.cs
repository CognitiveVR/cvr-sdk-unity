using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//goes on a mesh collider. found during gaze
//either on a visible surface or hidden to record the uvs for a skybox

namespace CognitiveVR
{
    [HelpURL("https://docs.cognitive3d.com/unity/media/")]
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(DynamicObject))]
    [AddComponentMenu("Cognitive3D/Common/Media Component")]
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
            if (!IsVideo) { return; }
            CognitiveVR.Core.TickEvent += CognitiveVR_Manager_TickEvent;
        }

        bool wasPrepared = true;
        bool WasPlaying = false;
        long lastFrame = 0;

        private void CognitiveVR_Manager_TickEvent()
        {
            if (!IsVideo) { return; }
            if (WasPlaying)
            {
                if (!VideoPlayer.isPlaying)
                {
                    if (VideoPlayer.frame == 0)
                    {
                        //stopped event
                        CustomEvent.SendCustomEvent("cvr.media.stop", new List<KeyValuePair<string, object>>() {new KeyValuePair<string, object>( "videoTime", lastFrame ), new KeyValuePair<string, object>("mediaId", MediaSource ) },transform.position);
                    }
                    else
                    {
                        //paused event
                        CustomEvent.SendCustomEvent("cvr.media.pause", new List<KeyValuePair<string, object>>() { new KeyValuePair<string, object>("videoTime", VideoPlayer.frame ), new KeyValuePair<string, object>("mediaId", MediaSource ) }, transform.position);
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
                    CustomEvent.SendCustomEvent("cvr.media.play", new List<KeyValuePair<string, object>>() { new KeyValuePair<string, object>("videoTime", VideoPlayer.frame ), new KeyValuePair<string, object>("mediaId", MediaSource ) }, transform.position);
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
                    CustomEvent.SendCustomEvent("cvr.media.videoBuffer", new List<KeyValuePair<string, object>>() { new KeyValuePair<string, object>("videoTime", VideoPlayer.frame ), new KeyValuePair<string, object>("mediaId", MediaSource ) }, transform.position);
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

        private void OnDestroy()
        {
            CognitiveVR.Core.TickEvent -= CognitiveVR_Manager_TickEvent;
        }
    }
}