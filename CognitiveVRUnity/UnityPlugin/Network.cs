using UnityEngine;
using System.Collections.Generic;
using System.IO;
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

        static string localDataPath = Application.persistentDataPath + "/c3dlocal/data";
        static string localExitPollPath = Application.persistentDataPath + "/c3dlocal/exitpoll/";

        public delegate void FullResponse(string url, string content, int responsecode, string error, string text, bool uploadLocalData);

        public delegate void Response(int responsecode, string error, string text);

        static StreamReader sr;
        static StreamWriter sw;
        static FileStream fs;
        //line sizes of contents, ignoring line breaks. line breaks added automatically from StreamWriter.WriteLine
        static Stack<int> linesizes = new Stack<int>();
        static int totalBytes = 0;

        //requests that are not from the cache and should write to the cache if session ends and requests are aborted
        static HashSet<UnityWebRequest> activeRequests = new HashSet<UnityWebRequest>();

        private void OnDestroy()
        {
            if (activeRequests.Count > 0)
            {
                EndSession();
            }
            Debug.Log("network on destroy");
            if (sr != null) sr.Close();
            if (sw != null) { sw.Close(); }
            if (fs != null) { fs.Close(); fs = null; }
        }

        //called from core.reset
        internal void EndSession()
        {
            Debug.Log("network end session");
            foreach (var v in activeRequests)
            {
                v.Abort();
                string content = System.Text.Encoding.UTF8.GetString(v.uploadHandler.data);
                if (content.Length > 0)
                {
                    WriteRequestToFile(v.url, content);
                }
            }
            activeRequests.Clear();
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
                //try to read from file
                if (enabledLocalStorage && File.Exists(localExitPollPath+hookname))
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
                if (callback != null)
                {
                    callback.Invoke(responsecode, www.error, www.downloadHandler.text);
                }
                if (enabledLocalStorage)
                {
                    //write content to exitpoll local storage
                    File.WriteAllText(localExitPollPath + hookname, www.downloadHandler.text);
                }
            }
            www.Dispose();
            activeRequests.Remove(www);
        }

        System.Collections.IEnumerator WaitForFullResponse(UnityWebRequest www, string contents, FullResponse callback, bool allowLocalUpload, bool autoDispose)
        {
            yield return new WaitUntil(() => www.isDone);

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
                callback.Invoke(www.url, contents, responsecode, www.error, www.downloadHandler.text, allowLocalUpload);
            }
            if (autoDispose)
                www.Dispose();
            activeRequests.Remove(www);
        }

        void GenericPostFullResponse(string url, string content, int responsecode, string error, string text, bool allowLocalUpload)
        {
            if (responsecode == 200)
            {
                if (!allowLocalUpload) { return; }
                if (!enabledLocalStorage) { return; }
                //search through files and upload outstanding data + remove that file
                UploadLocalFile();
            }
            else
            {
                if (responsecode == 401) { Util.logWarning("Network Post Data response code is 401. Is APIKEY set?"); return; } //ignore if invalid auth api key
                if (responsecode == -1) { Util.logWarning("Network Post Data could not parse response code. Check upload URL"); return; } //ignore. couldn't parse response code, likely malformed url
                //write to file
                if (allowLocalUpload)
                {
                    WriteRequestToFile(url, content);
                }
            }
        }

        static int EOLByteCount = 2;
        static string EnvironmentEOL;
        static int ReadLocalCacheCount;

        //set once at the beginning of the session. allowing this to change during runtime would likely bloat error checking for a probably never used feature
        static bool enabledLocalStorage = false;

        //called on init to find all files not uploaded
        public static void InitLocalStorage(string environmentEOL)
        {
            ReadLocalCacheCount = CognitiveVR_Preferences.Instance.ReadLocalCacheCount;
            EnvironmentEOL = environmentEOL;
            EOLByteCount = System.Text.Encoding.UTF8.GetByteCount(environmentEOL);
            enabledLocalStorage = CognitiveVR_Preferences.Instance.LocalStorage;

            if (!enabledLocalStorage) { return; }
            try
            {
                if (!Directory.Exists(localExitPollPath))
                    Directory.CreateDirectory(localExitPollPath);

                fs = File.Open(localDataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                sr = new StreamReader(fs);
                sw = new StreamWriter(fs);
                //read all line sizes from data
                while (sr.Peek() != -1)
                {
                    int lineLength = System.Text.Encoding.UTF8.GetByteCount(sr.ReadLine());
                    linesizes.Push(lineLength);
                    totalBytes += lineLength;
                }
            }
            catch (System.Exception e)
            {
                enabledLocalStorage = false;
                Debug.LogException(e);
            }
        }
        
        void WriteRequestToFile(string url, string contents)
        {
            if (!enabledLocalStorage) { return; }

            if (sw == null)
            {
                Debug.LogError("attempting to write request after streamwriter closed");
                return;
            }

            
            contents = contents.Replace('\n', ' ');

            int urlByteCount = System.Text.Encoding.UTF8.GetByteCount(url);
            int contentByteCount = System.Text.Encoding.UTF8.GetByteCount(contents);

            if (urlByteCount + contentByteCount + totalBytes > CognitiveVR.CognitiveVR_Preferences.Instance.LocalDataCacheSize)
            {
                //cache size reached! skip writing data
                return;
            }
            try
            {
                sw.Write(url);
                sw.Write(EnvironmentEOL);
                linesizes.Push(urlByteCount);
                totalBytes += urlByteCount;

                sw.Write(contents);
                sw.Write(EnvironmentEOL);
                linesizes.Push(contentByteCount);
                totalBytes += contentByteCount;

                sw.Flush();
            }
            catch(System.Exception e)
            {
                //turn off to avoid other errors
                enabledLocalStorage = false;
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Upload all data from local storage. will call completed after everything has been uploaded, failed if not connected to internet or local storage not enabled
        /// </summary>
        /// <param name="completedCallback"></param>
        /// <param name="failedCallback"></param>
        public static void UploadAllLocalData(System.Action completedCallback, System.Action failedCallback)
        {
            if (string.IsNullOrEmpty(CognitiveStatics.ApplicationKey))
                CognitiveStatics.Initialize();

            //upload from local storage
            if (!CognitiveVR_Preferences.Instance.LocalStorage) { if (failedCallback != null) { failedCallback.Invoke(); } return; }

            if (fs == null)
            {
                InitLocalStorage(System.Environment.NewLine);
            }

            Debug.Log("start local upload");
            Sender.StartCoroutine(Sender.ForceUploadLocalStorage(completedCallback,failedCallback));
        }

        static FileInfo localDataInfo;

        /// <summary>
        /// return 0-1 for how full the local cache is
        /// if not muted, will print a message with the current cache size
        /// </summary>
        /// <param name="mute"></param>
        /// <returns></returns>
        public static float GetLocalStorage(bool mute = false)
        {
            float percent = 0;
            try
            {
                localDataInfo = new FileInfo(localDataPath);
                if (!localDataInfo.Exists) { return 0; }
                int length = (int)localDataInfo.Length;
                percent = length / (float)CognitiveVR_Preferences.Instance.LocalDataCacheSize;

                if (!mute)
                {
                    string mbsizeformat = string.Format("{0:0.000}", (length / 1048576f));
                    string percentformat = string.Format("{0:0.000}", percent * 100);

                    if (percent > 0.5f)
                    {
                        Debug.LogWarning("Cognitive3D local cache is " + mbsizeformat + "MB, " + percentformat + "% full");
                    }
                    else
                    {
                        Debug.Log("Cognitive3D local cache is " + mbsizeformat + "MB, " + percentformat + "% full");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            return percent;
        }

        public static void OpenLocalStorageDirectory()
        {
            Application.OpenURL(Application.persistentDataPath + "/c3dlocal/");
        }

        bool isuploadingfromcache = false;
        System.Collections.IEnumerator ForceUploadLocalStorage(System.Action completed, System.Action failed)
        {
            if (isuploadingfromcache) { yield break; }

            while (linesizes.Count > 1)
            {
                isuploadingfromcache = true;
                //get contents from file

                //changed to read lines from file and write request before popping data from cache
                //could do it this way when writing requests - write locally immediately, send to server, pop from local if successful. overhead, but safe
                int contentsize = linesizes.Pop();
                int urlsize = linesizes.Pop();
                linesizes.Push(urlsize);
                linesizes.Push(contentsize);

                int lastrequestsize = contentsize + urlsize + EOLByteCount + EOLByteCount;

                fs.Seek(-lastrequestsize, SeekOrigin.End);

                long originallength = fs.Length;

                string tempurl = null;
                string tempcontent = null;
                char[] buffer = new char[urlsize];
                while (sr.Peek() != -1)
                {
                    sr.ReadBlock(buffer, 0, urlsize);

                    tempurl = new string(buffer);
                    //line return
                    for (int eolc = 0; eolc < EOLByteCount; eolc++)
                        sr.Read();


                    buffer = new char[contentsize];
                    sr.ReadBlock(buffer, 0, contentsize);
                    tempcontent = new string(buffer);
                    //line return
                    for (int eolc2 = 0; eolc2 < EOLByteCount; eolc2++)
                        sr.Read();
                }

                //wait for post response
                var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(tempcontent);
                var request = UnityWebRequest.Put(tempurl, bytes);
                request.method = "POST";
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-HTTP-Method-Override", "POST");
                request.SetRequestHeader("Authorization", CognitiveStatics.ApplicationKey);
                request.Send();
                yield return Sender.StartCoroutine(Sender.WaitForFullResponse(request, tempcontent, Sender.GenericPostFullResponse, false, false));

                //check internet access
                var headers = request.GetResponseHeaders();
                int responsecode = (int)request.responseCode;
                if (responsecode == 200)
                {
                    //check cvr header to make sure not blocked by capture portal
                    if (!headers.ContainsKey("cvr-request-time"))
                    {
                        if (failed != null)
                            failed.Invoke();
                        isuploadingfromcache = false;
                        request.Dispose();
                        yield break;
                    }
                }
                else
                {
                    if (failed != null)
                        failed.Invoke();
                    isuploadingfromcache = false;
                    request.Dispose();
                    yield break;
                }
                //ie, if successful, pop last request from data cache
                linesizes.Pop();
                linesizes.Pop();
                fs.SetLength(originallength - lastrequestsize);
                request.Dispose();
            }

            if (completed != null)
                completed.Invoke();
            isuploadingfromcache = false;
        }

        //uploads a single request from the file (1 line url, 1 line content). only called when a 200 is returned from a post request
        void UploadLocalFile()
        {
            if (linesizes.Count < 2) { return; }

            for (int i = 0; i < ReadLocalCacheCount; i++)
            {
                if (linesizes.Count < 2) { return; }
                int contentsize = linesizes.Pop();
                int urlsize = linesizes.Pop();

                int lastrequestsize = contentsize + urlsize + EOLByteCount + EOLByteCount;

                fs.Seek(-lastrequestsize, SeekOrigin.End);

                long originallength = fs.Length;

                string tempurl = null;
                string tempcontent = null;
                char[] buffer = new char[urlsize];
                while (sr.Peek() != -1)
                {                   
                    sr.ReadBlock(buffer, 0, urlsize);
                    
                    tempurl = new string(buffer);
                    //line return
                    for(int eolc = 0; eolc < EOLByteCount; eolc++)
                        sr.Read();
                    

                    buffer = new char[contentsize];
                    sr.ReadBlock(buffer, 0, contentsize);
                    tempcontent = new string(buffer);
                    //line return
                    for (int eolc2 = 0; eolc2 < EOLByteCount; eolc2++)
                        sr.Read();
                }

                fs.SetLength(originallength - lastrequestsize);
                LocalCachePost(tempurl, tempcontent);
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
            Sender.StartCoroutine(Sender.WaitForFullResponse(request, stringcontent, Sender.GenericPostFullResponse, true, true));

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
            Sender.StartCoroutine(Sender.WaitForFullResponse(request, stringcontent, Sender.GenericPostFullResponse,true, true));

            if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment(url + " " + stringcontent);
        }

        //used internally so uploading a file from cache doesn't trigger more files
        private static void LocalCachePost(string url, string stringcontent)
        {
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            var request = UnityWebRequest.Put(url, bytes);
            request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-HTTP-Method-Override", "POST");
            request.SetRequestHeader("Authorization", CognitiveStatics.ApplicationKey);
            request.Send();

            Sender.StartCoroutine(Sender.WaitForFullResponse(request, stringcontent, Sender.GenericPostFullResponse,false, false));

            if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment(url + " " + stringcontent);
        }
    }
}
