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
                helper.Initialize(() => OnHelperPostRender());
                RTex = new RenderTexture(Resolution, Resolution, 0);
            }
        }

        CognitiveVR.Components.PlayerRecorderHelper helper;
        bool hasHitDynamic = false;
        string hitDynamicObjectId;
        float hitDynamicDistance;
        Vector3 hitDynamicLocalGaze;

        private void CognitiveVR_Manager_TickEvent()
        {
            //raycast for dynamics
            //on render image for other stuff


            helper.enabled = true;
            RTex = helper.DoRender(RTex);
            helper.enabled = false;


            RaycastHit hit = new RaycastHit();
            Ray ray = new Ray(CameraTransform.position, CameraTransform.forward);

            //TODO ray origin should include gaze direction

            if (Physics.Raycast(ray, out hit, cam.farClipPlane))
            {
                Vector3 pos = CameraTransform.position;
                Vector3 gazepoint = hit.point;
                Quaternion rot = CameraTransform.rotation;

                DynamicObject dyn = null;
                if (CognitiveVR_Preferences.S_DynamicObjectSearchInParent)
                    dyn = hit.collider.GetComponentInParent<DynamicObject>();
                else
                    dyn = hit.collider.GetComponent<DynamicObject>();

                if (dyn != null) //hit dynamic object
                {
                    string ObjectId = dyn.ObjectId.Id;
                    hitDynamicLocalGaze = dyn.transform.InverseTransformPointUnscaled(hit.point);
                    hitDynamicDistance = hit.distance;
                    hitDynamicObjectId = dyn.ObjectId.Id;
                    //GazeCore.RecordGazePoint(Util.Timestamp(), ObjectId, LocalGaze, pos, rot);
                    hasHitDynamic = true;
                }
            }
        }

        private void OnHelperPostRender()
        {
            //evaluate if a dynamic was hit
            //refresh stuff

            if (!hasHitDynamic)
            {
                Vector3 point;

                Vector3 gazedirection = CameraTransform.forward;
                Vector2 screenGazePoint = Vector2.one * 0.5f;

#if CVR_FOVE //direction
            var eyeRays = FoveInstance.GetGazeRays();
            var ray = eyeRays.left;
            gazeDirection = new Vector3(ray.direction.x, ray.direction.y, ray.direction.z);
            gazeDirection.Normalize();
#endif //fove direction
#if CVR_PUPIL //direction
            //var v2 = PupilGazeTracker.Instance.GetEyeGaze(PupilGazeTracker.GazeSource.BothEyes); //0-1 screen pos
            var v2 = PupilData._2D.GetEyeGaze(Pupil.GazeSource.BothEyes);

            //if it doesn't find the eyes, skip this snapshot
            if (PupilTools.Confidence(PupilData.rightEyeID) > 0.1f)
            {
                var ray = instance.cam.ViewportPointToRay(v2);
                gazeDirection = ray.direction.normalized;
            } //else uses HMD forward
#endif //pupil direction


#if CVR_FOVE //screenpoint

            //var normalizedPoint = FoveInterface.GetNormalizedViewportPosition(ray.GetPoint(1000), Fove.EFVR_Eye.Left); //Unity Plugin Version 1.3.1
            var normalizedPoint = cam.WorldToViewportPoint(ray.GetPoint(1000));

            //Vector2 gazePoint = hmd.GetGazePoint();
            if (float.IsNaN(normalizedPoint.x))
            {
                return;
            }

            screenGazePoint = new Vector2(normalizedPoint.x, normalizedPoint.y);
#endif //fove screenpoint
#if CVR_PUPIL//screenpoint
            screenGazePoint = PupilData._2D.GetEyeGaze(Pupil.GazeSource.BothEyes);
#endif //pupil screenpoint



                bool hitworld = GetGazePoint(Resolution, Resolution, out point, CameraComponent.nearClipPlane, CameraComponent.farClipPlane, CameraTransform.forward, CameraTransform.position, screenGazePoint, gazedirection);

                if (hitworld) //hit world
                {
                    GazeCore.RecordGazePoint(Util.Timestamp(), point, CameraTransform.position, CameraTransform.rotation);
                }
                else //hit skybox
                {
                    GazeCore.RecordGazePoint(Util.Timestamp(), CameraTransform.position, CameraTransform.rotation);
                }
            }

            //TODO compare distance between dynamic hit point and world hit point

            hasHitDynamic = false;
        }

        private void OnDestroy()
        {
            Destroy(helper);
            CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
        }


        public static int Resolution = 64;
        public static ColorSpace colorSpace = ColorSpace.Linear;
        public static RenderTexture RTex;

        private static float GetAdjustedDistance(float far, Vector3 gazeDir, Vector3 camForward)
        {
            //====== edge
            //float height = 2 * far * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            //float width = height * aspect;
            //Debug.Log("height: " + height + "\nwidth: "+width);
            //this is the maximum distance - looking directly at the edge of the frustrum
            //float edgeDistance = Mathf.Sqrt(Mathf.Pow(width * 0.5f, 2) + Mathf.Pow(cam.farClipPlane, 2));

            //Vector3 position = CognitiveVR_Manager.HMD.position;

            //======= gaze (farclip distance)
            //Debug.DrawRay(position, gazeDir * far, Color.red, 0.1f);
            //Debug.DrawRay(position, camForward * far, new Color(0.5f, 0, 0), 0.1f);

            //Debug.Log("get adjusted distance. gaze direction " + gazeDir + "      camera forward  " + camForward);

            //dot product to find projection of gaze with direction
            float fwdAmount = Vector3.Dot(gazeDir.normalized * far, camForward.normalized);
            //Vector3 fwdPoint = camForward * fwdAmount;
            //Debug.DrawRay(fwdPoint, Vector3.up, Color.cyan, 0.1f);
            //Debug.DrawRay(gazeDir * far, Vector3.up, Color.yellow, 0.1f);

            //======= angle towards farPlane
            //get angle between center and gaze direction. cos(A) = b/c
            float gazeRads = Mathf.Acos(fwdAmount / far);
            if (Mathf.Approximately(fwdAmount, far))
            {
                //when fwdAmount == far, Acos returns NaN for some reason
                gazeRads = 0;
            }

            float dist = far * Mathf.Tan(gazeRads);

            float hypotenuseDist = Mathf.Sqrt(Mathf.Pow(dist, 2) + Mathf.Pow(far, 2));

            //float missingDist = hypotenuseDist-far;
            //Debug.DrawRay(position + gazeDir * far, gazeDir * missingDist, Color.green, 0.1f); //appended distance to hit farPlane
            return hypotenuseDist;
        }

        /// <summary>
        /// ignores width and height if using gaze tracking from fove/pupil labs
        /// returns true if it hit a valid point. false if the point is at the farplane
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static bool GetGazePoint(int width, int height, out Vector3 gazeWorldPoint, float neardepth, float fardepth, Vector3 hmdforward, Vector3 hmdpos, Vector3 HMDGazePoint, Vector3 GazeDirection)
        {
#if CVR_FOVE || CVR_PUPIL
            float relativeDepth = 0;

            Vector2 snapshotPixel = HMDGazePoint;

            snapshotPixel *= Resolution;

            snapshotPixel.x = Mathf.Clamp(snapshotPixel.x, 0, Resolution-1);
            snapshotPixel.y = Mathf.Clamp(snapshotPixel.y, 0, Resolution-1);

            var color = GetRTColor(RTex, (int)snapshotPixel.x, (int)snapshotPixel.y);

            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                relativeDepth = color.linear.r; //does running the color through this linear multiplier cause NaN issues? GetAdjustedDistance passed in essentially 0?
            }
            else
            {
                relativeDepth = color.r;
            }

            if (relativeDepth > 0.99f)
            {
                gazeWorldPoint = Vector3.zero;
                return false;
            }

            //Debug.Log("relativeDepth " + relativeDepth);

            //how does this get the actual depth? missing an argument?
            float actualDepth = GetAdjustedDistance(FarDepth, GazeDirection, HMDForward);
            //Debug.Log("actualDepth " + actualDepth); //adjusted for trigonometry

            float actualDistance = Mathf.Lerp(NearDepth, actualDepth, relativeDepth);
            //Debug.Log("actualDistance " + actualDistance);

            gazeWorldPoint = Position + GazeDirection * actualDistance;

            //Debug.Log("gazeWorldPoint " + gazeWorldPoint);

            return true;
