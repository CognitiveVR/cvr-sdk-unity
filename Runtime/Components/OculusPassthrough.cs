using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if C3D_OCULUS
using OVR;
#endif
namespace Cognitive3D.Components
{
    public class OculusPassthrough : AnalyticsComponentBase
    {
#if C3D_OCULUS
        private OVRPassthroughLayer passthroughLayerRef;
        private bool isPassthroughEnabled;
        private float lastEventTime;
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            GetPassthroughLayer(out passthroughLayerRef);
        }

        // Update is called once per frame
        void Update()
        {
            if (passthroughLayerRef != null)
            {
                // Sending Sensor Value
                // Converting the bool to int this way. Can use ternary operator, but this is much clearer
                SensorRecorder.RecordDataPoint("c3d.app.passthroughEnabled", Convert.ToUInt32(passthroughLayerRef.isActiveAndEnabled));

                // Send event on state change
                if (isPassthroughEnabled != passthroughLayerRef.isActiveAndEnabled)
                {
                    new CustomEvent("Passthrough Layer Changed")
                        .SetProperties(new Dictionary<string, object>
                        {
                            {"New State", passthroughLayerRef.isActiveAndEnabled },
                            {"Duration",  Time.time - lastEventTime}
                        }).Send();
                    lastEventTime = Time.time; 
                }

                isPassthroughEnabled = passthroughLayerRef.isActiveAndEnabled;
            }
            else
            {
                GetPassthroughLayer(out passthroughLayerRef);
            }
        }

        private bool GetPassthroughLayer(out OVRPassthroughLayer layer)
        {
            if (GameObject.FindObjectOfType<OVRPassthroughLayer>() != null)
            {
                layer = GameObject.FindObjectOfType<OVRPassthroughLayer>();
                return true;
            }
            layer = null;
            return false;
        }
#endif

        public override string GetDescription()
        {
#if C3D_OCULUS
            return "Records a sensor value determining if passthrough is enabled. 0 means no, 1 means yes.";
#else
            return "Oculus Passthrough properties can only be accessed when using the Oculus Platform";
#endif
        }


        public override bool GetWarning()
        {
#if C3D_OCULUS
            return false;
#else
            return true;
#endif
        }
    }
}
