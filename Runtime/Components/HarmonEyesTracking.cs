using UnityEngine;

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/HarmonEyes Tracking")]
    public class HarmonEyesTracking : AnalyticsComponentBase
    {
#if C3D_HARMONEYES
        HarmonEyes.EyeTracking.Common.EyeTrackingAnalyzer subscribedAnalyzer;
        bool isPolling;

        protected override void OnSessionBegin()
        {
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded += Cognitive3D_Manager_OnLevelLoaded;
            EnsureSubscription();
        }

        void Cognitive3D_Manager_OnLevelLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode, bool didChangeSceneId)
        {
            EnsureSubscription();
        }

        void Cognitive3D_Manager_OnTick()
        {
            EnsureSubscription();
        }

        /// <summary>
        /// Reconciles subscription with whichever analyzer currently exists: subscribes when one
        /// appears, re-binds when the per-scene instance is swapped, and polls on tick while none is available
        /// </summary>
        void EnsureSubscription()
        {
            HarmonEyes.EyeTracking.Common.EyeTrackingAnalyzer current = HarmonEyes.EyeTracking.Common.AnalyzeEyeTrackingData.Instance != null
                ? HarmonEyes.EyeTracking.Common.AnalyzeEyeTrackingData.Instance.EyeTrackingAnalyzer
                : null;

            if (!ReferenceEquals(current, subscribedAnalyzer))
            {
                if (subscribedAnalyzer != null)
                {
                    RemoveHarmonEyesListeners(subscribedAnalyzer);
                    subscribedAnalyzer = null;
                }

                if (current != null)
                {
                    AddHarmonEyesListeners(current);
                    subscribedAnalyzer = current;
                }
            }

            // Keep polling only while there's nothing to subscribe to.
            SetPolling(subscribedAnalyzer == null);
        }

        void SetPolling(bool shouldPoll)
        {
            if (shouldPoll == isPolling) return;
            isPolling = shouldPoll;
            if (shouldPoll)
            {
                Cognitive3D_Manager.OnTick += Cognitive3D_Manager_OnTick;
                Util.logWarning("HarmonEyesTracking: AnalyzeEyeTrackingData is not ready. Waiting for the instance before subscribing.");
            }
            else
            {
                Cognitive3D_Manager.OnTick -= Cognitive3D_Manager_OnTick;
            }
        }

        void AddHarmonEyesListeners(HarmonEyes.EyeTracking.Common.EyeTrackingAnalyzer analyzer)
        {
            analyzer.OnAttentionResult += OnAttentionResult;
            analyzer.OnFatigueResult += OnFatigueResult;
            analyzer.OnMentalReadinessResult += OnMentalReadinessResult;
            analyzer.OnMentalWorkloadResult += OnMentalWorkloadResult;
        }

        void RemoveHarmonEyesListeners(HarmonEyes.EyeTracking.Common.EyeTrackingAnalyzer analyzer)
        {
            analyzer.OnAttentionResult -= OnAttentionResult;
            analyzer.OnFatigueResult -= OnFatigueResult;
            analyzer.OnMentalReadinessResult -= OnMentalReadinessResult;
            analyzer.OnMentalWorkloadResult -= OnMentalWorkloadResult;
        }

        void Cognitive3D_Manager_OnPreSessionEnd()
        {
            SetPolling(false);
            if (subscribedAnalyzer != null)
            {
                RemoveHarmonEyesListeners(subscribedAnalyzer);
                subscribedAnalyzer = null;
            }
            Cognitive3D_Manager.OnLevelLoaded -= Cognitive3D_Manager_OnLevelLoaded;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        void OnAttentionResult(HarmonEyes.EyeTracking.Common.AttentionData result)
        {
            if (result == null) return;
            SensorRecorder.RecordDataPoint("c3d.harmoneyes.attention_level", result.level);
            SensorRecorder.RecordDataPoint("c3d.harmoneyes.attention_score", result.composite_score);
        }

        void OnFatigueResult(HarmonEyes.EyeTracking.Common.FatigueData result)
        {
            if (result == null) return;
            SensorRecorder.RecordDataPoint("c3d.harmoneyes.fatigue_level", result.level);
        }

        void OnMentalReadinessResult(HarmonEyes.EyeTracking.Common.MentalReadinessData result)
        {
            if (result == null) return;

            SensorRecorder.RecordDataPoint("c3d.harmoneyes.mental_readiness_low_percent", result.low_percentage);
            SensorRecorder.RecordDataPoint("c3d.harmoneyes.mental_readiness_moderate_percent", result.moderate_percentage);
            SensorRecorder.RecordDataPoint("c3d.harmoneyes.mental_readiness_high_percent", result.high_percentage);
        }

        void OnMentalWorkloadResult(HarmonEyes.EyeTracking.Common.MentalWorkloadData result)
        {
            if (result == null) return;
            SensorRecorder.RecordDataPoint("c3d.harmoneyes.mental_workload_level", result.level);
        }
#endif

        public override string GetDescription()
        {
#if C3D_HARMONEYES
            return "Records HarmonEyes cognitive-state metrics (Mental Workload, Fatigue, Attention and Mental Readiness) as sensors";
#else
            return "Records HarmonEyes cognitive-state metrics (Mental Workload, Fatigue, Attention and Mental Readiness) as sensors. Requires the HarmonEyes Unity SDK in your project and the C3D_HARMONEYES scripting define symbol to be set.";
#endif
        }

        public override bool GetWarning()
        {
#if C3D_HARMONEYES
            return false;
#else
            return true;
#endif
        }
    }
}
