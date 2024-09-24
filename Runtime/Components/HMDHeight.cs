using UnityEngine;
using System.Collections;
using UnityEngine.XR;

#if COGNITIVE3D_INCLUDE_COREUTILITIES
using Unity.XR.CoreUtils;
#endif

#if C3D_VIVEWAVE
using Wave.Essence;
#endif

#if COGNITIVE3D_INCLUDE_LEGACYINPUTHELPERS
using UnityEditor.XR.LegacyInputHelpers;
#endif

/// <summary>
/// samples height of a player's HMD. average is assumed to be roughly player's eye height
/// </summary>

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/HMD Height")]
    public class HMDHeight : AnalyticsComponentBase
    {
        private readonly int SampleCount = 50;
        private readonly float StartDelay = 10;
        private readonly float Interval = 1;
        private readonly float ForeheadHeight = 0.11f; //meters
        private const float SAMPLE_INTERVAL = 10;
        private float[] heights;
        private Transform trackingSpace;
#if COGNITIVE3D_INCLUDE_COREUTILITIES
        XROrigin xrOrigin;
#endif

#if C3D_VIVEWAVE
        WaveRig waveRig;

#endif

#if C3D_DEFAULT && COGNITIVE3D_INCLUDE_LEGACYINPUTHELPERS
        CameraOffset cameraOffset = null;
#endif

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            heights = new float[SampleCount];
            StartCoroutine(Tick());
        }

        IEnumerator Tick()
        {
            yield return new WaitForSeconds(StartDelay);
            YieldInstruction wait = new WaitForSeconds(Interval);

            trackingSpace = Cognitive3D_Manager.Instance.trackingSpace;

            //median
            for (int i = 0; i < SampleCount; i++)
            {
                yield return wait;
                if (TryGetHeight(out float currentheight))
                {
                    heights[i] = currentheight;
                    if (Mathf.Approximately(i % SAMPLE_INTERVAL, 0.0f))
                    {
                        RecordAndSendMedian(heights, i);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if tracking space exist and calculates HMD height according to device type
        /// </summary>
        /// <returns>A float representing the height of the HMD</returns>
        private bool TryGetHeight(out float height)
        {
            height = 0;

            if (trackingSpace == null)
            {
                Debug.LogWarning("Tracking Space not found. Unable to record HMD height.");
                return false;
            }

#if C3D_OCULUS
            // Calculates height according to camera offset relative to Floor level and rig customization
            height = GameplayReferences.HMD.position.y - OVRPlugin.GetTrackingTransformRelativePose(OVRPlugin.TrackingOrigin.FloorLevel).Position.y - trackingSpace.position.y;
#elif C3D_VIVEWAVE
            if (waveRig == null)
            {
                waveRig = FindObjectOfType<WaveRig>();
            }

            if (waveRig != null)
            {
                if (waveRig.TrackingOrigin == TrackingOriginModeFlags.Device)
                {
                    height = GameplayReferences.HMD.position.y + waveRig.CameraYOffset - trackingSpace.position.y;
                }
                else if (waveRig.TrackingOrigin == TrackingOriginModeFlags.Floor 
                    || waveRig.TrackingOrigin == TrackingOriginModeFlags.Unknown 
                    || waveRig.TrackingOrigin == TrackingOriginModeFlags.TrackingReference
                    || waveRig.TrackingOrigin == TrackingOriginModeFlags.Unbounded) // unknown and tracking gives incorrect values
                {
                    height = GameplayReferences.HMD.position.y - trackingSpace.position.y;
                }
            }

#elif C3D_DEFAULT

#if COGNITIVE3D_INCLUDE_COREUTILITIES
            if (xrOrigin == null)
            {
                xrOrigin = FindObjectOfType<XROrigin>(); 
            }  

            if (xrOrigin != null)
            {
                if (xrOrigin.CurrentTrackingOriginMode == UnityEngine.XR.TrackingOriginModeFlags.Device)
                {
                    // Calculates the height based on the customized camera offset relative to the Device and rig settings (Does not account for the user's actual physical height)
                    // TODO: Determine the user's accurate height by computing the camera offset relative to the floor level
                    height = GameplayReferences.HMD.position.y + xrOrigin.CameraYOffset - trackingSpace.position.y;
                }
                else if (xrOrigin.CurrentTrackingOriginMode == UnityEngine.XR.TrackingOriginModeFlags.Floor || xrOrigin.CurrentTrackingOriginMode == UnityEngine.XR.TrackingOriginModeFlags.Unknown)
                {
                    // Calculates height based on the camera offset relative to Floor level and rig settings
                    height = GameplayReferences.HMD.position.y - trackingSpace.position.y;
                }
            } 
#endif

#if COGNITIVE3D_INCLUDE_LEGACYINPUTHELPERS
            if (cameraOffset == null)
            {
                cameraOffset = FindObjectOfType<CameraOffset>();
            }
            
            if (cameraOffset != null)
            {
                if (cameraOffset.TrackingOriginMode == UnityEngine.XR.TrackingOriginModeFlags.Device)
                {
                    height = GameplayReferences.HMD.position.y + cameraOffset.cameraYOffset - trackingSpace.position.y;
                }
                else if (cameraOffset.TrackingOriginMode == UnityEngine.XR.TrackingOriginModeFlags.Floor || cameraOffset.TrackingOriginMode == UnityEngine.XR.TrackingOriginModeFlags.Unknown)
                {
                    height = GameplayReferences.HMD.position.y - trackingSpace.position.y;
                }
            }
#endif
#else // C3D_DEFAULT == FALSE
            height = GameplayReferences.HMD.position.y - trackingSpace.position.y;
#endif

            return true;
        }

        private void RecordAndSendMedian(float[] heights, int lastIndex)
        {
            float medianHeight = Median(heights, lastIndex);
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSpatialDataAllowed)
#endif
            {
                Cognitive3D_Manager.SetParticipantProperty("height", medianHeight * 100 + ForeheadHeight * 100);

            }
        }

        private float Median(float[] items, int lastIndex)
        {
            float[] tempArray = new float[lastIndex + 1];
            for (int j = 0; j < lastIndex; j++)
            {
                tempArray[j] = items[j];
            }
            var i = (int)Mathf.Ceil((float)(lastIndex) / 2);
            if (i >= 0)
            {
                System.Array.Sort(tempArray);
                return tempArray[i];
            }
            return 0;
        }

        public override string GetDescription()
        {
#if C3D_PICOVR
            return "Samples the height of a player's HMD. Average is assumed to be player's eye height\nPvr_UnitySDKManager.TrackingOrigin MUST be set as FloorLevel!";
#else
            return "Samples the height of a player's HMD. Average is assumed to be player's eye height";
#endif
        }

        public override bool GetWarning()
        {
#if C3D_PICOVR
            var pvrManager = GameplayReferences.Pvr_UnitySDKManager;
            if (pvrManager != null)
            {
                if (pvrManager.TrackingOrigin == Pvr_UnitySDKAPI.TrackingOrigin.EyeLevel)
                {
                    return true;
                }
            }
#endif
            return false;
        }
    }
}
