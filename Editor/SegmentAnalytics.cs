using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cognitive3D.Newtonsoft.Json;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal static class SegmentAnalytics
    {
        private const string KEY_URL = "https://data.cognitive3d.com/segmentWriteKey";
        private const string TRACK_URL = "https://api.segment.io/v1/track";
        private const string PAGE_URL = "https://api.segment.io/v1/page";

        private static string _writeKey;
        private static string _anonymousId;
        static string _organizationName;

        static SegmentAnalytics()
        {
            if (!string.IsNullOrEmpty(EditorCore.DeveloperKey))
            {
                EditorCore.CheckSubscription(EditorCore.DeveloperKey, GetSubscriptionResponse);
            } 
            _anonymousId = System.Guid.NewGuid().ToString();
            FetchKey();
        }

        /// <summary>
        /// Method to fetch the Segment key
        /// </summary>
        private static async void FetchKey()
        {
            _writeKey = await GetKeyFromServerAsync();
        }

        private static async Task<string> GetKeyFromServerAsync()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(KEY_URL))
            {
                var operation = request.SendWebRequest();

                // Await until the request is done
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                return request.downloadHandler.text;
            }
        }

        /// <summary>
        /// Async method to send page data to Segment
        /// More info about page event: https://segment.com/docs/connections/spec/page/
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="status"></param>
        public static async void PageEvent(string pageName, string status = "")
        {
            // Create the data payload in JSON format
            string jsonPayload = UnityEngine.JsonUtility.ToJson(new SegmentTrackPayload
            {
                anonymousId = _anonymousId,
                organizationName = _organizationName,
                sdkVersion = Cognitive3D_Manager.SDK_VERSION,
                name = pageName,
                properties = new SegmentProperties
                {
                    status = status
                }
            });

            await SendTrackingDataAsync(PAGE_URL, jsonPayload);
        }

        /// <summary>
        /// Async method to send tracking data to Segment
        /// More info about track event: https://segment.com/docs/connections/spec/track/ 
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="buttonName"></param>
        public static async void TrackEvent(string eventName, string buttonName = "")
        {
            // Create the data payload in JSON format
            string jsonPayload = UnityEngine.JsonUtility.ToJson(new SegmentTrackPayload
            {
                anonymousId = _anonymousId,
                organizationName = _organizationName,
                sdkVersion = Cognitive3D_Manager.SDK_VERSION,
                @event = eventName,
                properties = new SegmentProperties
                {
                    buttonName = buttonName,
                }
            });

            await SendTrackingDataAsync(TRACK_URL, jsonPayload);
        }

        /// <summary>
        /// Async method to send tracking data to Segment
        /// More info about track event: https://segment.com/docs/connections/spec/track/ 
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="properties"></param>
        public static async void TrackEvent(string eventName, SegmentProperties properties)
        {
            var payload = new SegmentTrackPayload
            {
                anonymousId = _anonymousId,
                organizationName = _organizationName,
                sdkVersion = Cognitive3D_Manager.SDK_VERSION,
                @event = eventName,
                properties = properties
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);

            await SendTrackingDataAsync(TRACK_URL, jsonPayload);
        }

        private static async Task SendTrackingDataAsync(string trackURL, string data)
        {
            if (string.IsNullOrEmpty(_writeKey)) FetchKey();

            if (!string.IsNullOrEmpty(_writeKey))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(data);
                UnityWebRequest request = UnityWebRequest.Put(trackURL, bytes);
                request.method = "POST";
                request.SetRequestHeader("Content-Type", "application/json");
                // Segment requires basic auth using a base64-encoded Write Key
                string auth = System.Convert.ToBase64String(Encoding.ASCII.GetBytes(_writeKey + ":"));
                request.SetRequestHeader("Authorization", "Basic " + auth);
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }
            }
        }

        private static void GetSubscriptionResponse(int responseCode, string error, string text)
        {
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
            if (organizationDetails == null)
            {
                Debug.LogError("GetSubscriptionResponse data is null or invalid. Please get in touch");
            }
            else
            {
                _organizationName = organizationDetails.organizationName;
            }
        }

        // Classes to match Segment API payload structure
        [System.Serializable]
        internal class SegmentTrackPayload
        {
            public string anonymousId;
            public string organizationName;
            public string sdkVersion;
            public string @event;
            public string name;
            public SegmentProperties properties;
        }

        [System.Serializable]
        internal class SegmentProperties
        {
            public string buttonName;
            public string status;

            [JsonExtensionData]
            private Dictionary<string, object> otherProperties = new Dictionary<string, object>();

            public void SetProperty(string key, object value)
            {
                otherProperties[key] = value;
            }

            public object GetProperty(string key)
            {
                return otherProperties.TryGetValue(key, out var value) ? value : null;
            }
        }
    }
}
