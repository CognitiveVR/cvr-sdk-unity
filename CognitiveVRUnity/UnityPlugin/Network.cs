using UnityEngine;
using System.Collections.Generic;

namespace CognitiveVR
{
    public class NetworkManager : MonoBehaviour
    {
        static NetworkManager _sender;
        static NetworkManager Sender
        {
            get
            {
                if (_sender == null)
                {
                    var go = new GameObject("Cognitive Network");
                    Object.DontDestroyOnLoad(go);
                    _sender = go.AddComponent<NetworkManager>();
                }
                return _sender;
            }
        }

        public delegate void Response(int responsecode, string error, string text);

        //class WebRequest
        //{
        //    public WWW Request;
        //    public Response Response;
        //    public WebRequest(WWW request, Response response)
        //    {
        //        Request = request;
        //        Response = response;
        //    }
        //}

        System.Collections.IEnumerator WaitForResponse(WWW www, Response callback)
        {
            yield return www;
            if (callback != null)
            {
                callback.Invoke(Util.GetResponseCode(www.responseHeaders), www.error, www.text);
            }
        }

        static Dictionary<string, string> getHeaders;

        public static void Get(string url, Response callback)
        {
            if (getHeaders == null)//AUTH
            {
                 getHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "GET" }, {"Auth",CognitiveVR_Preferences.Instance.APIKey } };
            }
            WWW www = new WWW(url,null, getHeaders);
            Sender.StartCoroutine(Sender.WaitForResponse(www, callback));
        }

        static Dictionary<string, string> postHeaders;// = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" } };

        public static void Post(string url, string stringcontent)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Auth", CognitiveVR_Preferences.Instance.APIKey } };
            }
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            WWW www = new WWW(url, bytes,postHeaders);
        }

        public static void Post(string url, byte[] bytecontent)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Auth", CognitiveVR_Preferences.Instance.APIKey } };
            }
            WWW www = new WWW(url, bytecontent, postHeaders);
        }

        public static void Post(string url, string stringcontent, Dictionary<string, string> headers)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Auth", CognitiveVR_Preferences.Instance.APIKey } };
            }
            foreach (var kvp in postHeaders)
            {
                if (!headers.ContainsKey(kvp.Key)) { headers.Add(kvp.Key, kvp.Value); }
            }
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            WWW www = new WWW(url, bytes, headers);
        }

        /// <summary>
        /// headers replaces default post headers
        /// </summary>
        /// <param name="url"></param>
        /// <param name="bytecontent"></param>
        /// <param name="headers"></param>
        /// <param name="callback"></param>
        public static void Post(string url, byte[] bytecontent, Dictionary<string, string> headers, Response callback)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Auth", CognitiveVR_Preferences.Instance.APIKey } };
            }
            foreach(var kvp in postHeaders)
            {
                if (!headers.ContainsKey(kvp.Key)) { headers.Add(kvp.Key, kvp.Value); }
            }
            WWW www = new WWW(url, bytecontent, headers);
            Sender.StartCoroutine(Sender.WaitForResponse(www, callback));
        }

        public static void Post(string url, byte[] bytecontent, Dictionary<string, string> headers)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Auth", CognitiveVR_Preferences.Instance.APIKey } };
            }
            foreach (var kvp in postHeaders)
            {
                if (!headers.ContainsKey(kvp.Key)) { headers.Add(kvp.Key, kvp.Value); }
            }
            WWW www = new WWW(url, bytecontent, headers);
        }

        public static void Post(string url, byte[] bytecontent, Response callback)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Auth", CognitiveVR_Preferences.Instance.APIKey } };
            }
            WWW www = new WWW(url, bytecontent);
            Sender.StartCoroutine(Sender.WaitForResponse(www, callback));
        }
    }
}
