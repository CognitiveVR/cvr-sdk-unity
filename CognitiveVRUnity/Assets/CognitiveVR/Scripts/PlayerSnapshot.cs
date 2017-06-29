using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// container for cached player tracking data points
/// </summary>

namespace CognitiveVR
{
    public class PlayerSnapshot
    {
        public static int Resolution = 64;

        public double timestamp;
        public Dictionary<string, object> Properties = new Dictionary<string, object>();

        public PlayerSnapshot()
        {
            timestamp = Util.Timestamp();
        }

        public PlayerSnapshot(Dictionary<string, object> properties)
        {
            Properties = properties;
            timestamp = Util.Timestamp();
        }

        public float GetAdjustedDistance(float far, Vector3 gazeDir, Vector3 camForward)
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
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Vector3 GetGazePoint(int width, int height)
        {
#if CVR_GAZETRACK
            float relativeDepth = 0;
            Vector3 gazeWorldPoint;

            Vector2 snapshotPixel = (Vector2)Properties["hmdGazePoint"];

            snapshotPixel *= Resolution;

            snapshotPixel.x = Mathf.Clamp(snapshotPixel.x, 0, Resolution-1);
            snapshotPixel.y = Mathf.Clamp(snapshotPixel.y, 0, Resolution-1);

            var color = GetRTColor((RenderTexture)Properties["renderDepth"], (int)snapshotPixel.x, (int)snapshotPixel.y);

            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                relativeDepth = color.linear.r; //does running the color through this linear multiplier cause NaN issues? GetAdjustedDistance passed in essentially 0?
            }
            else
            {
                relativeDepth = color.r;
            }

            //Debug.Log("relativeDepth " + relativeDepth);

            //how does this get the actual depth? missing an argument?
            float actualDepth = GetAdjustedDistance((float)Properties["farDepth"], (Vector3)Properties["gazeDirection"], (Vector3)Properties["hmdForward"]);
            //Debug.Log("actualDepth " + actualDepth); //adjusted for trigonometry

            float actualDistance = Mathf.Lerp((float)Properties["nearDepth"], actualDepth, relativeDepth);
            //Debug.Log("actualDistance " + actualDistance);

            gazeWorldPoint = (Vector3)Properties["position"] + (Vector3)Properties["gazeDirection"] * actualDistance;

            //Debug.Log("gazeWorldPoint " + gazeWorldPoint);

            return gazeWorldPoint;
#else
            float relativeDepth = 0;
            Vector3 gazeWorldPoint;

            var color = GetRTColor((RenderTexture)Properties["renderDepth"], width / 2, height / 2);
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                relativeDepth = color.linear.r;

            }
            else
            {
                relativeDepth = color.r;
                //TODO gamma lighting doesn't correctly sample the greyscale depth value, but very close. not sure why
            }

            float actualDistance = Mathf.Lerp((float)Properties["nearDepth"], (float)Properties["farDepth"], relativeDepth);
            gazeWorldPoint = (Vector3)Properties["position"] + (Vector3)Properties["hmdForward"] * actualDistance;
            return gazeWorldPoint;
#endif


        }

        static Texture2D tex;
        public Color GetRTColor(RenderTexture rt, int x, int y)
        {
            if (tex == null)
            {
#if CVR_GAZETRACK
                tex = new Texture2D(Resolution, Resolution);
#else
                tex = new Texture2D(1,1);
#endif
            }

            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = rt;

#if CVR_GAZETRACK //TODO read 1 pixel from the render texture where the request point is
            tex.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0, false);
            var color = tex.GetPixel(x,y);
#else
            tex.ReadPixels(new Rect(x, y, 1, 1), 0, 0, false);
            var color = tex.GetPixel(0,0);
#endif

            RenderTexture.active = currentActiveRT;
            return color;
        }
    }
}