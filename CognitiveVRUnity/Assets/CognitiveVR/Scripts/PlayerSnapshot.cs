using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// container for cached player tracking data points
/// </summary>

namespace CognitiveVR
{
    public class PlayerSnapshot
    {
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
            float relativeDepth;

            Vector3 GazeWorldPoint;
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                relativeDepth = texTemp.GetPixel(texTemp.width / 2, texTemp.height / 2).linear.r;
            else
            {
                //TODO fix depth samples on gamma lighting. gamma lighting doesn't correctly sample the greyscale depth value, but very close. not sure why
                relativeDepth = texTemp.GetPixel(texTemp.width / 2, texTemp.height / 2).r;
            }

            float actualDistance = Mathf.Lerp((float)Properties["nearDepth"], (float)Properties["farDepth"], relativeDepth);
            GazeWorldPoint = (Vector3)Properties["position"] + (Vector3)Properties["gazeDirection"] * actualDistance;
            return GazeWorldPoint;
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