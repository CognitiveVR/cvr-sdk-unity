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

            //median
            //iterate a fixed number of times so the loop always ends after SampleCount ticks regardless of tracking reliability.
            //only reliable readings advance validSampleCount and feed the median, so skipped (unreliable) samples don't leave zero-holes.
            int validSampleCount = 0;
            for (int i = 0; i < SampleCount; i++)
            {
                yield return wait;
                if (TryGetHeight(out float currentheight))
                {
                    heights[validSampleCount] = currentheight;
                    validSampleCount++;
                    if (Mathf.Approximately(validSampleCount % SAMPLE_INTERVAL, 0.0f))
                    {
                        RecordAndSendMedian(heights, validSampleCount);
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

            if (BoundaryUtil.TryGetTrackingSpaceTransform(out var trackingSpaceTransform) == false)
            {
                Debug.LogWarning("Tracking Space not found. Unable to record HMD height.");
                return false;
            }

#if C3D_OCULUS
            // Calculates height according to camera offset relative to Floor level and rig customization
            height = GameplayReferences.HMD.position.y - trackingSpaceTransform.pos.y;
            return true;
#elif C3D_VIVEWAVE
            if (waveRig == null)
            {
                waveRig = FindObjectOfType<WaveRig>();
            }

            if (waveRig == null)
            {
                // Rig not resolved yet; can't trust a height reading.
                return false;
            }

            // Device tracking origin reports the HMD relative to the headset, not the user's physical height. Skip recording.
            if (waveRig.TrackingOrigin == TrackingOriginModeFlags.Floor)
            {
                height = GameplayReferences.HMD.position.y - trackingSpaceTransform.pos.y;
                return true;
            }

            return false;

#elif C3D_DEFAULT

#if COGNITIVE3D_INCLUDE_COREUTILITIES
            if (xrOrigin == null)
            {
                xrOrigin = FindFirstObjectByType<XROrigin>();
            }

            if (xrOrigin != null)
            {
                // Device origin computes height from the configured camera offset, which does not reflect the user's actual physical height. Skip recording.
                if (xrOrigin.CurrentTrackingOriginMode == UnityEngine.XR.TrackingOriginModeFlags.Floor)
                {
                    // Calculates height based on the camera offset relative to Floor level and rig settings
                    height = GameplayReferences.HMD.position.y - trackingSpaceTransform.pos.y;
                    return true;
                }

                return false;
            }
#endif

#if COGNITIVE3D_INCLUDE_LEGACYINPUTHELPERS
            if (cameraOffset == null)
            {
                cameraOffset = FindFirstObjectByType<CameraOffset>();
            }

            if (cameraOffset != null)
            {
                if (cameraOffset.TrackingOriginMode == UnityEngine.XR.TrackingOriginModeFlags.Floor)
                {
                    height = GameplayReferences.HMD.position.y - trackingSpaceTransform.pos.y;
                    return true;
                }

                return false;
            }
#endif

            // No rig/origin resolved; can't trust a height reading.
            return false;
#else // C3D_DEFAULT == FALSE
            height = GameplayReferences.HMD.position.y - trackingSpaceTransform.pos.y;
            return true;
#endif
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
