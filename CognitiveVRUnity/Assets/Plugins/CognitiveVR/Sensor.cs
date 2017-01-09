using System.Collections;
using System.Collections.Generic;

namespace CognitiveVR.Json
{
    public class SensorData
    {
        public string userid;
        public string sessionid;
        public double timestamp;
        public List<SensorType> sensorType;

        public string Serialize()
        {
            return External.MiniJSON.Json.Serialize(this);
        }
    }

    public class SensorType
    {
        public string name;
        public SensorDataPoint[] data;
    }

    public class SensorDataPoint
    {
        public float time;
        public float value;
    }
}

namespace CognitiveVR.Plugins
{


    /// <summary>
    /// This CognitiveVR plugin provides a simple interface for instrumenting purchase flows in an application.
    /// </summary>
    public class Sensor
	{
        public Json.SensorData sensors;
        public void GenerateSensorData()
        {
            sensors.userid = "somerandomuserid1234565432";
            sensors.sessionid = "sessionid675894857653";
            sensors.timestamp = 124573.21467324;

            sensors.sensorType = new List<Json.SensorType>();

            sensors.sensorType.Add(new Json.SensorType() { name = "temperature", data = new Json.SensorDataPoint[5] });
            //temperature data
            for (int i = 0; i<5; i++)
            {
                System.Random r = new System.Random();
                sensors.sensorType[0].data[i] = new Json.SensorDataPoint() { time = i, value = (float)r.NextDouble() };
            }

            sensors.sensorType.Add(new Json.SensorType() { name = "heartbeat", data = new Json.SensorDataPoint[5] });
            //hearbeat data
            for (int i = 0; i < 5; i++)
            {
                System.Random r = new System.Random();
                sensors.sensorType[1].data[i] = new Json.SensorDataPoint() { time = i, value = (float)r.NextDouble() };
            }

            sensors.sensorType.Add(new Json.SensorType() { name = "twitchiness", data = new Json.SensorDataPoint[5] });
            //twitchiness data
            for (int i = 0; i < 5; i++)
            {
                System.Random r = new System.Random();
                sensors.sensorType[2].data[i] = new Json.SensorDataPoint() { time = i, value = (float)r.NextDouble() };
            }
        }

        public string SerializeSensorData()
        {
            return External.MiniJSON.Json.Serialize(sensors);
        }
    }
}

