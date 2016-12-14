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

            //======= gaze (farclip distance)
            //Debug.DrawRay(position, gazeDir * far, Color.red, 0.1f);
            //Debug.DrawRay(position, camForward * far, new Color(0.5f, 0, 0), 0.1f);

            //dot product to find projection of gaze with direction
            float fwdAmount = Vector3.Dot(gazeDir.normalized * far, camForward.normalized);
            Vector3 fwdPoint = camForward * fwdAmount;
            //Debug.DrawRay(fwdPoint, Vector3.up, Color.cyan, 0.1f);
            //Debug.DrawRay(gazeDir * far, Vector3.up, Color.yellow, 0.1f);


            //======= angle towards farPlane
            //get angle between center and gaze direction. cos(A) = b/c
            float gazeRads = Mathf.Acos(fwdAmount / far);
            float dist = far * Mathf.Tan(gazeRads);

            float hypotenuseDist = Mathf.Sqrt(Mathf.Pow(dist, 2) + Mathf.Pow(far, 2));

            //Debug.DrawRay(position + gazeDir * far, gazeDir * missingDist, Color.green, 0.1f); //appended distance to hit farPlane
            return hypotenuseDist;
        }

        public Vector3 GetGazePoint(Texture2D texTemp)
        {
            texTemp = GetRTPixels((RenderTexture)Properties["renderDepth"]);
            float relativeDepth = 0;
            Vector3 gazeWorldPoint;

#if CVR_FOVE

            //range between 0 and 1.0
            Vector2 snapshotPixel = (Vector2)Properties["hmdGazePoint"] * 0.5f + Vector2.one * 0.5f;

            snapshotPixel *= Resolution;

            snapshotPixel.x = Mathf.Clamp(snapshotPixel.x, 0, Resolution);
            snapshotPixel.y = Mathf.Clamp(snapshotPixel.y, 0, Resolution);

            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                relativeDepth = texTemp.GetPixel((int)snapshotPixel.x, (int)snapshotPixel.y).linear.r;
            }
            else
            {
                //TODO gamma lighting doesn't correctly sample the greyscale depth value, but very close. not sure why
                relativeDepth = texTemp.GetPixel((int)snapshotPixel.x, (int)snapshotPixel.y).r;
            }

            float actualDepth = GetAdjustedDistance((float)Properties["farDepth"], (Vector3)Properties["convergence"], (Vector3)Properties["gazeDirection"]);

            float actualDistance = Mathf.Lerp((float)Properties["nearDepth"], actualDepth, relativeDepth);

            gazeWorldPoint = (Vector3)Properties["position"] + (Vector3)Properties["convergence"] * actualDistance;
            return gazeWorldPoint;
#else
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                relativeDepth = texTemp.GetPixel(texTemp.width / 2, texTemp.height / 2).linear.r;
            else
            {
                //TODO gamma lighting doesn't correctly sample the greyscale depth value, but very close. not sure why
                relativeDepth = texTemp.GetPixel(texTemp.width / 2, texTemp.height / 2).r;
            }

            float actualDistance = Mathf.Lerp((float)Properties["nearDepth"], (float)Properties["farDepth"], relativeDepth);
            gazeWorldPoint = (Vector3)Properties["position"] + (Vector3)Properties["gazeDirection"] * actualDistance;
            return gazeWorldPoint;
#endif

        }

        public Texture2D GetRTPixels(RenderTexture rt)
        {
            Texture2D tex = new Texture2D(rt.width, rt.height);

            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = rt;

            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            RenderTexture.active = currentActiveRT;
            return tex;
        }
    }
}