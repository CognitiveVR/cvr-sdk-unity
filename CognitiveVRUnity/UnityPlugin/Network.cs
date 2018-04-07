using UnityEngine;
using System.Collections.Generic;
using System.IO;

//handles network requests at runtime
//also handles local storage of data. saving + uploading
//TODO test only saving 1 file, separating lines into url, contents, urls, contents, etc. memory overhead, but cheaper than open/close filestreams

namespace CognitiveVR
{
    public class NetworkManager : MonoBehaviour
    {
        static bool showDebugLogs = false;
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

        static string localDataPath = Application.persistentDataPath + "/c3dlocal/";
        static string localExitPollPath = Application.persistentDataPath + "/c3dlocal/exitpoll/";

        public delegate void FullResponse(string url, string content, int responsecode, string error, string text, bool uploadLocalData);

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

        System.Collections.IEnumerator WaitForExitpollResponse(WWW www, string hookname, Response callback, float timeout)
        {
            float time = 0;
            while (time < timeout)
            {
                yield return null;
                if (www.isDone) break;
                time += Time.deltaTime;
            }

            int responsecode = Util.GetResponseCode(www.responseHeaders);

            if (!www.isDone || responsecode != 200)
            {
                //try to read from file
                if (File.Exists(localExitPollPath+hookname))
                {
                    var text = File.ReadAllText(localExitPollPath + hookname);
                    if (callback != null)
                    {
                        callback.Invoke(responsecode, "", text);
                    }
                }
                else
                {
                    //do callback, even if no files saved
                    if (callback != null)
                    {
                        callback.Invoke(responsecode, "", "");
                    }
                }
            }
            else
            {
                if (!CognitiveVR_Preferences.Instance.LocalStorage) { yield break; }
                //write response to file
                File.WriteAllText(localExitPollPath + hookname, www.text);
                if (callback != null)
                {
                    callback.Invoke(responsecode, www.error, www.text);
                }
            }
        }

        System.Collections.IEnumerator WaitForFullResponse(WWW www, string contents, FullResponse callback, bool allowLocalUpload)
        {
            yield return www;
            if (callback != null)
            {
                callback.Invoke(www.url, contents, Util.GetResponseCode(www.responseHeaders), www.error, www.text, allowLocalUpload);
            }
        }

        void GenericPostFullResponse(string url, string content, int responsecode, string error, string text, bool allowLocalUpload)
        {
            if (!allowLocalUpload) { return; }

            if (responsecode == 200)
            {
                if (!CognitiveVR_Preferences.Instance.LocalStorage) { return; }
                //search through files and upload outstanding data + remove that file
                if (LocalDataFilenames.Count > 0)
                {
                    UploadLocalFile();
                }
            }
            else
            {
                //if (responsecode == 401) { return; } //ignore if invalid auth api key
                //write to file
                WriteRequestToFile(url, content);
            }
        }

        static long LocalDataSize = 0;
        static Queue<string> LocalDataFilenames = new Queue<string>();

        //called on init to find all files not uploaded
        public static void FindLocalDataFilenames()
        {
            if (!CognitiveVR_Preferences.Instance.LocalStorage) { return; }
            if (!Directory.Exists(localDataPath))
                Directory.CreateDirectory(localDataPath);
            if (!Directory.Exists(localExitPollPath))
                Directory.CreateDirectory(localExitPollPath);

            var files = Directory.GetFiles(localDataPath);
            for (int i = 0; i < files.Length; i++)
            {
                LocalDataFilenames.Enqueue(Path.GetFileName(files[i]));
                FileInfo fi = new FileInfo(files[i]);
                LocalDataSize += fi.Length;
            }
            if (showDebugLogs) { Debug.Log("startup found cache of " + files.Length + " items. total size " + LocalDataSize); }
        }

        int filenameincrement = 1;
        void WriteRequestToFile(string url, string contents)
        {
            if (!CognitiveVR_Preferences.Instance.LocalStorage) { return; }

            string fullcontents = url + "\n" + contents;
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(fullcontents);

            if (LocalDataSize + bytes.LongLength > CognitiveVR_Preferences.Instance.LocalDataCacheSize)
            {
                if (showDebugLogs) { Debug.Log(">>>>>>>>>>local cache size hit limit"); }
                return;
            }

            if (showDebugLogs) { Debug.Log(">>>>>>>>>>write request to file"); }

            string filename = "localdata" + filenameincrement.ToString() + (int)Util.Timestamp();
            //File.WriteAllText(localDataPath + filename, fullcontents);
            //File.WriteAllBytes(localDataPath + filename, bytes);
            using (var stream = File.Open(localDataPath + filename, FileMode.OpenOrCreate))
            {
                stream.Write(bytes, 0, bytes.Length);
                /*for (var i = 0; i < FileSize; ++i)
                {
                    stream.WriteByte(0);
                }*/
            }

            LocalDataFilenames.Enqueue(filename);
            filenameincrement++;
            LocalDataSize += bytes.LongLength;
        }

