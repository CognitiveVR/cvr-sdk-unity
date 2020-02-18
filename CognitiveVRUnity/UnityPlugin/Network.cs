using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;

//handles network requests at runtime
//also handles local storage of data. saving + uploading
//stack of line lengths, read/write through single filestream

//IMPROVEMENT? single coroutine queue waiting for network responses instead of creating many

namespace CognitiveVR
{
    [AddComponentMenu("")]
    public class NetworkManager : MonoBehaviour
    {
        //used by posting session data - get all details of the web response
        public delegate void FullResponse(string url, string uploadcontent, int responsecode, string error, string downloadcontent);
        //used by getting exitpoll question set - only need to know the c
        public delegate void Response(int responsecode, string error, string text);

        static NetworkManager _sender;
        internal static NetworkManager Sender
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

        //requests that are not from the cache and should write to the cache if session ends and requests are aborted
        static HashSet<UnityWebRequest> activeRequests = new HashSet<UnityWebRequest>();

        static LocalCache lc;

        /// <summary>
        /// called from CognitiveVR_Manager to set environmentEOL character. needed to correctly read local data cache
        /// </summary>
        /// <param name="environmentEOL"></param>
        public static void InitLocalStorage(string environmentEOL)
        {
            if (lc == null || LocalCache.EnvironmentEOL != environmentEOL)
            {
                lc = new LocalCache(Sender, environmentEOL);
            }
        }

        System.Collections.IEnumerator WaitForExitpollResponse(UnityWebRequest www, string hookname, Response callback, float timeout)
        {
            float time = 0;
            while (time < timeout)
            {
                yield return null;
                if (www.isDone) break;
                time += Time.deltaTime;
            }

            var headers = www.GetResponseHeaders();
            int responsecode = (int)www.responseCode;
            //check cvr header to make sure not blocked by capture portal

            if (!www.isDone)
                Util.logWarning("Network::WaitForExitpollResponse timeout");
            if (responsecode != 200)
                Util.logWarning("Network::WaitForExitpollResponse responsecode is " + responsecode);

            if (headers != null)
            {
                if (!headers.ContainsKey("cvr-request-time"))
                    Util.logWarning("Network::WaitForExitpollResponse does not contain cvr-request-time header");
            }

            if (!www.isDone || responsecode != 200 || (headers != null && !headers.ContainsKey("cvr-request-time")))
            {
                if (LocalCache.LocalStorageActive)
                {
                    string text;
                    if (LocalCache.GetExitpoll(hookname, out text))
                    {
                        if (callback != null)
                        {
                            callback.Invoke(responsecode, "", text);
                        }
                    }
                    else
                    {
                        if (callback != null)
                        {
                            callback.Invoke(responsecode, "", "");
                        }
                    }
                }
                else
                {
                    if (callback != null)
                    {
                        callback.Invoke(responsecode, "", "");
                    }
                }
            }
            else
            {
                if (callback != null)
                {
                    callback.Invoke(responsecode, www.error, www.downloadHandler.text);
                }
                if (LocalCache.LocalStorageActive)
                {
                    LocalCache.WriteExitpoll(hookname, www.downloadHandler.text);
                }
            }
            www.Dispose();
            activeRequests.Remove(www);
        }

        System.Collections.IEnumerator WaitForFullResponse(UnityWebRequest www, string contents, FullResponse callback, bool autoDispose)
        {
            float time = 0;
            float timeout = 10;
            //yield return new WaitUntil(() => www.isDone);
            while (time < timeout)
            {
                yield return null;
                if (www == null) { break; }
                if (www.isDone) break;
                time += Time.deltaTime;
            }

            if (www == null)
            {
                Debug.LogError("WaitForFullResponse request is null!");
                yield break;
            }


            if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment("response code to "+www.url + "  " + www.responseCode);

            if (callback != null)
            {
                var headers = www.GetResponseHeaders();
                int responsecode = (int)www.responseCode;
                if (responsecode == 200)
                {
                    //check cvr header to make sure not blocked by capture portal
                    if (!headers.ContainsKey("cvr-request-time"))
                    {
                        if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                            Util.logDevelopment("capture portal error! " + www.url);
                        responsecode = 404;
                    }
                }
                callback.Invoke(www.url, contents, responsecode, www.error, www.downloadHandler.text);
            }
            if (autoDispose)
                www.Dispose();
            activeRequests.Remove(www);
        }

