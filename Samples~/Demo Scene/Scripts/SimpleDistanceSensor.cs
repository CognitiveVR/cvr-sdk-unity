using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//every second, records a data point of the distance from a source to a target

namespace Cognitive3D.Demo
{
    public class SimpleDistanceSensor : MonoBehaviour
    {
		[SerializeField]
        private Transform target;
		[SerializeField]
        private Transform source;
        YieldInstruction waitForOneSecond;

        private void Start()
        {
            if (target == null || source == null) { return; }

            waitForOneSecond = new WaitForSeconds(1);
            StartCoroutine(SecondTick());
        }

        IEnumerator SecondTick()
        {
            while(Application.isPlaying)
            {
                yield return waitForOneSecond;
                if (target == null || source == null) { break; }

                //find the distance between the two transforms and record that value
                float distance = Vector3.Distance(source.position, target.position);
                Cognitive3D.SensorRecorder.RecordDataPoint("Distance", distance);
            }
        }
    }
}