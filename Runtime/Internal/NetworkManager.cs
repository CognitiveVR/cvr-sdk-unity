using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Threading.Tasks;

//handles network requests at runtime
//also handles local storage of data. saving + uploading
//stack of line lengths, read/write through single filestream

//TODO should use async/await for web requests instead of coroutines - less garbage and faster
//IMPROVEMENT shouldn't be a monobehaviour. shouldn't be static. instance should live on Core
//IMPROVEMENT handle writing to cache (if it exists). move this from CoreInterface

namespace Cognitive3D
{
    [AddComponentMenu("")]
    internal class NetworkManager : MonoBehaviour
    {
        //used by posting session data - get all details of the web response
        public delegate void FullResponse(string url, string uploadcontent, int responsecode, string error, string downloadcontent);
        //used by getting exitpoll question set - only need to know the c
        public delegate void Response(int responsecode, string error, string text);

        static NetworkManager instance;

        //requests that are not from the cache and should write to the cache if session ends and requests are aborted
        static HashSet<UnityWebRequest> activeRequests = new HashSet<UnityWebRequest>();

        internal ICache runtimeCache;
        internal ILocalExitpoll exitpollCache;
        int lastDataResponse = 0;

        internal void Initialize(ICache runtimeCache, ILocalExitpoll exitpollCache)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
            this.runtimeCache = runtimeCache;
            this.exitpollCache = exitpollCache;
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
            lastDataResponse = responsecode;
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
                if (Cognitive3D_Preferences.Instance.LocalStorage)
                {
                    string text;
                    if (Cognitive3D_Manager.ExitpollHandler.GetExitpoll(hookname,out text))
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
                if (Cognitive3D_Preferences.Instance.LocalStorage)
                {
                    Cognitive3D_Manager.ExitpollHandler.WriteExitpoll(hookname, www.downloadHandler.text);
                    //LocalCache.WriteExitpoll(hookname, www.downloadHandler.text);
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


            if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment("response code to "+www.url + "  " + www.responseCode);
            lastDataResponse = (int)www.responseCode;
            if (callback != null)
            {
                var headers = www.GetResponseHeaders();
                int responsecode = (int)www.responseCode;
                if (responsecode == 200)
                {
                    //check cvr header to make sure not blocked by capture portal
                    if (!headers.ContainsKey("cvr-request-time"))
                    {
                        if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                            Util.logDevelopment("capture portal error! " + www.url);
                        responsecode = 307;
                        lastDataResponse = responsecode;
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
                if (runtimeCache == null) { return; }
                if (isuploadingfromcache) { return; }
                if (runtimeCache.HasContent())
                {
                    UploadAllLocalData(() => Util.logDebug("Network Post Data Local Cache Complete"), () => Util.logDebug("Network Post Data Local Cache Automatic Failure"));
                }
            }
            else
            {
                if (responsecode < 500)
                {
                    switch (responsecode)
                    {
                        case 400: Util.logError("Network Post Data response code is 400. Bad Request"); break;
                        case 401: Util.logError("Network Post Data response code is 401. Is APIKEY set?"); break;
                        case 404: Util.logError("There is no scene associated with this SceneID on the dashboard. Please upload the scene using the Scene Setup Window."); break;
                        case -1: Util.logError("Network Post Data could not parse response code. Check upload URL"); break;
                        default: Util.logError("Network Post Data response code is " + responsecode); break;
                    }
                    return;
                }



                if (CacheRequest != null)
                {
                    isuploadingfromcache = false;
                    CacheResponseAction = null;
                    CacheRequest.Abort();
                    CacheRequest.Dispose();
                    CacheRequest = null;
                }

                if (runtimeCache == null) { return; }

                if (runtimeCache.CanWrite(url, content))
                {
                    //try to append to local cache file
                    runtimeCache.WriteContent(url, content);
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
                runtimeCache.PopContent();
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
                Util.logDevelopment("NETWORK UploadAllLocalData");
                if (string.IsNullOrEmpty(CognitiveStatics.ApplicationKey))
                CognitiveStatics.Initialize();

                //upload from local storage
                if (!Cognitive3D_Preferences.Instance.LocalStorage) { if (failedCallback != null) { failedCallback.Invoke(); } Util.logDevelopment("Local Cache is disabled"); return; }

                if (instance.runtimeCache == null){return;}

                cacheCompletedAction = completedCallback;
                cacheFailedAction = failedCallback;

                instance.LoopUploadFromLocalCache();
            }
            else
            {
                Util.logDevelopment("UploadAllLocalData cannot upload all local data - already in upload loop!");
            }
        }

        //is there an active web request to upload from the cache
        internal static bool isuploadingfromcache = false;


        UnityWebRequest CacheRequest;
        FullResponse CacheResponseAction;

        static System.Action cacheCompletedAction;
        static System.Action cacheFailedAction;

        //either started manually from LocalCache.UploadAllLocalData or from successful 200 response from current session data
        void LoopUploadFromLocalCache()
        {
            if (isuploadingfromcache) { return; }

            string url = "";
            string content = "";
            if (runtimeCache.PeekContent(ref url, ref content))
            {
                isuploadingfromcache = true;
                //lc.GetCachedDataPoint(out url, out content);
                
                //wait for post response
                var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(content);
                CacheRequest = UnityWebRequest.Put(url, bytes);
                CacheRequest.method = "POST";
                CacheRequest.SetRequestHeader("Content-Type", "application/json");
                CacheRequest.SetRequestHeader("X-HTTP-Method-Override", "POST");
                CacheRequest.SetRequestHeader("Authorization", CognitiveStatics.ApplicationKey);
                CacheRequest.SendWebRequest();

                if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                    Util.logDevelopment("NETWORK LoopUploadFromLocalCache " + url + " " + content);

                CacheResponseAction = instance.CACHEDResponseCallback;

                instance.StartCoroutine(instance.WaitForFullResponse(CacheRequest, content, CacheResponseAction, false));
            }
            else if (!runtimeCache.HasContent())
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
            request.SendWebRequest();

            instance.StartCoroutine(instance.WaitForExitpollResponse(request, hookname, callback,timeout));
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
            request.SendWebRequest();

            activeRequests.Add(request);
            instance.StartCoroutine(instance.WaitForFullResponse(request, stringcontent, instance.POSTResponseCallback, true));

            if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment(url + " " + stringcontent);
        }

        private async Task AsyncWaitForFullResponse(UnityWebRequest www, string contents, FullResponse callback, bool autoDispose)
        {
            float time = 0;
            float timeout = 10;
            //yield return new WaitUntil(() => www.isDone);
            while (time < timeout)
            {
                await Task.Yield();
                if (www == null) { break; }
                if (www.isDone) break;
                time += Time.deltaTime;
            }

            if (www == null)
            {
                Debug.LogError("WaitForFullResponse request is null!");
                return;
            }

            if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment("response code to " + www.url + "  " + www.responseCode);
            lastDataResponse = (int)www.responseCode;
            if (callback != null)
            {
                var headers = www.GetResponseHeaders();
                int responsecode = (int)www.responseCode;
                if (responsecode == 200)
                {
                    //check cvr header to make sure not blocked by capture portal
                    if (!headers.ContainsKey("cvr-request-time"))
                    {
                        if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                            Util.logDevelopment("capture portal error! " + www.url);
                        responsecode = 307;
                        lastDataResponse = responsecode;
                    }
                }
                callback.Invoke(www.url, contents, responsecode, www.error, www.downloadHandler.text);
            }
            if (autoDispose)
                www.Dispose();
            activeRequests.Remove(www);
        }

        internal async void Post(string url, string stringcontent)
        {
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            var request = UnityWebRequest.Put(url, bytes);
            request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-HTTP-Method-Override", "POST");
            request.SetRequestHeader("Authorization", CognitiveStatics.ApplicationKey);
            request.SendWebRequest();

            activeRequests.Add(request);
            await instance.AsyncWaitForFullResponse(request, stringcontent, instance.POSTResponseCallback,true);

            if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment("POST REQUEST  "+url + " " + stringcontent);
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
            if (runtimeCache != null)
            {
                runtimeCache.Close();
                runtimeCache = null;
            }
            isuploadingfromcache = false;
        }

        //called from core.reset
        internal void EndSession()
        {
            StopAllCoroutines();

            //write all active webrequests to cache
            if (Cognitive3D_Preferences.Instance.LocalStorage)
            {
                if (lastDataResponse != 200)
                {
                    //unclear if cached request will actually reach backend, based on previous response codes
                    //session is ending, so can't wait for response from this request
                    //abort request and write to local cache
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
                        if (runtimeCache == null) { break; }
                        if (runtimeCache.CanWrite(v.url, content))
                        {
                            v.Abort();
                            if (v.uploadHandler.data.Length > 0)
                            {
                                //lc.Append(v.url, content);
                                runtimeCache.WriteContent(v.url, content);
                            }
                        }
                    }
                }
                if (runtimeCache != null)
                {
                    runtimeCache.Close();
                    runtimeCache = null;
                }
            }
            activeRequests.Clear();
        }
    }
}