        void POSTResponseCallback(string url, string content, int responsecode, string error, string text)
        {
            if (responsecode == 200)
            {
                if (lc == null) { Util.logError("Network Post Data 200 LocalCache null"); return; }
                if (lc.CanReadFromCache())
                {
                    UploadAllLocalData(() => Util.logDebug("Network Post Data Local Cache Complete"), () => Util.logDebug("Network Post Data Local Cache Automatic Failure"));
                }
            }
            else
            {
                if (responsecode == 401) { Util.logWarning("Network Post Data response code is 401. Is APIKEY set?"); return; } //ignore if invalid auth api key
                if (responsecode == -1) { Util.logWarning("Network Post Data could not parse response code. Check upload URL"); return; } //ignore. couldn't parse response code, likely malformed url

                if (CacheRequest != null)
                {
                    isuploadingfromcache = false;
                    CacheResponseAction = null;
                    CacheRequest.Abort();
                    CacheRequest.Dispose();
                    CacheRequest = null;
                }

                if (lc == null) { Util.logWarning("Network Post Data !200 LocalCache null"); return; }

                if (lc.CanAppend(url, content))
                {
                    //try to append to local cache file
                    lc.Append(url, content);
                }
            }
        }

        void CACHEDResponseCallback(string url, string content, int responsecode, string error, string text)
        {
            //before this callback is invoked, if headers does not contain cvr-request-time it sets the response code to 404



            if (responsecode == 200)
            {
                CacheRequest.Dispose();
                CacheRequest = null;
                CacheResponseAction = null;
                lc.SuccessfulResponse();
                isuploadingfromcache = false;
                LoopUploadFromLocalCache();
            }
            else
            {
                if (cacheFailedAction != null)
                {
                    cacheFailedAction.Invoke();
                }
                cacheFailedAction = null;
                cacheCompletedAction = null;

                isuploadingfromcache = false;
                CacheResponseAction = null;
                CacheRequest.Abort();
                CacheRequest.Dispose();
                CacheRequest = null;
            }
        }

        /// <summary>
        /// Upload all data from local storage. will call completed after everything has been uploaded, failed if not connected to internet or local storage not enabled
        /// </summary>
        /// <param name="completedCallback"></param>
        /// <param name="failedCallback"></param>
        public static void UploadAllLocalData(System.Action completedCallback, System.Action failedCallback)
        {
            if (!isuploadingfromcache)
            {
                Debug.Log("NETWORK UploadAllLocalData");
                if (string.IsNullOrEmpty(CognitiveStatics.ApplicationKey))
                CognitiveStatics.Initialize();

                //upload from local storage
                if (!CognitiveVR_Preferences.Instance.LocalStorage) { if (failedCallback != null) { failedCallback.Invoke(); } return; }

                if (lc == null)
                {
                    lc = new LocalCache(Sender,null);
                }

                cacheCompletedAction = completedCallback;
                cacheFailedAction = failedCallback;

                Sender.LoopUploadFromLocalCache();
            }
            else
            {
                Debug.Log("UploadAllLocalData cannot upload all local data - already in upload loop!");
            }
        }

        //is there an active web request to upload from the cache
        internal static bool isuploadingfromcache = false;


        UnityWebRequest CacheRequest;
        FullResponse CacheResponseAction;

        static System.Action cacheCompletedAction;
        static System.Action cacheFailedAction;

        public static int GetLocalStorageBatchCount()
        {
            if (lc == null)
                InitLocalStorage(null);
            return lc.GetCacheLineCount();
        }

