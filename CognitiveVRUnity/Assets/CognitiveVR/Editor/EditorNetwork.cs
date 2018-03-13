using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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

    public static void Get(string url, Response callback, bool blocking, string requestName = "Get", string requestInfo = "")
    {
        WWW www = new WWW(url);

        EditorWebRequests.Add(new EditorWebRequest(www, callback, blocking,requestName,requestInfo));

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    public static void Post(string url, string stringcontent, Response callback, bool blocking, string requestName = "Post", string requestInfo = "")
    {
        var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
        WWW www = new WWW(url,bytes);

        EditorWebRequests.Add(new EditorWebRequest(www, callback, blocking, requestName, requestInfo));

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    public static void Post(string url, byte[] bytecontent, Response callback, bool blocking, string requestName = "Post", string requestInfo = "")
    {
        WWW www = new WWW(url, bytecontent);

        EditorWebRequests.Add(new EditorWebRequest(www, callback, blocking, requestName, requestInfo));

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    public static void Post(string url, WWWForm formcontent, Response callback, bool blocking, string requestName = "Post", string requestInfo = "")
    {
        WWW www = new WWW(url, formcontent);

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

            int responseCode = CognitiveVR.Util.GetResponseCode(EditorWebRequests[i].Request.responseHeaders);
            Debug.Log("response from " + EditorWebRequests[i].Request.url + ": " + responseCode + " text " + EditorWebRequests[i].Request.text);
            if (EditorWebRequests[i].Response != null)
            {
                EditorWebRequests[i].Response.Invoke(responseCode, EditorWebRequests[i].Request.error, EditorWebRequests[i].Request.text);
            }
            EditorWebRequests.RemoveAt(i);
        }
    }
}
