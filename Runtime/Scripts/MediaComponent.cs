using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//goes on a mesh collider. found during gaze
//either on a visible surface or hidden to record the uvs for a skybox
//uses Unity's VideoPlayer component by default. if using a different video player, you will also need to modify MediaComponentInspector.cs

namespace Cognitive3D
{
    [HelpURL("https://docs.cognitive3d.com/unity/media/")]
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(DynamicObject))]
    [AddComponentMenu("Cognitive3D/Common/Media Component")]
    public class MediaComponent : MonoBehaviour
    {
        /// <summary>
        /// only used by the inspector to display a friendly name for the media
        /// </summary>
        
        [SerializeField]
        internal string MediaName;

        [UnityEngine.Serialization.FormerlySerializedAs("MediaSource")]
        public string MediaId;

        [System.Obsolete("Use MediaId instead")]
        public string MediaSource { get { return MediaId; } set { MediaId = value; } }

        public UnityEngine.Video.VideoPlayer VideoPlayer;

        #region Video Player Properties
        //if implementing an alternate video player, you should modify these properties to get the state of that video player component
        public bool IsVideo
        {
            get
            {
                return VideoPlayer != null;
            }
        }

        public bool VideoPlayerIsPlaying
        {
            get
            {
                if (IsVideo)
                {
                    return VideoPlayer.isPlaying;
                }
                return false;
            }
        }

        public bool VideoPlayerIsBuffered
        {
            get
            {
                if (IsVideo)
                {
                    return VideoPlayer.isPrepared;
                }
                return false;
            }
        }

        public long VideoPlayerFrame
        {
            get
            {
                if (IsVideo)
                {
                    return VideoPlayer.frame;
                }
                return 0;
            }
        }

        public float VideoPlayerFrameRate
        {
            get
            {
                if (IsVideo)
                {
                    return VideoPlayer.frameRate;
                }
                return 0;
            }
        }
        #endregion

        private void Start()
        {
            //not every frame + only if initialization is fine. MUST HAVE VALID SCENEID
            if (!IsVideo) { return; }
            Cognitive3D_Manager.OnTick += Cognitive3D_Manager_TickEvent;
        }

        bool wasPrepared = true;
        bool WasPlaying = false;
        long lastFrame = 0;

        private void Cognitive3D_Manager_TickEvent()
        {
            if (!IsVideo) { return; }
            if (WasPlaying)
            {
                if (!VideoPlayerIsPlaying)
                {
                    if (VideoPlayerFrame == 0)
                    {
                        //stopped event
                        CustomEvent.SendCustomEvent("cvr.media.stop", new List<KeyValuePair<string, object>> {new KeyValuePair<string, object>( "videoTime", lastFrame ), new KeyValuePair<string, object>("mediaId", MediaId) },transform.position);
                    }
                    else
                    {
                        //paused event
                        CustomEvent.SendCustomEvent("cvr.media.pause", new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("videoTime", VideoPlayerFrame), new KeyValuePair<string, object>("mediaId", MediaId) }, transform.position);
                    }
                    WasPlaying = false;
                }
                lastFrame = VideoPlayerFrame;
            }
            else
            {
                if (VideoPlayerIsPlaying)
                {
                    //play event
                    CustomEvent.SendCustomEvent("cvr.media.play", new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("videoTime", VideoPlayerFrame), new KeyValuePair<string, object>("mediaId", MediaId) }, transform.position);
                    WasPlaying = true;
                }
            }
            //register to prepare_complete to see if video has finished buffering
            //how to tell if video starts buffering again?

            if (wasPrepared)
            {
                if (!VideoPlayerIsBuffered) //started buffering. possibly stopped
                {
                    wasPrepared = false;
                    CustomEvent.SendCustomEvent("cvr.media.videoBuffer", new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("videoTime", VideoPlayerFrame), new KeyValuePair<string, object>("mediaId", MediaId) }, transform.position);
                }
            }
            else
            {
                if (VideoPlayerIsBuffered) //finishing buffering
                {
                    wasPrepared = true;
                }
            }
        }

        private void OnDestroy()
        {
            Cognitive3D_Manager.OnTick -= Cognitive3D_Manager_TickEvent;
        }
    }
}