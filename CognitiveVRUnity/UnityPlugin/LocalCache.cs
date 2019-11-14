using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace CognitiveVR
{
    public class LocalCache
    {
        static int EOLByteCount = 2;
        static string EnvironmentEOL;

        //set on constructor. can become disabled if there are any file errors or if preferences have local storage disabled
        internal static bool LocalStorageActive = false;

        static string localDataPath = Application.persistentDataPath + "/c3dlocal/data";
        static string localExitPollPath = Application.persistentDataPath + "/c3dlocal/exitpoll/";

        static StreamReader sr;
        static StreamWriter sw;
        static FileStream fs;
        
        //line sizes of contents, ignoring line breaks. line breaks added automatically from StreamWriter.WriteLine
        static Stack<int> linesizes = new Stack<int>();
        static int totalBytes = 0;

        internal LocalCache(NetworkManager network, string EOLCharacter)
        {
            //constructed from CognitiveVR_Manager enable. sets environment end of line character
            if (EnvironmentEOL == null && !string.IsNullOrEmpty(EOLCharacter))
            {
                EnvironmentEOL = EOLCharacter;
                EOLByteCount = System.Text.Encoding.UTF8.GetByteCount(EOLCharacter);
            }

            //open file streams
            //should listen for network destroy event
            if (sr != null) { sr.Close(); sr = null; }
            if (sw != null) { sw.Close(); sw = null; }
            if (fs != null) { fs.Close(); fs = null; }

            LocalStorageActive = CognitiveVR_Preferences.Instance.LocalStorage;

            if (!LocalStorageActive) { return; }
            try
            {
                if (!Directory.Exists(localExitPollPath))
                    Directory.CreateDirectory(localExitPollPath);

                fs = File.Open(localDataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                sr = new StreamReader(fs);
                sw = new StreamWriter(fs);
                //read all line sizes from data
                linesizes = new Stack<int>();
                while (sr.Peek() != -1)
                {
                    int lineLength = System.Text.Encoding.UTF8.GetByteCount(sr.ReadLine());
                    linesizes.Push(lineLength);
                    totalBytes += lineLength;
                }
            }
            catch (System.Exception e)
            {
                LocalStorageActive = false;
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// checks that local storage is active, stream writer is not null and cache size has enough free space
        /// </summary>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        internal bool CanAppend(string url, string content)
        {
            if (!LocalStorageActive) { return false; }
            if (sw == null)
            {
                Util.logError("LocalCache attempting to write request after streamwriter closed");
                return false;
            }
            int urlByteCount = System.Text.Encoding.UTF8.GetByteCount(url);
            int contentByteCount = System.Text.Encoding.UTF8.GetByteCount(content);
            if (CheckCacheSize(urlByteCount + contentByteCount))
            {
                return true;
            }
            return false;
        }

        //returns the total number of lines in the cache file
        internal int GetCacheLineCount()
        {
            return linesizes.Count;
        }

        //immediately abort uploading from cache and write this new session data to local cache
        internal bool Append(string url, string content)
        {
            if (!LocalStorageActive)
            {
                Util.logWarning("LocalCache could not append data. Local Cache is inactive");
                return false;
            }

            content = content.Replace('\n', ' ');

            int urlByteCount = System.Text.Encoding.UTF8.GetByteCount(url);
            int contentByteCount = System.Text.Encoding.UTF8.GetByteCount(content);

            try
            {
                sw.Write(url);
                sw.Write(EnvironmentEOL);
                linesizes.Push(urlByteCount);
                totalBytes += urlByteCount;

                sw.Write(content);
                sw.Write(EnvironmentEOL);
                linesizes.Push(contentByteCount);
                totalBytes += contentByteCount;

                sw.Flush();
            }
            catch (System.Exception e)
            {
                //turn off to avoid other errors
                LocalStorageActive = false;
                Debug.LogException(e);
                return false;
            }

            //write to cache file
            return true;
        }

        internal static bool GetExitpoll(string hookname, out string text)
        {
            try
            {
                if (File.Exists(localExitPollPath + hookname))
                {
                    text = File.ReadAllText(localExitPollPath + hookname);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            text = "";
            return false;
        }

        internal static void WriteExitpoll(string hookname, string text)
        {
            try
            {
                File.WriteAllText(localExitPollPath + hookname, text);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// returns true if there's enough space
        /// </summary>
        /// <param name="newBytes"></param>
        /// <returns></returns>
        private bool CheckCacheSize(int newBytes)
        {
            if (newBytes + totalBytes > CognitiveVR.CognitiveVR_Preferences.Instance.LocalDataCacheSize)
            {
                return false;
            }
            return true;
        }

        //returns false if network isuploadingfromcache
        //returns false if cache is empty
        internal bool CanReadFromCache()
        {
            if (NetworkManager.isuploadingfromcache) { return false; }
            if (linesizes.Count < 2) { return false; }
            return true;
        }

        internal bool CacheEmpty()
        {
            if (linesizes.Count < 2) { return true; }
            return false;
        }

        int successfulReponseNewCacheSize;

        //network running localcache coroutine to pull data out of file
        internal void GetCachedDataPoint(out string url, out string content)
        {
            int contentsize = linesizes.Pop();
            int urlsize = linesizes.Pop();

            int lastrequestsize = contentsize + urlsize + EOLByteCount + EOLByteCount;

            linesizes.Push(urlsize);
            linesizes.Push(contentsize);

            fs.Seek(-lastrequestsize, SeekOrigin.End);

            long originallength = fs.Length;

            successfulReponseNewCacheSize = (int)(originallength - lastrequestsize);

            url = "";
            content = "";
            
            char[] buffer = new char[urlsize];
            while (sr.Peek() != -1)
            {
                sr.ReadBlock(buffer, 0, urlsize);

                url = new string(buffer);
                //line return
                for (int eolc = 0; eolc < EOLByteCount; eolc++)
                    sr.Read();


                buffer = new char[contentsize];
                sr.ReadBlock(buffer, 0, contentsize);
                content = new string(buffer);
                //line return
                for (int eolc2 = 0; eolc2 < EOLByteCount; eolc2++)
                    sr.Read();
            }
        }

        //if the network recieves a successful cache cache web request response
        internal void SuccessfulResponse()
        {
            try
            {
                //pop the last url and contents from cache
                linesizes.Pop();
                linesizes.Pop();
                fs.SetLength(successfulReponseNewCacheSize);
            }
            catch(System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        //called from Network.OnDestroy
        internal void OnDestroy()
        {
            if (sr != null) { sr.Close(); sr = null; }
            if (sw != null) { sw.Close(); sw = null; }
            if (fs != null) { fs.Close(); fs = null; }
        }

        static FileInfo localDataInfo;
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
    }
}
