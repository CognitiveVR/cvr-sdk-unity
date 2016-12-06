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

        public Vector3 GetGazePoint(Texture2D texTemp)
        {
            texTemp = GetRTPixels((RenderTexture)Properties["renderDepth"]);
            float relativeDepth = 0;

            Vector3 GazeWorldPoint;

#if CVR_FOVE

            //TODO project vector3 onto vector2

            //Vector3 convergence = (Vector3)Properties["convergence"];
            //Vector3 directionToConvergencePoint = convergence - (Vector3)Properties["position"];
            //directionToConvergencePoint.Normalize();

            Vector2 foveGazePoint = (Vector2)Properties["fovepoint"] * 0.5f + Vector2.one * 0.5f;
            //range between -1.0 and 1.0. SHOULD CLAMP THIS 1.1 has been seen

            foveGazePoint *= Resolution;

            foveGazePoint.x = Mathf.Clamp(foveGazePoint.x, 0, Resolution);
            foveGazePoint.y = Mathf.Clamp(foveGazePoint.y, 0, Resolution);

            //Debug.Log("foveGazePoint " + foveGazePoint);

            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                relativeDepth = texTemp.GetPixel((int)foveGazePoint.x, (int)foveGazePoint.y).linear.r;
            else
            {
                //TODO gamma lighting doesn't correctly sample the greyscale depth value, but very close. not sure why
                relativeDepth = texTemp.GetPixel((int)foveGazePoint.x, (int)foveGazePoint.y).r;
            }

#if CVR_DEBUG
            //texTemp.SetPixel((int)foveGazePoint.x, (int)foveGazePoint.y, Color.red);
            //System.IO.File.WriteAllBytes(System.Guid.NewGuid().ToString() + ".png", texTemp.EncodeToJPG(100));
#endif


            float actualDistance = Mathf.Lerp((float)Properties["nearDepth"], (float)Properties["farDepth"], relativeDepth);
            GazeWorldPoint = (Vector3)Properties["position"] + (Vector3)Properties["convergence"] * actualDistance;
            return GazeWorldPoint;
#else
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                relativeDepth = texTemp.GetPixel(texTemp.width / 2, texTemp.height / 2).linear.r;
            else
            {
                //TODO gamma lighting doesn't correctly sample the greyscale depth value, but very close. not sure why
                relativeDepth = texTemp.GetPixel(texTemp.width / 2, texTemp.height / 2).r;
            }

            float actualDistance = Mathf.Lerp((float)Properties["nearDepth"], (float)Properties["farDepth"], relativeDepth);
            GazeWorldPoint = (Vector3)Properties["position"] + (Vector3)Properties["gazeDirection"] * actualDistance;
            return GazeWorldPoint;
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