using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cognitive3D;
using UnityEngine.Networking;

namespace Cognitive3D
{
    internal class EditorNetwork
    {
        public delegate void Response(int responsecode, string error, string text);

        class EditorWebRequest
        {
            public UnityWebRequest Request;
            public Response Response;
            public bool IsBlocking;
            public string RequestName;
            public string RequestInfo;
            public System.Action<float> ProgressAction;
            public EditorWebRequest(UnityWebRequest request, Response response, bool blocking, string requestName, string requestInfo)
            {
                Request = request;
                Response = response;
                IsBlocking = blocking;
                RequestName = requestName;
                RequestInfo = requestInfo;
            }
            public EditorWebRequest(UnityWebRequest request, Response response, bool blocking, string requestName, string requestInfo, System.Action<float>progressAction)
            {
                Request = request;
                Response = response;
                IsBlocking = blocking;
                RequestName = requestName;
                RequestInfo = requestInfo;
                ProgressAction = progressAction;
            }
        }


        static List<EditorWebRequest> EditorWebRequests = new List<EditorWebRequest>();

        static Queue<EditorWebRequest> EditorWebRequestsQueue = new Queue<EditorWebRequest>();
        static EditorWebRequest ActiveQueuedWebRequest;

        public static void Get(string url, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Get", string requestInfo = "")
        {
            var req = UnityWebRequest.Get(url);
            req.timeout = 10;
            req.disposeUploadHandlerOnDispose = true;
            req.disposeDownloadHandlerOnDispose = true;
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-HTTP-Method-Override", "GET");
            foreach (var v in headers)
            {
                req.SetRequestHeader(v.Key, v.Value);
            }
            req.SendWebRequest();


            EditorWebRequests.Add(new EditorWebRequest(req, callback, blocking, requestName, requestInfo));

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        //adds a network post request to a queue that is sent one at a time
        public static void QueuePost(string url, string stringcontent, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Post", string requestInfo = "")
        {
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            var p = UnityWebRequest.Put(url, bytes);
            p.disposeUploadHandlerOnDispose = true;
            p.disposeDownloadHandlerOnDispose = true;
            p.method = "POST";
            p.SetRequestHeader("Content-Type", "application/json");
            p.SetRequestHeader("X-HTTP-Method-Override", "POST");
            foreach (var v in headers)
            {
                p.SetRequestHeader(v.Key, v.Value);
            }

            EditorWebRequestsQueue.Enqueue(new EditorWebRequest(p, callback, blocking, requestName, requestInfo));

            EditorApplication.update -= EditorQueueUpdate;
            EditorApplication.update += EditorQueueUpdate;
        }

        /// <summary>
        /// PUT request that streams directly from a file path instead of loading into memory.
        /// Uses UploadHandlerFile for memory-efficient uploads of large files.
        /// </summary>
        public static void PutFile(string url, string filePath, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Put", string requestInfo = "", System.Action<float> progressCallback = null)
        {
            var p = new UnityWebRequest(url, "PUT");
            p.uploadHandler = new UploadHandlerFile(filePath);
            p.downloadHandler = new DownloadHandlerBuffer();
            p.disposeUploadHandlerOnDispose = true;
            p.disposeDownloadHandlerOnDispose = true;
            p.SetRequestHeader("X-HTTP-Method-Override", "PUT");
            foreach (var v in headers)
            {
                p.SetRequestHeader(v.Key, v.Value);
            }
            p.SendWebRequest();

            EditorWebRequests.Add(new EditorWebRequest(p, callback, blocking, requestName, requestInfo, progressCallback));

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        /// <summary>
        /// POST request that streams directly from a file path instead of loading into memory.
        /// Uses UploadHandlerFile for memory-efficient uploads of large files.
        /// </summary>
        public static void PostFile(string url, string filePath, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Post", string requestInfo = "", System.Action<float> progressCallback = null)
        {
            var p = new UnityWebRequest(url, "POST");
            p.uploadHandler = new UploadHandlerFile(filePath);
            p.downloadHandler = new DownloadHandlerBuffer();
            p.disposeUploadHandlerOnDispose = true;
            p.disposeDownloadHandlerOnDispose = true;
            p.SetRequestHeader("X-HTTP-Method-Override", "POST");
            foreach (var v in headers)
            {
                p.SetRequestHeader(v.Key, v.Value);
            }
            p.SendWebRequest();

            EditorWebRequests.Add(new EditorWebRequest(p, callback, blocking, requestName, requestInfo, progressCallback));

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        //post a request immediately and listen for a response callback
        public static void Post(string url, byte[] bytecontent, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Post", string requestInfo = "", System.Action<float> progressCallback = null)
        {
            var p = UnityWebRequest.Put(url, bytecontent);
            p.disposeUploadHandlerOnDispose = true;
            p.disposeDownloadHandlerOnDispose = true;
            p.method = "POST";
            p.SetRequestHeader("X-HTTP-Method-Override", "POST");
            foreach (var v in headers)
            {
                p.SetRequestHeader(v.Key, v.Value);
            }
            p.SendWebRequest();

            EditorWebRequests.Add(new EditorWebRequest(p, callback, blocking, requestName, requestInfo, progressCallback));

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        //post a request immediately and listen for a response callback
        public static void Post(string url, WWWForm formcontent, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Post", string requestInfo = "")
        {
            var p = UnityWebRequest.Post(url, formcontent);
            p.disposeUploadHandlerOnDispose = true;
            p.disposeDownloadHandlerOnDispose = true;
            p.SetRequestHeader("X-HTTP-Method-Override", "POST");
            foreach (var v in headers)
            {
                p.SetRequestHeader(v.Key, v.Value);
            }
            p.SendWebRequest();

            EditorWebRequests.Add(new EditorWebRequest(p, callback, blocking, requestName, requestInfo));

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        //patch a request immediately and listen for a response callback
        public static void Patch(string url, string stringcontent, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Patch", string requestInfo = "")
        {
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            var p = UnityWebRequest.Put(url, bytes);
            p.disposeUploadHandlerOnDispose = true;
            p.disposeDownloadHandlerOnDispose = true;
            p.method = "PATCH";
            p.SetRequestHeader("X-HTTP-Method-Override", "PATCH");
            foreach (var v in headers)
            {
                p.SetRequestHeader(v.Key, v.Value);
            }
            p.SendWebRequest();

            EditorWebRequests.Add(new EditorWebRequest(p, callback, blocking, requestName, requestInfo));

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        //update all outstanding web requests
        static void EditorUpdate()
        {
            if (EditorWebRequests.Count == 0) { EditorApplication.update -= EditorUpdate; return; }
            for (int i = EditorWebRequests.Count - 1; i >= 0; i--)
            {
                var webRequest = EditorWebRequests[i];
                var request = webRequest.Request;

                float currentProgress = request.uploadProgress;

                if (webRequest.ProgressAction != null)
                {
                    webRequest.ProgressAction.Invoke(currentProgress);
                }
                if (webRequest.IsBlocking)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(webRequest.RequestName, webRequest.RequestInfo, currentProgress))
                    {
                        // User clicked cancel
                        EditorUtility.ClearProgressBar();

                        // Notify callback with cancelled status
                        if (EditorWebRequests[i].Response != null)
                        {
                            EditorWebRequests[i].Response.Invoke(100, "Upload cancelled by user", "");
                        }

                        // Properly dispose the request before removing
                        EditorWebRequests[i].Request.Abort();
                        EditorWebRequests[i].Request.Dispose();
                        EditorWebRequests.RemoveAt(i);
                        Debug.Log("<color=yellow>Upload cancelled by user.</color>");
                        continue; // Continue processing other requests instead of returning
                    }
                }
                if (!EditorWebRequests[i].Request.isDone)
                {
                    continue; // Continue checking other requests instead of returning
                }
                if (EditorWebRequests[i].IsBlocking)
                    EditorUtility.ClearProgressBar();

                try
                {
                    int responseCode = (int)EditorWebRequests[i].Request.responseCode;
                    Util.logDevelopment("Got Response from " + EditorWebRequests[i].Request.url + ": [CODE] " + responseCode
                        + (!string.IsNullOrEmpty(EditorWebRequests[i].Request.downloadHandler.text) ? " [TEXT] " + EditorWebRequests[i].Request.downloadHandler.text : "")
                        + (!string.IsNullOrEmpty(EditorWebRequests[i].Request.error) ? " [ERROR] " + EditorWebRequests[i].Request.error : ""));
                    if (EditorWebRequests[i].Response != null)
                    {
                        EditorWebRequests[i].Response.Invoke(responseCode, EditorWebRequests[i].Request.error, EditorWebRequests[i].Request.downloadHandler.text);
                    }
                }
                finally //if there is an error in try, still remove request and dispose
                {
                    if (EditorWebRequests[i].IsBlocking)
                    {
                        EditorUtility.ClearProgressBar();
                    }
                    EditorWebRequests[i].Request.Dispose();
                    EditorWebRequests.RemoveAt(i);
                }
            }
        }

        static void EditorQueueUpdate()
        {
            if (ActiveQueuedWebRequest == null && EditorWebRequestsQueue.Count == 0) { EditorUtility.ClearProgressBar(); EditorApplication.update -= EditorQueueUpdate; return; }
            if (ActiveQueuedWebRequest == null)
            {
                ActiveQueuedWebRequest = EditorWebRequestsQueue.Dequeue();
                ActiveQueuedWebRequest.Request.SendWebRequest();
            }
            EditorUtility.DisplayProgressBar(ActiveQueuedWebRequest.RequestName, ActiveQueuedWebRequest.RequestInfo, ActiveQueuedWebRequest.Request.uploadProgress);
            if (ActiveQueuedWebRequest.Request.isDone)
            {
                EditorUtility.ClearProgressBar();
                int responseCode = (int)ActiveQueuedWebRequest.Request.responseCode;
                Util.logDevelopment("Got Response from " + ActiveQueuedWebRequest.Request.url + ": [CODE] " + responseCode
                    + (!string.IsNullOrEmpty(ActiveQueuedWebRequest.Request.downloadHandler.text) ? " [TEXT] " + ActiveQueuedWebRequest.Request.downloadHandler.text : "")
                    + (!string.IsNullOrEmpty(ActiveQueuedWebRequest.Request.error) ? " [ERROR] " + ActiveQueuedWebRequest.Request.error : ""));
                if (ActiveQueuedWebRequest.Response != null)
                {
                    ActiveQueuedWebRequest.Response.Invoke(responseCode, ActiveQueuedWebRequest.Request.error, ActiveQueuedWebRequest.Request.downloadHandler.text);
                }

                // ready for next request
                ActiveQueuedWebRequest.Request.Dispose();
                ActiveQueuedWebRequest = null;
            }
        }
    }
}