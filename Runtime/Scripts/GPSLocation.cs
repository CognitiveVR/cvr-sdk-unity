using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Adds the starting GPS location to session properties at the start of the session
/// </summary>

//TODO consider looping this on an interval

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/GPS Location")]
    public class GPSLocation : Cognitive3D.Components.AnalyticsComponentBase
    {
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();

            //loop until TryGetGPSLocation returns true
            StartCoroutine(GPSBegin());
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
#if XRPF
                    if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsLocationDataAllowed)
#endif
                    {
                        Cognitive3D_Manager.SetSessionProperty("c3d.geo.latitude", geo.x);
                        Cognitive3D_Manager.SetSessionProperty("c3d.geo.longitude", geo.y);
                        Cognitive3D_Manager.SetSessionProperty("c3d.geo.altitude", geo.z);
                    }    
                    break;
                }
            }
        }

        public override string GetDescription()
        {
            if (!GameplayReferences.IsGPSLocationEnabled())
            {
                return "Records GPS Location as a Session Property\nGPS Location is not enabled\nAdd C3D_LOCATION to scripting define symbols in Project Settings";
            }
            else
            {
                return "Records GPS Location as a Session Property";
            }
        }

        public override bool GetWarning()
        {
            return !GameplayReferences.IsGPSLocationEnabled();
        }
    }
}
