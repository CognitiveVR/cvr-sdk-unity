using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace Cognitive3D
{
    //shouldn't have any static fields!
    public class LocalCache : ICache
    {
        static int EOLByteCount = System.Text.Encoding.UTF8.GetByteCount(System.Environment.NewLine);
        static string EnvironmentEOL = System.Environment.NewLine;

        //set on constructor. can become disabled if there are any file errors or if preferences have local storage disabled
        //internal static bool LocalStorageActive = false;

        static string localDataPath;// = Application.persistentDataPath + "/c3dlocal/data";
        //static string localExitPollPath = Application.persistentDataPath + "/c3dlocal/exitpoll/";

        static StreamReader sr;
        static StreamWriter sw;
        static FileStream fs;
        
        //TODO compare readline and readblock performance + garbage
        //line sizes of contents, ignoring line breaks. line breaks added automatically from StreamWriter.WriteLine
        static Stack<int> linesizes = new Stack<int>();
        static int totalBytes = 0;

        public LocalCache(string directoryPath)
        {
            localDataPath = directoryPath;// + "data/";

            //open file streams
            //should listen for network destroy event
            if (sr != null) { sr.Close(); sr = null; }
            if (sw != null) { sw.Close(); sw = null; }
            if (fs != null) { fs.Close(); fs = null; }

            if (!Cognitive3D_Preferences.Instance.LocalStorage) { return; }
            try
            {
                if (!Directory.Exists(localDataPath))
                    Directory.CreateDirectory(localDataPath);

                fs = File.Open(localDataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                sr = new StreamReader(fs);
                sw = new StreamWriter(fs);

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
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// checks that local storage is active, stream writer is not null and cache size has enough free space
        /// </summary>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public bool CanWrite(string url, string content)
        {
            if (!Cognitive3D_Preferences.Instance.LocalStorage) { return false; }
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

        public int NumberOfBatches()
        {
            return linesizes.Count / 2;
        }

        //immediately abort uploading from cache and write this new session data to local cache
        //internal bool Append(string url, string content)
        public bool WriteContent(string url, string content)
        {
            if (!Cognitive3D_Preferences.Instance.LocalStorage)
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
                //LocalStorageActive = false;
                Debug.LogException(e);
                return false;
            }

            //write to cache file
            return true;
        }

        /// <summary>
        /// returns true if there's enough space
        /// </summary>
        /// <param name="newBytes"></param>
        /// <returns></returns>
        private bool CheckCacheSize(int newBytes)
        {
            if (newBytes + totalBytes > Cognitive3D.Cognitive3D_Preferences.Instance.LocalDataCacheSize)
            {
                return false;
            }
            return true;
        }

        //IMPROVEMENT shouldn't have reference to network manager. CanReadFromCache should only be called when NetworkManager knows it can read
        //returns false if network isuploadingfromcache
        //returns false if cache is empty
        internal bool CanReadFromCache()
        {
            //if (NetworkManager.isuploadingfromcache) { return false; }
            if (linesizes.Count < 2) { return false; }
            return true;
        }

        internal bool CacheEmpty()
        {
            if (linesizes.Count < 2) { return true; }
            return false;
        }

        public bool HasContent()
        {
            if (linesizes.Count > 0) { return true; }
            return false;
        }

        int successfulReponseNewCacheSize;

        //network running localcache coroutine to pull data out of file
        //internal void GetCachedDataPoint(out string url, out string content)
        public bool PeekContent(ref string url, ref string content)
        {
            url = "";
            content = "";
            if (!HasContent())
                return false;

            //pop the line sizes off the stack to figure out how many characters to pull from the local data cache
            int contentsize = linesizes.Pop();
            int urlsize = linesizes.Pop();
            int lastrequestsize = contentsize + urlsize + EOLByteCount + EOLByteCount;

            //put the line sizes back. if !200 response, this will still have the response
            linesizes.Push(urlsize);
            linesizes.Push(contentsize);

            //start reading characters from data
            long newSeekPosition = fs.Seek(-lastrequestsize, SeekOrigin.End);
            long originallength = fs.Length;
            successfulReponseNewCacheSize = (int)(originallength - lastrequestsize);

            //Debug.Log("fs.Length " + fs.Length + " last request size " + lastrequestsize + " new position " + (fs.Length - lastrequestsize) + " new seek position " + newSeekPosition);
            //Debug.Log("content size " + contentsize);
            //Debug.Log("sr " + sr.Peek());
            //Debug.Log("sr read to end " + sr.read());
            //Debug.Log("sr read to end " + sr.ReadToEnd());

            //while (sr.Peek() != -1)
            {
                //read all the characters for the url
                char[] buffer = new char[urlsize];
                sr.ReadBlock(buffer, 0, urlsize);
                url = new string(buffer);
                //line return
                for (int eolc = 0; eolc < EOLByteCount; eolc++)
                    sr.Read();

                //read all the characters for the body
                buffer = new char[contentsize];
                sr.ReadBlock(buffer, 0, contentsize);
                content = new string(buffer);
                //line return
                for (int eolc2 = 0; eolc2 < EOLByteCount; eolc2++)
                    sr.Read();
            }
            return true;
        }

        //if the network recieves a successful cache cache web request response
        //internal void SuccessfulResponse()
        public void PopContent()
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

        public void Close()
        {
            OnDestroy();
        }

        //called from Network.OnDestroy
        internal void OnDestroy()
        {
            if (sr != null) { sr.Close(); sr = null; }
            if (sw != null) { try { sw.Close(); sw = null; } catch { sw.Dispose(); Util.logDebug("LocalCache::OnDestroy stream writer already closed!"); } }
            if (fs != null) { fs.Close(); fs = null; }
        }

        static FileInfo localDataInfo;

        /// <summary>
        /// returns 0.0-1.0 for the percent the local cache is full
        /// </summary>
        /// <param name="mute">optionally log the filesize and percent to the console</param>
        /// <returns></returns>
        public static float GetLocalStorage(bool mute = false)
        {
            float percent = 0;
            try
            {
                if (localDataInfo == null)
                    localDataInfo = new FileInfo(localDataPath);
                localDataInfo.Refresh();
                if (!localDataInfo.Exists) { return 0; }
                int length = (int)localDataInfo.Length;
                percent = length / (float)Cognitive3D_Preferences.Instance.LocalDataCacheSize;

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

        /// <summary>
        /// opens the cached local data at the persistent data path
        /// </summary>
        public static void OpenLocalStorageDirectory()
        {
            Application.OpenURL(Application.persistentDataPath + "/c3dlocal/");
        }
    }
}
