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
        while (true)
        {
            yield return new WaitForSeconds(0.05f);
            SensorRecorder.RecordDataPoint("temperature", Mathf.Sin(Time.time*Mathf.PI) + 35 + Random.Range(-0.2f,0.2f));
            SensorRecorder.RecordDataPoint("heartrate", Mathf.Sin(Time.time) * 10 + 70);
            SensorRecorder.RecordDataPoint("twitchiness", Random.Range(1, 100f));
        }
        //this should be sent when player gaze is sent
    }
}
