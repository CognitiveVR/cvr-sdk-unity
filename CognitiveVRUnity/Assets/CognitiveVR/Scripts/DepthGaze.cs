using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;

//gets a depth render texture from the camera
//raycasts to see if gazing at a dynamic object
//calculates distance of depth from camera
namespace CognitiveVR
{
    public class DepthGaze : GazeBase
    {
        private int Resolution = 64;
        private RenderTexture RTex;
        private Texture2D tex;
        CognitiveVR.Components.PlayerRecorderHelper helper;

        public override void Initialize()
        {
            base.Initialize();
            CognitiveVR_Manager.InitEvent += CognitiveVR_Manager_InitEvent;
        }

        private void CognitiveVR_Manager_InitEvent(Error initError)
        {
            if (initError == Error.Success)
            {
                CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_TickEvent;
                helper = CameraTransform.gameObject.AddComponent<CognitiveVR.Components.PlayerRecorderHelper>();
                helper.Initialize(OnHelperPostRender);
                RTex = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.ARGBFloat);
                helper.enabled = false;
            }
        }

        private void CognitiveVR_Manager_TickEvent()
        {
            if (helper == null)
            {
                helper = CameraTransform.gameObject.AddComponent<CognitiveVR.Components.PlayerRecorderHelper>();
                helper.Initialize(OnHelperPostRender);
            }
            helper.enabled = true;
            RTex = helper.DoRender(RTex);
            helper.enabled = false;
        }

