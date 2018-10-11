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

        private void OnDestroy()
        {
            if (sr != null) sr.Close();
            if (sw != null) { sw.Flush(); sw.Close(); }
            if (fs != null) fs.Close();
        }

        System.Collections.IEnumerator WaitForResponse(UnityWebRequest www, Response callback)
        {
            yield return new WaitUntil(() => www.isDone);
            if (callback != null)
            {
                callback.Invoke((int)www.responseCode, www.error, www.downloadHandler.text);
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
                //try to read from file
                if (CognitiveVR_Preferences.Instance.LocalStorage && File.Exists(localExitPollPath+hookname))
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
            }
        }

        System.Collections.IEnumerator WaitForFullResponse(UnityWebRequest www, string contents, FullResponse callback, bool allowLocalUpload)
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
        }

        void GenericPostFullResponse(string url, string content, int responsecode, string error, string text, bool allowLocalUpload)
        {
            if (responsecode == 200)
            {
                if (!allowLocalUpload) { return; }
                if (!CognitiveVR_Preferences.Instance.LocalStorage) { return; }
                //search through files and upload outstanding data + remove that file
                UploadLocalFile();
            }
            else
            {
                if (responsecode == 401) { Util.logWarning("Network Post Data response code is 401. Is APIKEY set?"); return; } //ignore if invalid auth api key
                if (responsecode == -1) { Util.logWarning("Network Post Data could not parse response code. Check upload URL"); return; } //ignore. couldn't parse response code, likely malformed url
                //write to file
                WriteRequestToFile(url, content);
            }
        }

        static int EOLByteCount = 2;
        static string EnvironmentEOL;
        static int ReadLocalCacheCount;

        //called on init to find all files not uploaded
        public static void InitLocalStorage(string environmentEOL)
        {
            ReadLocalCacheCount = CognitiveVR_Preferences.Instance.ReadLocalCacheCount;
            EnvironmentEOL = environmentEOL;
            EOLByteCount = System.Text.Encoding.UTF8.GetByteCount(environmentEOL);
            //EOLByteCount = eolByteCount;
            if (!CognitiveVR_Preferences.Instance.LocalStorage) { return; }
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
        
        void WriteRequestToFile(string url, string contents)
        {
            if (!CognitiveVR_Preferences.Instance.LocalStorage) { return; }
            
            contents = contents.Replace('\n', ' ');

            int urlByteCount = System.Text.Encoding.UTF8.GetByteCount(url);
            int contentByteCount = System.Text.Encoding.UTF8.GetByteCount(contents);

            if (urlByteCount + contentByteCount + totalBytes > CognitiveVR.CognitiveVR_Preferences.Instance.LocalDataCacheSize)
            {
                //cache size reached! skip writing data
                //Debug.Log("!!!!!!!!!!!!!cache reached");
                return;
            }

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

        /// <summary>
        /// Upload all data from local storage. will call completed after everything has been uploaded, failed if not connected to internet or local storage not enabled
        /// </summary>
        /// <param name="completedCallback"></param>
        /// <param name="failedCallback"></param>
        public static void UploadAllLocalData(System.Action completedCallback, System.Action failedCallback)
        {
            //upload from local storage
            if (!CognitiveVR_Preferences.Instance.LocalStorage) { if (failedCallback != null) { failedCallback.Invoke(); } return; }

            if (fs == null)
            {
                InitLocalStorage(System.Environment.NewLine);
            }

            Sender.StartCoroutine(Sender.ForceUploadLocalStorage(completedCallback,failedCallback));
        }

        System.Collections.IEnumerator ForceUploadLocalStorage(System.Action completed, System.Action failed)
        {
            bool hasFailed = false;
            while (linesizes.Count > 1)
            {
                //get contents from file
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
                    for (int eolc = 0; eolc < EOLByteCount; eolc++)
                        sr.Read();


                    buffer = new char[contentsize];
                    sr.ReadBlock(buffer, 0, contentsize);
                    tempcontent = new string(buffer);
                    //line return
                    for (int eolc2 = 0; eolc2 < EOLByteCount; eolc2++)
                        sr.Read();
                }

                fs.SetLength(originallength - lastrequestsize);

                //wait for post response
                var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(tempcontent);
                var request = UnityWebRequest.Put(tempurl, bytes);
                request.method = "POST";
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-HTTP-Method-Override", "POST");
                request.SetRequestHeader("Authorization", Constants.APIKey);
                yield return Sender.StartCoroutine(Sender.WaitForFullResponse(request, tempcontent, Sender.GenericPostFullResponse, false));

                //check internet access
                var headers = request.GetResponseHeaders();
                int responsecode = (int)request.responseCode;
                if (responsecode == 200)
                {
                    //check cvr header to make sure not blocked by capture portal
                    if (!headers.ContainsKey("cvr-request-time"))
                    {
                        hasFailed = true;
                        if (failed != null)
                            failed.Invoke();
                        yield break;
                    }
                }
                else
                {
                    hasFailed = true;
                    if (failed != null)
                        failed.Invoke();
                    yield break;
                }
            }

            if (!hasFailed)
            {
                if (completed != null)
                    completed.Invoke();
            }
        }

        //uploads a single local file from the queue. only called when a 200 is returned from a post request
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

        public static void GetExitPollQuestions(string url, string hookname, Response callback, float timeout = 3)
        {
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-HTTP-Method-Override", "GET");
            request.SetRequestHeader("Authorization", Constants.APIKey);
            request.Send();

            Sender.StartCoroutine(Sender.WaitForExitpollResponse(request, hookname, callback,timeout));
        }

        public static void Post(string url, string stringcontent)
        {
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);

            var request = UnityWebRequest.Put(url, bytes);
            request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-HTTP-Method-Override", "POST");
            request.SetRequestHeader("Authorization", Constants.APIKey);
            request.Send();

            Sender.StartCoroutine(Sender.WaitForFullResponse(request, stringcontent, Sender.GenericPostFullResponse,true));

            if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment(url + " " + stringcontent);
        }

        //used internally so uploading a file from cache doesn't trigger more files
        public static void LocalCachePost(string url, string stringcontent)
        {
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringcontent);
            var request = UnityWebRequest.Put(url, bytes);
            request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-HTTP-Method-Override", "POST");
            request.SetRequestHeader("Authorization", Constants.APIKey);
            request.Send();

            Sender.StartCoroutine(Sender.WaitForFullResponse(request, stringcontent, Sender.GenericPostFullResponse,false));

            if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                Util.logDevelopment(url + " " + stringcontent);
        }
    }
}
