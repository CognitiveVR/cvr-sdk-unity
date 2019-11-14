using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CognitiveVR.ActiveSession
{
    public class SensorCanvas : MonoBehaviour
    {
        public struct SensorDataPoint
        {
            public double Time;
            public float Value;
            public SensorDataPoint(double time, float value)
            {
                Time = time;
                Value = value;
            }
        }

        [System.Serializable]
        public class SensorEntry
        {
            public bool active = false;
            public string name;
            public Text Name;
            public Image ColourSwatch;
            public Text MinValue;
            public Text MaxValue;
            public Material Material;
            public List<SensorDataPoint> TimesValues = new List<SensorDataPoint>();
            public float minValue = 0;
            public float maxValue = 0;
        }

        public SensorEntry[] SensorEntries;
        public SensorRenderCamera renderCamera;


        public float MaxSensorTimeSpan = 120;

        System.Reflection.FieldInfo canvasHackField;
        object canvasHackObject;
        float RenderDelayFrameCount = 0;

        void Start()
        {
            renderCamera.Initialize(this);
            SensorRecorder.OnNewSensorRecorded += SensorRecorder_OnNewSensorRecorded;
            for (int i = 0; i < SensorEntries.Length; i++)
            {
                SensorEntries[i].active = false;
                SensorEntries[i].name = "";
                SensorEntries[i].ColourSwatch.enabled = false;
                SensorEntries[i].Name.enabled = false;
                SensorEntries[i].Name.text = "";
                SensorEntries[i].MaxValue.enabled = false;
                SensorEntries[i].MinValue.enabled = false;
            }
            canvasHackField = typeof(Canvas).GetField("willRenderCanvases", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            canvasHackObject = canvasHackField.GetValue(null);
        }

        private void SensorRecorder_OnNewSensorRecorded(string sensorName, float value)
        {
            for (int i = 0; i < SensorEntries.Length; i++)
            {
                if (SensorEntries[i].name == sensorName)
                {
                    break;
                }
                if (SensorEntries[i].active) { continue; }
                SensorEntries[i].active = true;
                SensorEntries[i].name = sensorName;
                SensorEntries[i].ColourSwatch.enabled = true;
                SensorEntries[i].Name.enabled = true;
                SensorEntries[i].Name.text = sensorName;
                SensorEntries[i].MaxValue.enabled = true;
                SensorEntries[i].MinValue.enabled = true;
                SensorEntries[i].MinValue.text = value.ToString();
                SensorEntries[i].MaxValue.text = value.ToString();
                break;
            }
        }

        void Update()
        {
            RenderDelayFrameCount += 1;
            if (RenderDelayFrameCount < 10) { return; }
            RenderDelayFrameCount = 0;

            foreach (var sensor in SensorRecorder.LastSensorValues)
            {
                for (int i = 0; i < SensorEntries.Length; i++)
                {
                    if (SensorEntries[i].active == false) { continue; }
                    if (sensor.Key != SensorEntries[i].name) { continue; }
                    SensorEntries[i].TimesValues.Add(new SensorDataPoint(Util.Timestamp(Time.frameCount), sensor.Value));
                    if (SensorEntries[i].maxValue < sensor.Value)
                    {
                        SensorEntries[i].maxValue = sensor.Value;
                        SensorEntries[i].MaxValue.text = SensorEntries[i].maxValue.ToString("0.000");
                    }
                    if (SensorEntries[i].minValue > sensor.Value)
                    {
                        SensorEntries[i].minValue = sensor.Value;
                        SensorEntries[i].MinValue.text = SensorEntries[i].minValue.ToString("0.000");
                    }
                }
            }
                
            double staleTimestamp = Util.Timestamp(Time.frameCount) - MaxSensorTimeSpan;

            for (int i = 0; i < SensorEntries.Length; i++)
            {
                while (SensorEntries[i].TimesValues.Count > 0)
                {
                    if (SensorEntries[i].TimesValues[0].Time < staleTimestamp)
                    {
                        SensorEntries[i].TimesValues.RemoveAt(0);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            //disabling then enabling canvas rendering is faster than just leaving it on
            canvasHackField.SetValue(null, null);
            renderCamera.Camera.Render();
            canvasHackField.SetValue(null, canvasHackObject);
        }
            
        private void OnDestroy()
        {
            SensorRecorder.OnNewSensorRecorded -= SensorRecorder_OnNewSensorRecorded;
        }
    }
}