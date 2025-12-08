using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal static class FeatureLibrary
    {
        internal enum FeatureActionType
        {
            Apply,
            Remove,
            Upload,
            LinkTo,
            Settings
        }

        internal static int projectID;
        private static int userID;

        internal static List<FeatureData> features = new List<FeatureData>();
        internal delegate void refreshFeatureStates();
        /// <summary>
        /// Called just after a session has begun
        /// </summary>
        internal static event refreshFeatureStates RefreshFeatureStates;
        private static void InvokeRefreshFeatureStatesEvent() { if (RefreshFeatureStates != null) { RefreshFeatureStates.Invoke(); } }

        internal static List<FeatureData> CreateFeatures(System.Action<int> setFeatureIndex)
        {
            if (!string.IsNullOrEmpty(EditorCore.DeveloperKey))
            {
                EditorCore.GetUserData(EditorCore.DeveloperKey, GetUserResponse);
                EditorCore.CheckForExpiredDeveloperKey(EditorCore.DeveloperKey, GetDevKeyResponse);
            }

            return features = new List<FeatureData>
            {
                new FeatureData(
                    false,
                    "Dynamic Objects",
                    "Manage and track specific Dynamic Objects in the current scene",
                    EditorCore.DynamicsIcon,
                    () =>
                    {
                        setFeatureIndex(0);
                        SegmentAnalytics.TrackEvent("DynamicObjectsWindow_Opened", "DynamicObjectsWindow", "new");
                    },
                    new List<FeatureAction>
                    {
                        new FeatureAction(
                            FeatureActionType.LinkTo,
                            "Link to Dynamic Object documentation",
                            () =>
                            {
                                Application.OpenURL("https://docs.cognitive3d.com/unity/dynamic-objects/");
                            }
                        )
                    },
                    new DynamicObjectDetailGUI()
                ),
                new FeatureData(
                    false,
                    "ExitPoll Survey",
                    "Set up ExitPoll surveys to collect and view user feedback",
                    EditorCore.ExitpollIcon,
                    () =>
                    {
                        setFeatureIndex(1);
                        SegmentAnalytics.TrackEvent("ExitPollWindow_Opened", "ExitPollWindow", "new");
                    },
                    new List<FeatureAction>
                    {
                        new FeatureAction(
                            FeatureActionType.LinkTo,
                            "Link to ExitPoll Survey documentation",
                            () =>
                            {
                                Application.OpenURL("https://docs.cognitive3d.com/unity/exitpoll/");
                            }
                        )
                    },
                    new ExitpollDetailGUI()
                ),
                new FeatureData(
                    false,
                    "Remote Controls",
                    "Set up variables to customize app behavior for different users",
                    EditorCore.RemoteControlsIcon,
                    () =>
                    {
                        setFeatureIndex(2);
                        SegmentAnalytics.TrackEvent("RemoteControlsWindow_Opened", "RemoteControlsWindow", "new");
                    },
                    new List<FeatureAction>
                    {
                        new FeatureAction(
                            FeatureActionType.LinkTo,
                            "Link to Remote Controls documentation",
                            () =>
                            {
                                Application.OpenURL("https://docs.cognitive3d.com/unity/remote-controls/");
                            }
                        )
                    },
                    new RemoteControlsDetailGUI()
                ),
                new FeatureData(
                    false,
                    "Social Platform",
                    "Set up a Social Platform to capture user and app identity data",
                    EditorCore.SocialPlatformIcon,
                    () =>
                    {
                        setFeatureIndex(3);
                        SegmentAnalytics.TrackEvent("SocialPlatformWindow_Opened", "SocialPlatformWindow", "new");
                    },
                    new List<FeatureAction>
                    {
                        new FeatureAction(
                            FeatureActionType.LinkTo,
                            "Link to Social Platform documentation",
                            () =>
                            {
                                Application.OpenURL("https://docs.cognitive3d.com/unity/components/#social-platform");
                            }
                        )
                    },
                    new SocialPlatformDetailGUI()
                ),
                new FeatureData(
                    false,
                    "Custom Events",
                    "API reference and examples for recording custom events",
                    EditorCore.CustomEventIcon,
                    () =>
                    {
                        setFeatureIndex(4);
                        SegmentAnalytics.TrackEvent("CustomEventsWindow_Opened", "CustomEventsWindow", "new");
                    },
                    new List<FeatureAction>
                    {
                        new FeatureAction(
                            FeatureActionType.LinkTo,
                            "Link to Custom Events documentation",
                            () =>
                            {
                                Application.OpenURL("https://docs.cognitive3d.com/unity/customevents/");
                            }
                        )
                    },
                    new CustomEventDetailGUI()
                ),
                new FeatureData(
                    false,
                    "Sensors",
                    "API reference and examples for recording custom sensors",
                    EditorCore.SensorIcon,
                    () =>
                    {
                        setFeatureIndex(5);
                        SegmentAnalytics.TrackEvent("SensorsWindow_Opened", "SensorsWindow", "new");
                    },
                    new List<FeatureAction>
                    {
                        new FeatureAction(
                            FeatureActionType.LinkTo,
                            "Link to Sensors documentation",
                            () =>
                            {
                                Application.OpenURL("https://docs.cognitive3d.com/unity/sensors/");
                            }
                        )
                    },
                    new SensorDetailGUI()
                ),
                new FeatureData(
                    false,
                    "Multiplayer",
                    "Set up Multiplayer to track server-client player activity and analytics",
                    EditorCore.MultiplayerIcon,
                    () =>
                    {
                        setFeatureIndex(6);
                        SegmentAnalytics.TrackEvent("MultiplayerWindow_Opened", "MultiplayerWindow", "new");
                    },
                    new List<FeatureAction>
                    {
                        new FeatureAction(
                            FeatureActionType.LinkTo,
                            "Link to Multiplayer documentation",
                            () =>
                            {
                                Application.OpenURL("https://docs.cognitive3d.com/unity/multiplayer/");
                            }
                        )
                    },
                    new MultiplayerDetailGUI()
                ),
                new FeatureData(
                    false,
                    "Media and 360 Video",
                    "Set up Media to track gaze on images and videos",
                    EditorCore.MediaIcon,
                    () =>
                    {
                        setFeatureIndex(7);
                        SegmentAnalytics.TrackEvent("Media360Window_Opened", "Media360Window", "new");
                    },
                    new List<FeatureAction>
                    {
                        new FeatureAction(
                            FeatureActionType.LinkTo,
                            "Link to Media & 360 Video documentation",
                            () =>
                            {
                                Application.OpenURL("https://docs.cognitive3d.com/unity/media/");
                            }
                        )
                    },
                    new MediaDetailGUI()
                ),
                new FeatureData(
                    false,
                    "Audio Recording",
                    "Set up Audio Recorder to capture speech from microphone or app audio",
                    EditorCore.AudioRecordingIcon,
                    () =>
                    {
                        setFeatureIndex(8);
                        SegmentAnalytics.TrackEvent("AudioRecordingWindow_Opened", "AudioRecordingWindow", "new");
                    },
                    new List<FeatureAction>
                    {
                        new FeatureAction(
                            FeatureActionType.LinkTo,
                            "Link to Audio Recording documentation",
                            () =>
                            {
                                Application.OpenURL("https://docs.cognitive3d.com/unity/");
                            }
                        )
                    },
                    new AudioRecordingDetailGUI()
                ),
            };
        }

        internal static void UpdateAllFeatureAvailability(bool isEnabled)
        {
            if (features.Count <= 0) return;

            foreach (var feature in features)
            {
                feature.isEnabled = isEnabled;
            }

            InvokeRefreshFeatureStatesEvent();
        }

        internal static void UpdateFeatureAvailability(string featureName, bool isEnabled)
        {
            if (features.Count <= 0) return;

            foreach (var feature in features)
            {
                if (feature.Title.Contains(featureName))
                {
                    feature.isEnabled = isEnabled;
                    InvokeRefreshFeatureStatesEvent();
                    break;
                }
            }
        }

        internal static void AddOrRemoveComponent<T>() where T : Component
        {
            GameObject c3dPrefab = EditorCore.GetCognitive3DManagerPrefab();

            if (c3dPrefab == null)
            {
                Debug.LogError("Cognitive3D Manager prefab not found in Resources folder!");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(c3dPrefab);

            GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);

            if (prefabContents.GetComponent<T>() != null)
            {
                UnityEngine.Object.DestroyImmediate(prefabContents.GetComponent<T>());
            }
            else
            {
                prefabContents.AddComponent<T>();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);

            AssetDatabase.Refresh();
        }

        internal static bool TryGetComponent<T>() where T : Component
        {
            GameObject c3dPrefab = EditorCore.GetCognitive3DManagerPrefab();

            if (c3dPrefab == null)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(c3dPrefab);

            GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);

            bool hasComponent = prefabContents.GetComponent<T>() != null;

            PrefabUtility.UnloadPrefabContents(prefabContents);

            return hasComponent;
        }