        //either started manually from LocalCache.UploadAllLocalData or from successful 200 response from current session data
        void LoopUploadFromLocalCache()
        {
            if (isuploadingfromcache) { return; }

            if (lc.CanReadFromCache())
            {
                isuploadingfromcache = true;
                string url = "";
                string content = "";
                lc.GetCachedDataPoint(out url, out content);
                
                //wait for post response
                var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(content);
                CacheRequest = UnityWebRequest.Put(url, bytes);
                CacheRequest.method = "POST";
                CacheRequest.SetRequestHeader("Content-Type", "application/json");
                CacheRequest.SetRequestHeader("X-HTTP-Method-Override", "POST");
                CacheRequest.SetRequestHeader("Authorization", CognitiveStatics.ApplicationKey);
                CacheRequest.Send();

                if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                    Util.logDevelopment("NETWORK LoopUploadFromLocalCache " + url + " " + content);

                CacheResponseAction = Sender.CACHEDResponseCallback;

                Sender.StartCoroutine(Sender.WaitForFullResponse(CacheRequest, content, CacheResponseAction, false));
            }
            else if (lc.CacheEmpty())
            {
                if (cacheCompletedAction != null)
                    cacheCompletedAction.Invoke();
                cacheCompletedAction = null;
            }
        }
        
        /// <summary>
        /// uses the Response 'callback' when the question set is recieved from the dashboard. if offline, tries to get question set from local cache
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="callback"></param>
        /// <param name="timeout"></param>
        public static void GetExitPollQuestions(string hookname, Response callback, float timeout = 3)
        {
            string url = CognitiveStatics.GETEXITPOLLQUESTIONSET(hookname);
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-HTTP-Method-Override", "GET");
            request.SetRequestHeader("Authorization", CognitiveStatics.ApplicationKey);
            request.Send();

            Sender.StartCoroutine(Sender.WaitForExitpollResponse(request, hookname, callback,timeout));
        }

        public static void PostExitpollAnswers(string stringcontent, string questionSetName, int questionSetVersion)
        {
            string url = CognitiveStatics.POSTEXITPOLLRESPONSES(questionSetName, questionSetVersion);

            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            var request = UnityWebRequest.Put(url, bytes);
            request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-HTTP-Method-Override", "POST");
            request.SetRequestHeader("Authorization", CognitiveStatics.ApplicationKey);
            request.Send();

            activeRequests.Add(request);
            Sender.StartCoroutine(Sender.WaitForFullResponse(request, stringcontent, Sender.POSTResponseCallback, true));

            if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment(url + " " + stringcontent);
        }

        public static void Post(string url, string stringcontent)
        {
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            var request = UnityWebRequest.Put(url, bytes);
            request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-HTTP-Method-Override", "POST");
            request.SetRequestHeader("Authorization", CognitiveStatics.ApplicationKey);
            request.Send();

            activeRequests.Add(request);
            Sender.StartCoroutine(Sender.WaitForFullResponse(request, stringcontent, Sender.POSTResponseCallback,true));

            if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment(url + " " + stringcontent);
        }

        //skip network cleanup if immediately/manually destroyed
        //gameobject is destroyed at end of frame
        //issue if ending session/destroy gameobject/new session all in one frame
        bool hasBeenDestroyed = false;
        internal void OnDestroy()
        {
            if (hasBeenDestroyed) { return; }
            hasBeenDestroyed = true;
            if (activeRequests.Count > 0)
            {
                EndSession();
            }
            if (lc != null)
            {
                lc.OnDestroy();
                lc = null;
            }
            isuploadingfromcache = false;
        }

        //called from core.reset
        internal void EndSession()
        {
            StopAllCoroutines();

            //write all active webrequests to cache
            if (LocalCache.LocalStorageActive)
            {
                foreach (var v in activeRequests)
                {
                    if (v.isDone) { continue; }
                    string content = System.Text.Encoding.UTF8.GetString(v.uploadHandler.data);

                    if (isuploadingfromcache && CacheRequest != null)
                    {
                        isuploadingfromcache = false;
                        CacheResponseAction = null;
                        CacheRequest.Abort();
                        CacheRequest.Dispose();
                        CacheRequest = null;
                    }
                    if (lc == null) { break; }
                    if (lc.CanAppend(v.url, content))
                    {
                        v.Abort();
                        if (v.uploadHandler.data.Length > 0)
                        {
                            lc.Append(v.url, content);
                        }
                    }
                }
            }
            activeRequests.Clear();
        }
    }
}
