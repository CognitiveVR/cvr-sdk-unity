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

        //4, 2 or 1. for 'shotgun' approach to capturing points
        public static int PixelSamples = 1;

        public PlayerSnapshot()
        {
            System.TimeSpan span = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            timestamp = span.TotalSeconds;
        }

        public PlayerSnapshot(Dictionary<string, object> properties)
        {
            Properties = properties;
            System.TimeSpan span = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            timestamp = span.TotalSeconds;
        }

        public Vector3 GetGazePoint(Texture2D texTemp)
        {
            texTemp = GetRTPixels((RenderTexture)Properties["renderDepth"]);
            float relativeDepth;

            if (PixelSamples == 1)
            {
                Vector3 GazeWorldPoint;
                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                    relativeDepth = texTemp.GetPixel(texTemp.width / 2, texTemp.height / 2).linear.r;
                else
                    relativeDepth = texTemp.GetPixel(texTemp.width / 2, texTemp.height / 2).r;
                float actualDistance = Mathf.Lerp((float)Properties["nearDepth"], (float)Properties["farDepth"], relativeDepth);
                GazeWorldPoint = (Vector3)Properties["position"] + (Vector3)Properties["gazeDirection"] * actualDistance;
                //Properties.Add("gazePoint", GazeWorldPoint);
                return GazeWorldPoint;
            }
            else
            {
                return Vector3.zero;
                /*
                Vector3[] GazeWorldPoints = new Vector3[PixelSamples * PixelSamples];

                int index = 0;
                for (int x = 0; x < texTemp.width; x += texTemp.width / PixelSamples)
                {
                    for (int y = 0; y < texTemp.height; y += texTemp.height / PixelSamples)
                    {
                        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                            relativeDepth = texTemp.GetPixel(x, y).linear.r;
                        else
                            relativeDepth = texTemp.GetPixel(x, y).r;


                        float actualDistance = Mathf.Lerp((float)Properties["nearDepth"], (float)Properties["farDepth"], relativeDepth);
                        GazeWorldPoints[index] = (Vector3)Properties["position"] + ConvertFlatPointToSphere(x, y) * actualDistance;
                        //Properties.Add("gazePoint", GazeWorldPoints[index]);
                        index++;
                    }
                }
                return GazeWorldPoints;*/
            }
        }

        Vector3 ConvertFlatPointToSphere(int width, int height)
        {
            Vector3 outVector = (Vector3)Properties["gazeDirection"];
            //float cameraFOV = 60; //assume 60 atm. save camera fov

            outVector += Vector3.Lerp(Vector3.down, Vector3.up, height / 64f) * 0.5f; //something something mat.sin(60) = 0.5f
            outVector += Vector3.Lerp(Vector3.left, Vector3.right, width / 64f) * 0.5f;

            return outVector;
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

        public string ToStringLook()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            Vector3 GazeWorldPoints = (Vector3)Properties["gazePoint"];
            builder.Append(string.Concat("\"gaze\":[", GazeWorldPoints.x, ",", GazeWorldPoints.y, ",", GazeWorldPoints.z, "]"));


            /*for (int i = 0; i < PixelSamples * PixelSamples; i++)
            {
                builder.Append(string.Concat("\"point\":[", GazeWorldPoints[i].x, ",", GazeWorldPoints[i].y, ",", GazeWorldPoints[i].z, "],\n"));
            }*/
            //builder = builder.Remove(builder.Length - 2, 2);
            return builder.ToString();
        }

        public string ToStringLookDir()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            Vector3 forward = (Vector3)Properties["gazeDirection"];
            builder.Append("\"dir\":[" + forward.x + "," + forward.y + "," + forward.z + "]");
            return builder.ToString();
        }

        public string ToStringPos()
        {
            Vector3 position = (Vector3)Properties["position"];
            return string.Concat("\"pos\":[", position.x, ",", position.y, ",", position.z, "]");
        }
    }
}