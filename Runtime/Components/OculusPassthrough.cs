using System.Collections.Generic;
using UnityEngine;
using System;
#if C3D_OCULUS
using OVR;
#endif
namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Oculus Passthrough")]
    public class OculusPassthrough : AnalyticsComponentBase
    {
#if C3D_OCULUS
        private OVRPassthroughLayer passthroughLayerRef;
        private bool isPassthroughEnabled;
        private float lastEventTime;

        private readonly float PassthroughSendInterval = 1;
        private float currentTime;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            GetPassthroughLayer(out passthroughLayerRef);
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()
            if (isActiveAndEnabled)
            {
                currentTime += deltaTime;

                if (currentTime > PassthroughSendInterval)
                {
                    currentTime = 0;
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
                                    {"Duration of Previous State",  Time.time - lastEventTime},
                                    {"New State", passthroughLayerRef.isActiveAndEnabled }
                                })
                                .Send();
                            lastEventTime = Time.time;
                        }

                        isPassthroughEnabled = passthroughLayerRef.isActiveAndEnabled;
                    }
                    else
                    {
                        GetPassthroughLayer(out passthroughLayerRef);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Oculus Passthrough component is disabled. Please enable in inspector.");
            }
        }

        private bool GetPassthroughLayer(out OVRPassthroughLayer layer)
        {
            layer = GameObject.FindObjectOfType<OVRPassthroughLayer>();
            if (layer != null)
            {
                return true;
            }
            layer = null;
            return false;
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }
#endif

        public override string GetDescription()
        {
#if C3D_OCULUS
            return "Records a sensor value determining if passthrough is enabled.";
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