        private void OnHelperPostRender()
        {
            Ray ray = new Ray(CameraTransform.position, GetWorldGazeDirection());

            Vector3 gpsloc = new Vector3();
            float compass = 0;
            Vector3 floorPos = new Vector3();

            GetOptionalSnapshotData(ref gpsloc, ref compass, ref floorPos);


            //raycast for dynamic
            float hitDistance;
            DynamicObject hitDynamic;
            Vector3 hitWorld;
            string objectId="";
            Vector3 localGaze = Vector3.zero;
            Vector2 hitcoord;
            if (DynamicRaycast(ray.origin, ray.direction, CameraComponent.farClipPlane, 0.05f, out hitDistance, out hitDynamic, out hitWorld, out hitcoord)) //hit dynamic
            {
                objectId = hitDynamic.Id;
                localGaze = hitDynamic.transform.InverseTransformPointUnscaled(hitWorld);
            }

            //get depth world point
            Vector3 point;
            Vector3 gazedirection = GetWorldGazeDirection();
            Vector2 screenGazePoint = GetViewportGazePoint();
            bool hitworld = GetGazePoint(Resolution, Resolution, out point, CameraComponent.nearClipPlane, CameraComponent.farClipPlane, CameraTransform.forward, CameraTransform.position, screenGazePoint, gazedirection);
            float depthDistance = Vector3.Distance(point, CameraTransform.position);

            if (hitDistance > 0 && hitDistance < depthDistance) //hit a dynamic object closer than the scene depth
            {
                hitDynamic.OnGaze(CognitiveVR_Preferences.S_SnapshotInterval);

                var mediacomponent = hitDynamic.GetComponent<MediaComponent>();
                if (mediacomponent != null)
                {
                    var mediatime = mediacomponent.IsVideo ? (int)((mediacomponent.VideoPlayer.frame / mediacomponent.VideoPlayer.frameRate) * 1000) : 0;
                    var mediauvs = hitcoord;
                    GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), objectId, localGaze, CameraTransform.position, CameraTransform.rotation, gpsloc, compass, mediacomponent.MediaSource, mediatime, mediauvs, floorPos);
                }
                else
                {
                    GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), objectId, localGaze, ray.origin, CameraTransform.rotation, gpsloc, compass, floorPos);
                }                
                Debug.DrawLine(CameraTransform.position, hitWorld, Color.magenta, 1);
            }
            else //didn't hit a dynamic, or hit one further away than the depth buffer
            {
                if (hitworld) //hit world
                {
                    GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), point, CameraTransform.position, CameraTransform.rotation, gpsloc, compass, floorPos);
                    Debug.DrawLine(CameraTransform.position, point, Color.red,1);
                }
                else //hit skybox
                {
                    GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), CameraTransform.position, CameraTransform.rotation, gpsloc, compass, floorPos);
                    Debug.DrawRay(CameraTransform.position, CameraTransform.forward * CameraComponent.farClipPlane, Color.cyan,1);
                }
            }
        }

        private void OnDestroy()
        {
            Destroy(helper);
            CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
        }

        //returns the depth adjusted by near/farplanes. without this, gaze distance doesn't scale correctly when looking off center
        private float GetAdjustedDistance(float far, Vector3 gazeDir, Vector3 camForward)
        {
            float fwdAmount = Vector3.Dot(gazeDir.normalized * far, camForward.normalized);
            //get angle between center and gaze direction. cos(A) = b/c
            float gazeRads = Mathf.Acos(fwdAmount / far);
            if (Mathf.Approximately(fwdAmount, far))
            {
                //when fwdAmount == far, Acos returns NaN for some reason
                gazeRads = 0;
            }

            float dist = far * Mathf.Tan(gazeRads);
            float hypotenuseDist = Mathf.Sqrt(Mathf.Pow(dist, 2) + Mathf.Pow(far, 2));
            return hypotenuseDist;
        }

        /// <summary>
        /// ignores width and height if using gaze tracking from fove/pupil labs
        /// returns true if it hit a valid point. false if the point is at the farplane
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private bool GetGazePoint(int width, int height, out Vector3 gazeWorldPoint, float neardepth, float fardepth, Vector3 hmdforward, Vector3 hmdpos, Vector3 HMDGazePoint, Vector3 GazeDirection)
        {
#if CVR_FOVE || CVR_PUPIL || CVR_TOBIIVR || CVR_NEURABLE || CVR_AH
            float relativeDepth = 0;
            Vector2 snapshotPixel = HMDGazePoint;

            snapshotPixel *= Resolution;
            snapshotPixel.x = Mathf.Clamp(snapshotPixel.x, 0, Resolution-1);
            snapshotPixel.y = Mathf.Clamp(snapshotPixel.y, 0, Resolution-1);

            var color = GetRTColor(RTex, (int)snapshotPixel.x, (int)snapshotPixel.y);
            relativeDepth = color.r;

            if (relativeDepth > 0.99f) //far plance
            {
                gazeWorldPoint = Vector3.zero;
                return false;
            }

            float actualDepth = GetAdjustedDistance(fardepth, GazeDirection, hmdforward);
            float actualDistance = Mathf.Lerp(neardepth, actualDepth, relativeDepth);
            gazeWorldPoint = hmdpos + GazeDirection * actualDistance;
            return true;
#else
            float relativeDepth = 0;
            //Vector3 gazeWorldPoint;

            //var color = GetRTColor((RenderTexture)Properties["renderDepth"], width / 2, height / 2);
            var color = GetRTColor(RTex, width / 2, height / 2);
            relativeDepth = color.r;

            if (relativeDepth > 0.99f)
            {
                gazeWorldPoint = Util.vector_zero;
                return false;
            }
            
            float actualDistance = neardepth + (fardepth - neardepth) * relativeDepth;
            gazeWorldPoint = hmdpos + hmdforward * actualDistance;

            return true;
#endif
        }

        private Color GetRTColor(RenderTexture rt, int x, int y)
        {
            if (tex == null)
            {
#if CVR_FOVE || CVR_PUPIL || CVR_TOBIIVR || CVR_NEURABLE || CVR_AH
                tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBAFloat,false);
#else
                //tex = new Texture2D(Resolution, Resolution,TextureFormat.ARGB32, false);
                tex = new Texture2D(1, 1);
#endif
            }

            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = rt;

#if CVR_FOVE || CVR_PUPIL || CVR_TOBIIVR || CVR_NEURABLE || CVR_AH
            tex.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0, false);
            //Graphics.CopyTexture(rt, tex);
            var color = tex.GetPixel(x,y);
#else
            //Graphics.CopyTexture(rt, tex);
            tex.ReadPixels(new Rect(x, y, 1, 1), 0, 0, false);
            var color = tex.GetPixel(0, 0);
#endif

            RenderTexture.active = currentActiveRT;
            return color;
        }
    }
}