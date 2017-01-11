using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CognitiveVR.Plugins
{

    public class SensorSnapshot
    {
        //public string category;
        public double timestamp;
        public double sensorValue;

        public SensorSnapshot(double time, double value)
        {
            //category = cat;
            timestamp = time;
            sensorValue = value;
        }
    }

    /// <summary>
    /// This CognitiveVR plugin provides a simple interface for instrumenting purchase flows in an application.
    /// </summary>
    public static class Sensor
	{
        //public static Dictionary<string, float[,]> CachedSnapshots = new Dictionary<string, float[,]>();
        public static Dictionary<string,List<SensorSnapshot>> CachedSnapshots = new Dictionary<string, List<SensorSnapshot>>();

        public static void RecordDataPoint(string category, float value)
        {
            if (CachedSnapshots.ContainsKey(category))
            {
                CachedSnapshots[category].Add(new SensorSnapshot(Util.Timestamp(), value));
            }
            else
            {
                CachedSnapshots.Add(category,new List<SensorSnapshot>());
                CachedSnapshots[category].Add(new SensorSnapshot(Util.Timestamp(), value));
            }
        }

        public static void GenerateSensorData()
        {
            for (int i = 0; i<5; i++)
            {
                System.Random r = new System.Random();
                RecordDataPoint("temperature",(float)r.NextDouble());
            }

            
            //hearbeat data
            for (int i = 0; i < 5; i++)
            {
                System.Random r = new System.Random();
                RecordDataPoint("heartbeat", (float)r.NextDouble());
            }

            //twitchiness data
            for (int i = 0; i < 5; i++)
            {
                System.Random r = new System.Random();
                RecordDataPoint("twitchiness", (float)r.NextDouble());
            }
        }

        public static string SerializeSensorData()
        {
            //external.minijson.json.serialize works fine for lists, not objects
            //return External.MiniJSON.Json.Serialize(sensors);

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"userid\": \"");
            sb.Append(Core.userId);
            sb.Append("\",\"sessionid\": \"");
            sb.Append(CognitiveVR_Preferences.SessionID);
            sb.Append("\",\"timestamp\": \"");
            sb.Append(CognitiveVR_Preferences.TimeStamp);
            sb.Append("\",\"data\":[");
            
            foreach (var kvp in CachedSnapshots)
            {
                AppendSensorData(kvp,sb);
            }

            sb.Remove(sb.Length - 1, 1); //remove the last comma
            sb.Append("]}");
            
            return sb.ToString();
        }

        static void AppendSensorData(KeyValuePair<string,List<SensorSnapshot>> kvp, StringBuilder sb)
        {
            sb.Append("{\"name\":\"");
            sb.Append(kvp.Key);
            sb.Append("\",\"data\":[");
            
            //loop through list
            for (int i = 0; i<kvp.Value.Count; i++)
            {
                sb.Append("[");
                sb.Append(kvp.Value[i].timestamp);
                sb.Append(",");
                sb.Append(kvp.Value[i].sensorValue);
                sb.Append("],");
            }

            sb.Remove(sb.Length - 1, 1); //remove the last comma
            sb.Append("]},");
        }
    }
}

