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
        private const string IDENTIFY_URL = "https://api.segment.io/v1/identify";
        private const string TRACK_URL = "https://api.segment.io/v1/track";
        private const string GROUP_URL = "https://api.segment.io/v1/group";

        private static string _writeKey;
        private static int _anonymousId;
        private static int _userId;
        private static int _groupId;

        static SegmentAnalytics()
        {
            Init();
        }

        /// <summary>
        /// Method to fetch the Segment key and identify user
        /// </summary>
        private static async void Init()
        {
            _writeKey = await GetKeyFromServerAsync();

            _anonymousId = Mathf.Abs(System.Guid.NewGuid().GetHashCode());
            _groupId = Mathf.Abs(System.Guid.NewGuid().GetHashCode());

            if (!string.IsNullOrEmpty(EditorCore.DeveloperKey))
            {
                EditorCore.GetUserData(EditorCore.DeveloperKey, GetUserResponse);
            } 
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

        public static async void Identify(EditorCore.UserData userData)
        {
            var _userTraits = new SegmentUserTraits();
            _userTraits.sdkVersion = Cognitive3D_Manager.SDK_VERSION;
            if (userData != null)
            {
                _userTraits.name = $"{userData.firstName} {userData.lastName}";
                _userTraits.email = userData.email;
                _userTraits.projectId = userData.projectId;
                _userTraits.projectName = userData.projectName;
            }            

            // Create the data payload in JSON format
            string jsonPayload = UnityEngine.JsonUtility.ToJson(new SegmentIdentifyPayload
            {
                anonymousId = _anonymousId,
                userId = _userId,
                traits = _userTraits
            });

            await SendTrackingDataAsync(IDENTIFY_URL, jsonPayload);
        }

        public static async void Group(EditorCore.UserData userData)
        {
            var _groupTraits = new SegmentGroupTraits();

            if (userData != null)
            {
                _groupTraits.organizationName = userData.organizationName;
            }

            // Create the data payload in JSON format
            string jsonPayload = UnityEngine.JsonUtility.ToJson(new SegmentGroupPayload
            {
                groupId = _groupId,
                anonymousId = _anonymousId,
                userId = _userId,
                traits = _groupTraits
            });

            await SendTrackingDataAsync(GROUP_URL, jsonPayload);
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
                userId = _userId,
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
                userId = _userId,
                @event = eventName,
                properties = properties
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);

            await SendTrackingDataAsync(TRACK_URL, jsonPayload);
        }

        private static async Task SendTrackingDataAsync(string trackURL, string data)
        {
            if (string.IsNullOrEmpty(_writeKey)) Init();

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


        private static void GetUserResponse(int responseCode, string error, string text)
        {
            var userdata = JsonUtility.FromJson<EditorCore.UserData>(text);
            if (responseCode == 200 && userdata != null)
            {
                _userId = userdata.userId;
                _groupId = userdata.organizationId;
            }

            Identify(userdata);
            Group(userdata);
        }

        // Classes to match Segment API payload structure
        [System.Serializable]
        internal class SegmentGroupPayload
        {
            // Same as organization ID
            public int groupId;
            public int anonymousId;
            public int userId;
            public SegmentGroupTraits traits;
        }

        [System.Serializable]
        public class SegmentGroupTraits
        {
            public string organizationName;
        }

        [System.Serializable]
        internal class SegmentIdentifyPayload
        {
            public int anonymousId;
            public int userId;
            public SegmentUserTraits traits;
        }

        [System.Serializable]
        public class SegmentUserTraits
        {
            public string name;
            public string email;
            public string sdkVersion;
            public int projectId;
            public string projectName;
        }

        [System.Serializable]
        internal class SegmentTrackPayload
        {
            public int anonymousId;
            public int userId;
            public string organizationName;
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

            internal void SetProperty(string key, object value)
            {
                otherProperties[key] = value;
            }

            internal object GetProperty(string key)
            {
                return otherProperties.TryGetValue(key, out var value) ? value : null;
            }
        }
    }
}
