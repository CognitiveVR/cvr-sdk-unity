using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;
using UnityEngine.Networking;

namespace CognitiveVR
{
public class EditorNetwork
{
    public delegate void Response(int responsecode, string error, string text);

    class EditorWebRequest
    {
        public UnityWebRequest Request;
        public Response Response;
        public bool IsBlocking;
        public string RequestName;
        public string RequestInfo;
        public EditorWebRequest(UnityWebRequest request, Response response, bool blocking, string requestName, string requestInfo)
        {
            Request = request;
            Response = response;
            IsBlocking = blocking;
            RequestName = requestName;
            RequestInfo = requestInfo;
        }
    }


    static List<EditorWebRequest> EditorWebRequests = new List<EditorWebRequest>();

    public static void Get(string url, Response callback, Dictionary<string,string> headers, bool blocking, string requestName = "Get", string requestInfo = "")
    {
        var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-HTTP-Method-Override", "GET");
        foreach (var v in headers)
        {
            req.SetRequestHeader(v.Key, v.Value);
        }
        req.Send();


        EditorWebRequests.Add(new EditorWebRequest(req, callback, blocking, requestName, requestInfo));

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    public static void Post(string url, string stringcontent, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Post", string requestInfo = "")
    {
        //if (headers == null) { headers = new Dictionary<string, string>(); }
        //if (!headers.ContainsKey("X-HTTP-Method-Override")) { headers.Add("X-HTTP-Method-Override", "POST"); }

        var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
        var p = UnityWebRequest.Put(url, bytes);
        p.method = "POST";
        p.SetRequestHeader("Content-Type", "application/json");
        p.SetRequestHeader("X-HTTP-Method-Override", "POST");
        foreach (var v in headers)
        {
            p.SetRequestHeader(v.Key, v.Value);
        }
        //p.SetRequestHeader("Authorization", "APIKEY:DATA " + CognitiveVR_Preferences.Instance.APIKey);
        p.Send();

        EditorWebRequests.Add(new EditorWebRequest(p, callback, blocking, requestName, requestInfo));

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    public static void Post(string url, byte[] bytecontent, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Post", string requestInfo = "")
    {
        //if (headers == null) { headers = new Dictionary<string, string>(); }
        //if (!headers.ContainsKey("X-HTTP-Method-Override")) { headers.Add("X-HTTP-Method-Override", "POST"); }
        var p = UnityWebRequest.Put(url, bytecontent);
        p.method = "POST";
        p.SetRequestHeader("X-HTTP-Method-Override", "POST");
        foreach (var v in headers)
        {
            p.SetRequestHeader(v.Key, v.Value);
        }
        p.Send();

        EditorWebRequests.Add(new EditorWebRequest(p, callback, blocking, requestName, requestInfo));

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    public static void Post(string url, WWWForm formcontent, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Post", string requestInfo = "")
    {            
        var p = UnityWebRequest.Post(url, formcontent);
        p.SetRequestHeader("X-HTTP-Method-Override", "POST");
        foreach (var v in headers)
        {
            p.SetRequestHeader(v.Key, v.Value);
        }
        p.Send();

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
            if (EditorWebRequests[i].IsBlocking)
                EditorUtility.DisplayProgressBar(EditorWebRequests[i].RequestName, EditorWebRequests[i].RequestInfo, EditorWebRequests[i].Request.uploadProgress);
            if (!EditorWebRequests[i].Request.isDone) { return; }
            if (EditorWebRequests[i].IsBlocking)
                EditorUtility.ClearProgressBar();

            try
            {
                    int responseCode = (int)EditorWebRequests[i].Request.responseCode;
                Util.logDebug("Got Response from " + EditorWebRequests[i].Request.url + ": [CODE] " + responseCode
                    + (!string.IsNullOrEmpty(EditorWebRequests[i].Request.downloadHandler.text) ? " [TEXT] " + EditorWebRequests[i].Request.downloadHandler.text:"")
                    + (!string.IsNullOrEmpty(EditorWebRequests[i].Request.error) ? " [ERROR] " + EditorWebRequests[i].Request.error:""));
                if (EditorWebRequests[i].Response != null)
                {
                    EditorWebRequests[i].Response.Invoke(responseCode, EditorWebRequests[i].Request.error, EditorWebRequests[i].Request.downloadHandler.text);
                }
            }
            finally //if there is an error in try, still remove request
            {
                EditorWebRequests.RemoveAt(i);
            }
        }
    }
}
}