#region Callback Responses
        private static void GetUserResponse(int responseCode, string error, string text)
        {
            var userdata = JsonUtility.FromJson<EditorCore.UserData>(text);
            if (responseCode != 200)
            {
                Util.logDevelopment("Failed to retrieve user data" + responseCode + "  " + error);
            }

            if (responseCode == 200 && userdata != null)
            {
                userID = userdata.userId;
                projectID = userdata.projectId;
            }
        }

        static void GetDevKeyResponse(int responseCode, string error, string text)
        {
            if (responseCode == 200)
            {
                //dev key is fine
                UpdateAllFeatureAvailability(true);
                EditorCore.CheckSubscription(EditorCore.DeveloperKey, GetSubscriptionResponse);
                return;
            }

            UpdateAllFeatureAvailability(false);
            Debug.LogError("Developer Key invalid or expired. Response code: " + responseCode + " error: " + error);
        }

        private static void GetSubscriptionResponse(int responseCode, string error, string text)
        {
            UpdateFeatureAvailability("Audio Recording", false);
            if (responseCode != 200)
            {
                Debug.LogError("GetSubscriptionResponse response code: " + responseCode + " error: " + error);
                return;
            }

            // Check if response data is valid
            try
            {
                JsonUtility.FromJson<EditorCore.OrganizationData>(text);
            }
            catch
            {
                Debug.LogError("Invalid JSON response");
                return;
            }

            EditorCore.OrganizationData organizationDetails = JsonUtility.FromJson<EditorCore.OrganizationData>(text);
            if (organizationDetails != null && 
                organizationDetails.subscriptions != null &&
                organizationDetails.subscriptions.Length > 0 && 
                organizationDetails.subscriptions[0].entitlements != null &&
                organizationDetails.subscriptions[0].entitlements.can_access_session_audio)
            {
                UpdateFeatureAvailability("Audio Recording", true);
            }
        }
#endregion
    }

    internal class FeatureData
    {
        internal bool isEnabled;
        internal string Title;
        internal string Description;
        internal Texture2D Icon;
        internal System.Action OnClick;

        internal List<FeatureAction> Actions;

        internal IFeatureDetailGUI DetailGUI;

        internal FeatureData(bool isEnabled, string title, string description, Texture2D icon, System.Action onClick, List<FeatureAction> actions, IFeatureDetailGUI detailGUI = null)
        {
            this.isEnabled = isEnabled;
            Title = title;
            Description = description;
            Icon = icon;
            OnClick = onClick;
            Actions = actions ?? new List<FeatureAction>();
            DetailGUI = detailGUI;
        }
    }

    internal class FeatureAction
    {
        internal FeatureLibrary.FeatureActionType Type;
        internal string Tooltip;
        internal System.Action OnClick;

        internal FeatureAction(FeatureLibrary.FeatureActionType type, string tooltip, System.Action onClick)
        {
            Type = type;
            Tooltip = tooltip;
            OnClick = onClick;
        }
    }
}