        //uploads a single local file from the queue. only called when a 200 is returned from a post request
        void UploadLocalFile(int count = 2)
        {
            for (int i = 0; i < count; i++)
            {
                if (LocalDataFilenames.Count == 0) { return; }
                //try to post this, might be removed then directly returned to queue
                string filename = LocalDataFilenames.Dequeue();
                var lines = File.ReadAllLines(localDataPath + filename);
                string url = lines[0];
                string contents = lines[1];
                LocalCachePost(url, contents);

                //TODO test if faster to get file info or get bytes from string
                FileInfo fi = new FileInfo(localDataPath + filename);
                LocalDataSize -= fi.Length;
                //var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(url + "\n" + contents);
                //LocalDataSize -= bytes.LongLength;

                if (showDebugLogs) { Debug.Log(">>>>>>>>>>upload cached file"); }

                File.Delete(localDataPath + filename);
            }
        }

        static Dictionary<string, string> getHeaders;

        public static void GetExitPollQuestions(string url, string hookname, Response callback, float timeout = 3)
        {

            if (getHeaders == null)//AUTH
            {
                getHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "GET" }, { "Authorization", "APIKEY:DATA " + CognitiveVR_Preferences.Instance.APIKey } };
            }
            WWW www = new WWW(url, null, getHeaders);
            Sender.StartCoroutine(Sender.WaitForExitpollResponse(www, hookname, callback,timeout));
        }

        //currently unused. TODO exitpoll should use this
        /*public static void Get(string url, Response callback)
        {
            if (getHeaders == null)//AUTH
            {
                 getHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "GET" }, {"Authorization","APIKEY:DATA "+CognitiveVR_Preferences.Instance.APIKey } };
            }
            WWW www = new WWW(url,null, getHeaders);
            Sender.StartCoroutine(Sender.WaitForResponse(www, callback));
        }*/

        static Dictionary<string, string> postHeaders;

        public static void Post(string url, string stringcontent)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Authorization", "APIKEY:DATA " + CognitiveVR_Preferences.Instance.APIKey } };
            }
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            WWW www = new WWW(url, bytes,postHeaders);
            Sender.StartCoroutine(Sender.WaitForFullResponse(www, stringcontent, Sender.GenericPostFullResponse,true));
        }

        //used internally so uploading a file from cache doesn't trigger more files
        public static void LocalCachePost(string url, string stringcontent)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Authorization", "APIKEY:DATA " + CognitiveVR_Preferences.Instance.APIKey } };
            }
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            WWW www = new WWW(url, bytes, postHeaders);
            Sender.StartCoroutine(Sender.WaitForFullResponse(www, stringcontent, Sender.GenericPostFullResponse,false));
        }

        /*public static void Post(string url, string stringcontent, Dictionary<string, string> headers)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Authorization", "APIKEY:DATA " + CognitiveVR_Preferences.Instance.APIKey } };
            }
            foreach (var kvp in postHeaders)
            {
                if (!headers.ContainsKey(kvp.Key)) { headers.Add(kvp.Key, kvp.Value); }
            }
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            WWW www = new WWW(url, bytes, headers);
            Sender.StartCoroutine(Sender.WaitForFullResponse(www, stringcontent, Sender.GenericPostFullResponse, true));
        }*/

        /// <summary>
        /// headers replaces default post headers
        /// </summary>
        /// <param name="url"></param>
        /// <param name="bytecontent"></param>
        /// <param name="headers"></param>
        /// <param name="callback"></param>
        /*public static void Post(string url, string content, Dictionary<string, string> headers, Response callback)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Authorization", "APIKEY:DATA " + CognitiveVR_Preferences.Instance.APIKey } };
            }
            foreach(var kvp in postHeaders)
            {
                if (!headers.ContainsKey(kvp.Key)) { headers.Add(kvp.Key, kvp.Value); }
            }
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(content);
            WWW www = new WWW(url, bytes, headers);
            Sender.StartCoroutine(Sender.WaitForResponse(www, callback));
        }

        public static void Post(string url, string content, Response callback)
        {
            if (postHeaders == null)//AUTH
            {
                postHeaders = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" }, { "Authorization", "APIKEY:DATA " + CognitiveVR_Preferences.Instance.APIKey } };
            }
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(content);
            WWW www = new WWW(url, bytes);
            Sender.StartCoroutine(Sender.WaitForResponse(www, callback));
        }*/
    }
}
