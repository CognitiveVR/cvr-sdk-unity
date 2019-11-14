using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;

//rendering is controlled by sensorcanvas
//manually rendering allows for no cost while 10 frame delay in sensorcanvas is active

namespace CognitiveVR.ActiveSession
{
    public class SensorRenderCamera : MonoBehaviour
    {
        SensorCanvas sensorCanvas;
        public int Mask = 64;

        public void Initialize(SensorCanvas canvas)
        {
            sensorCanvas = canvas;
            Camera = GetComponent<Camera>();
            Camera.enabled = false;
            Camera.cullingMask = Mask;
        }

        public Camera Camera { get; private set; }
        public float LineWidth = 0.03f;

        Color ColorWhite = Color.white;

        void OnPostRender()
        {
            double sessionTimestamp = Core.SessionTimeStamp;
            double sessionTimeSec = (Util.Timestamp(Time.frameCount) - Core.SessionTimeStamp);

            GL.PushMatrix();
            GL.LoadOrtho();
            for (int i = 0; i < sensorCanvas.SensorEntries.Length; i++)
            {
                if (!sensorCanvas.SensorEntries[i].active) { continue; }
                int sensorDataPointCount = sensorCanvas.SensorEntries[i].TimesValues.Count;
                if (sensorDataPointCount == 0) { continue; }

                GL.Begin(GL.QUADS);
                GL.Color(ColorWhite);

                sensorCanvas.SensorEntries[i].Material.SetPass(0);

                float semimax = 0.98f;
                float semimin = 0.02f;

                Vector3 dir;
                Vector3 normal = new Vector3();

                float minXValue = 1; //1 at session start, 0 at 30 seconds. after 30 seconds, don't bother clamping to range
                minXValue = Mathf.LerpUnclamped(1, 0, (float)sessionTimeSec / sensorCanvas.MaxSensorTimeSpan);
                
                SensorCanvas.SensorDataPoint previousSdp = sensorCanvas.SensorEntries[i].TimesValues[0];
                SensorCanvas.SensorDataPoint sdp;
                for (int j = 1; j < sensorDataPointCount; j++)
                {
                    //normalize this y value between min and max
                    //remap
                    float y1 = semimin + (previousSdp.Value - sensorCanvas.SensorEntries[i].minValue) * (semimax - semimin) / (sensorCanvas.SensorEntries[i].maxValue - sensorCanvas.SensorEntries[i].minValue);
                    float sessionTimeDataPointSec = (float)(previousSdp.Time - sessionTimestamp);
                    float x1 = minXValue + sessionTimeDataPointSec * (1 - minXValue) / (float)sessionTimeSec;


                    sdp = sensorCanvas.SensorEntries[i].TimesValues[j]; //List get item
                    //remap 
                    float y2 = semimin + (sdp.Value - sensorCanvas.SensorEntries[i].minValue) * (semimax - semimin) / (sensorCanvas.SensorEntries[i].maxValue - sensorCanvas.SensorEntries[i].minValue);
                    float sessionTimeDataPointSec2 = (float)(sdp.Time - sessionTimestamp);
                    float x2 = minXValue + sessionTimeDataPointSec2 * (1 - minXValue) / (float)sessionTimeSec;

                    dir.x = x2 - x1;
                    dir.y = y2 - y1;
                    //inline normalize. avoids vector constructor and vector division
                    {
                        normal.x = -dir.y;
                        normal.y = dir.x;
                        float mag = Mathf.Sqrt(normal.x * normal.x + normal.y * normal.y);
                        normal.x = normal.x / mag;
                        normal.y = normal.y / mag;
                    }

                    GL.Vertex3(x1 - normal.x * LineWidth, y1 - normal.y * LineWidth, 0);
                    GL.Vertex3(x1 + normal.x * LineWidth, y1 + normal.y * LineWidth, 0);
                    GL.Vertex3(x2 + normal.x * LineWidth, y2 + normal.y * LineWidth, 0);
                    GL.Vertex3(x2 - normal.x * LineWidth, y2 - normal.y * LineWidth, 0);
                    previousSdp = sdp;

                }
                GL.End();
            }
            GL.PopMatrix();
        }
    }
}