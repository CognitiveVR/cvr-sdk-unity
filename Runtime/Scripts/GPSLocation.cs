using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Adds the starting GPS location to session properties at the start of the session
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/GPS Location")]
    public class GPSLocation : Cognitive3D.Components.AnalyticsComponentBase
    {
        //YieldInstruction GPSUpdateInverval;
        //public float Interval = 10;
        //public bool UpdateOnInterval = true;

        public override void Cognitive3D_Init()
        {
            base.Cognitive3D_Init();

            //loop until TryGetGPSLocation returns true
            StartCoroutine(GPSBegin());

            //if enabled, loop on interval
            //if (UpdateOnInterval)
            //{
            //    GPSUpdateInverval = new WaitForSeconds(Interval);
            //    StartCoroutine(GPSTick());
            //}
        }

        IEnumerator GPSBegin()
        {
            YieldInstruction wait = new WaitForSeconds(1);
            Vector4 geo = new Vector4();
            while (Cognitive3D_Manager.IsInitialized)
            {
                yield return wait;
                if (GameplayReferences.TryGetGPSLocation(ref geo))
                {
                    Cognitive3D_Manager.SetSessionProperty("c3d.geo.latitude", geo.x);
                    Cognitive3D_Manager.SetSessionProperty("c3d.geo.longitude", geo.y);
                    Cognitive3D_Manager.SetSessionProperty("c3d.geo.altitude", geo.z);
                    break;
                }
            }
        }

        //IEnumerator GPSTick()
        //{
        //    while (Cognitive3D_Manager.IsInitialized)
        //    {
        //        yield return GPSUpdateInverval;
        //        //write to sensors?
        //    }
        //}

        public override string GetDescription()
        {
            return "Records GPS Location as a Session Property";
        }

        public override bool GetWarning()
        {
#if UNITY_ANDROID || CVR_ARKIT || CVR_ARCORE || UNITY_IOS
            return true;
#else
            return false;
#endif
        }
    }
}
