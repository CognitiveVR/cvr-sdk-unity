using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;

namespace CognitiveVR
{
public class EditorNetwork
{
    public delegate void Response(int responsecode, string error, string text);
    public delegate void mydelegate();

    class EditorWebRequest
    {
        public WWW Request;
        public Response Response;
        public bool IsBlocking;
        public string RequestName;
        public string RequestInfo;
        public EditorWebRequest(WWW request, Response response, bool blocking, string requestName, string requestInfo)
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
        if (headers == null) { headers = new Dictionary<string, string>(); }
        if (!headers.ContainsKey("Content-Type")){ headers.Add("Content-Type", "application/json"); }
        if (!headers.ContainsKey("X-HTTP-Method-Override")){ headers.Add("X-HTTP-Method-Override", "GET"); }
        WWW www = new WWW(url,null, headers);

        EditorWebRequests.Add(new EditorWebRequest(www, callback, blocking, requestName, requestInfo));

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    public static void Post(string url, string stringcontent, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Post", string requestInfo = "")
    {
        if (headers == null) { headers = new Dictionary<string, string>(); }
        if (!headers.ContainsKey("X-HTTP-Method-Override")) { headers.Add("X-HTTP-Method-Override", "POST"); }

        var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
        WWW www = new WWW(url,bytes,headers);

        EditorWebRequests.Add(new EditorWebRequest(www, callback, blocking, requestName, requestInfo));

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    public static void Post(string url, byte[] bytecontent, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Post", string requestInfo = "")
    {
        if (headers == null) { headers = new Dictionary<string, string>(); }
        if (!headers.ContainsKey("X-HTTP-Method-Override")) { headers.Add("X-HTTP-Method-Override", "POST"); }
        WWW www = new WWW(url, bytecontent,headers);

        EditorWebRequests.Add(new EditorWebRequest(www, callback, blocking, requestName, requestInfo));

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    public static void Post(string url, WWWForm formcontent, Response callback, Dictionary<string, string> headers, bool blocking, string requestName = "Post", string requestInfo = "")
    {
        if (headers == null) { headers = new Dictionary<string, string>(); }
        if (!headers.ContainsKey("X-HTTP-Method-Override")) { headers.Add("X-HTTP-Method-Override", "POST"); }
        WWW www = new WWW(url, formcontent.data,headers);

        EditorWebRequests.Add(new EditorWebRequest(www, callback, blocking, requestName, requestInfo));

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
                EditorUtility.DisplayProgressBar(EditorWebRequests[i].RequestName, EditorWebRequests[i].RequestInfo, EditorWebRequests[i].Request.progress);
            if (!EditorWebRequests[i].Request.isDone) { return; }
            if (EditorWebRequests[i].IsBlocking)
                EditorUtility.ClearProgressBar();

            try
            {
                int responseCode = CognitiveVR.Util.GetResponseCode(EditorWebRequests[i].Request.responseHeaders);
                Util.logDebug("Got Response from " + EditorWebRequests[i].Request.url + ": [CODE] " + responseCode + " [TEXT] " + EditorWebRequests[i].Request.text + " [ERROR] " + EditorWebRequests[i].Request.error);
                if (EditorWebRequests[i].Response != null)
                {
                    EditorWebRequests[i].Response.Invoke(responseCode, EditorWebRequests[i].Request.error, EditorWebRequests[i].Request.text);
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