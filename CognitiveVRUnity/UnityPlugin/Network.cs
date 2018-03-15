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

        class WebRequest
        {
            public WWW Request;
            public Response Response;
            public WebRequest(WWW request, Response response)
            {
                Request = request;
                Response = response;
            }
        }

        System.Collections.IEnumerator WaitForResponse(WWW www, Response callback)
        {
            yield return www;
            if (callback != null)
            {
                callback.Invoke(Util.GetResponseCode(www.responseHeaders), www.error, www.text);
            }
        }

        public static void Get(string url, Response callback)
        {
            WWW www = new WWW(url);
            Sender.StartCoroutine(Sender.WaitForResponse(www, callback));
        }

        public static void Post(string url, string stringcontent)
        {
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            WWW www = new WWW(url, bytes);
        }

        public static void Post(string url, byte[] bytecontent)
        {
            WWW www = new WWW(url, bytecontent);
        }

        public static void Post(string url, string stringcontent, Dictionary<string, string> headers)
        {
            //byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(sb.ToString()); //how is this different?
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            WWW www = new WWW(url, bytes, headers);
        }

        public static void Post(string url, byte[] bytecontent, Dictionary<string, string> headers, Response callback)
        {
            WWW www = new WWW(url, bytecontent, headers);
            Sender.StartCoroutine(Sender.WaitForResponse(www, callback));
        }

        public static void Post(string url, byte[] bytecontent, Dictionary<string, string> headers)
        {
            WWW www = new WWW(url, bytecontent, headers);
        }

        public static void Post(string url, byte[] bytecontent, Response callback)
        {
            WWW www = new WWW(url, bytecontent);
            Sender.StartCoroutine(Sender.WaitForResponse(www, callback));
        }
    }
}
