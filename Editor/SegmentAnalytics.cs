using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal static class SegmentAnalytics
    {
        private const string KEY_URL = "https://data.cognitive3d.com/segmentWriteKey";
        private const string TRACK_URL = "https://api.segment.io/v1/track";
        private const string PAGE_URL = "https://api.segment.io/v1/page";

        private static string writeKey;
        private static string anonymousId;

        static SegmentAnalytics()
        {
            anonymousId = System.Guid.NewGuid().ToString();
            FetchKey();
        }

        /// <summary>
        /// Method to fetch the Segment key
        /// </summary>
        private static async void FetchKey()
        {
            writeKey = await GetKeyFromServerAsync();
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

                // Check for network or HTTP errors
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    return null;
                }
                else
                {
                    return request.downloadHandler.text;
                }
            }
        }

        /// <summary>
        /// Async method to send page data to Segment
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="status"></param>
        /// <param name="buttonName"></param>
        public static async void PageEvent(string pageName, string status = "")
        {
            // Create the data payload in JSON format
            string jsonPayload = UnityEngine.JsonUtility.ToJson(new SegmentTrackPayload
            {
                anonymousId = anonymousId,
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
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="buttonName"></param>
        /// <param name="status"></param>
        public static async void TrackEvent(string eventName, string buttonName = "")
        {
            // Create the data payload in JSON format
            string jsonPayload = UnityEngine.JsonUtility.ToJson(new SegmentTrackPayload
            {
                anonymousId = anonymousId,
                @event = eventName,
                properties = new SegmentProperties
                {
                    buttonName = buttonName,
                }
            });

            await SendTrackingDataAsync(TRACK_URL, jsonPayload);
        }

        private static async Task SendTrackingDataAsync(string trackURL, string data)
        {
            if (string.IsNullOrEmpty(writeKey)) FetchKey();

            // Create the web request
            UnityWebRequest request = new UnityWebRequest(trackURL, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(data);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Set headers
            request.SetRequestHeader("Content-Type", "application/json");
            string auth = System.Convert.ToBase64String(Encoding.ASCII.GetBytes(writeKey + ":"));
            request.SetRequestHeader("Authorization", "Basic " + auth);

            // Send the request and wait asynchronously for a response
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Util.logError("Error sending data to Segment: " + request.error);
            }
        }

        // Classes to match Segment API payload structure
        [System.Serializable]
        private class SegmentTrackPayload
        {
            public string anonymousId;
            public string @event;
            public string name;
            public SegmentProperties properties;
        }

        [System.Serializable]
        private class SegmentProperties
        {
            public string buttonName;
            public string status;
        }
    }
}
