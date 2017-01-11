using UnityEngine;
using System.Collections;
using CognitiveVR;

public class SensorTest : MonoBehaviour {

	void Start ()
    {
        StartCoroutine(OneSecondLoop());
	}

    IEnumerator OneSecondLoop()
    {
        int i = 0;
        while (i < 5)
        {
            i++;
            yield return new WaitForSeconds(1f);
            CognitiveVR.Plugins.Sensor.RecordDataPoint("temperature", Random.Range(35f, 37f));
            CognitiveVR.Plugins.Sensor.RecordDataPoint("heatrate", Random.Range(60f, 80f));
            CognitiveVR.Plugins.Sensor.RecordDataPoint("twitchiness", Random.Range(1, 100f));
        }

        string json = CognitiveVR.Plugins.Sensor.SerializeSensorData();
        Debug.Log(json);


    }
}