#else
            float relativeDepth = 0;
            //Vector3 gazeWorldPoint;

            //var color = GetRTColor((RenderTexture)Properties["renderDepth"], width / 2, height / 2);
            var color = GetRTColor(RTex, width / 2, height / 2);
            if (colorSpace == ColorSpace.Linear)
            {
                relativeDepth = color.linear.r;

            }
            else
            {
                relativeDepth = color.r;
            }

            if (relativeDepth > 0.99f)
            {
                gazeWorldPoint = Util.vector_zero;
                return false;
            }


            //float actualDistance = Mathf.Lerp(NearDepth, FarDepth, relativeDepth);
            float actualDistance = neardepth + (fardepth - neardepth) * relativeDepth;
            gazeWorldPoint = hmdpos + hmdforward * actualDistance;

            //float actualDistance = Mathf.Lerp((float)Properties["nearDepth"], (float)Properties["farDepth"], relativeDepth);
            //gazeWorldPoint = (Vector3)Properties["position"] + (Vector3)Properties["hmdForward"] * actualDistance;
            return true;
#endif


        }

        private static Texture2D tex;
        private static Color GetRTColor(RenderTexture rt, int x, int y)
        {
            if (tex == null)
            {
#if CVR_FOVE || CVR_PUPIL
                tex = new Texture2D(Resolution, Resolution);
#else
                tex = new Texture2D(1, 1);
#endif
            }

            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = rt;

#if CVR_FOVE || CVR_PUPIL //TODO read 1 pixel from the render texture where the request point is
            tex.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0, false);
            var color = tex.GetPixel(x,y);
#else
            tex.ReadPixels(new Rect(x, y, 1, 1), 0, 0, false);
            var color = tex.GetPixel(0, 0);
#endif

            RenderTexture.active = currentActiveRT;
            return color;
        }
    }
}