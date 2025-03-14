﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D;
using System.IO;
using System.Text;

namespace Cognitive3D
{
    internal class DualFileCache : ICache
    {
        string eol_char = System.Environment.NewLine;
        //stack of line lengths of the READ file (excluding line breaks)
        List<int> readLineLengths = new List<int>();
        //number of characters (including line ends)
        int readLineLengthTotal;

        FileStream read_filestream;
        StreamReader read_reader;
        StreamWriter read_writer;

        FileStream write_filestream;
        StreamWriter writer;

        public DualFileCache(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            read_filestream = new FileStream(path + "data_read", FileMode.OpenOrCreate);
            write_filestream = new FileStream(path + "data_write", FileMode.OpenOrCreate);

            //get number of batches in write file
            var reader2 = new StreamReader(write_filestream);
            int i = 0;
            while (reader2.ReadLine() != null) { i++; }
            numberWriteBatches = i / 2;
            reader2.Close();
            write_filestream.Close();
            write_filestream = new FileStream(path + "data_write", FileMode.OpenOrCreate);

            //merge write file into read file (if there's content in the write file)

            if (read_reader == null)
                read_reader = new StreamReader(read_filestream);

            if (writer == null)
                writer = new StreamWriter(write_filestream);

            //copy all lines into data_write, then delete
            bool legacyDataExists = false;
            if (File.Exists(path + "data"))
            {
                legacyDataExists = true;
                var legacyReader = new StreamReader(path + "data");
                writer.BaseStream.Seek(0, SeekOrigin.End);
                while (true)
                {
                    var url = legacyReader.ReadLine();
                    var content = legacyReader.ReadLine();
                    if (string.IsNullOrEmpty(url)) { break; }
                    if (string.IsNullOrEmpty(content)) { break; }
                    WriteContent(url, content);
                }
                writer.Flush();
                legacyReader.Dispose();
                File.Delete(path + "data");
            }

            if (legacyDataExists || numberWriteBatches > 0)
                MergeDataFiles();

            //stack lines from reader
            readLineLengths.Clear();
            readLineLengthTotal = 0;
            read_reader.BaseStream.Position = 0;
            while (true)
            {
                var s = read_reader.ReadLine();
                if (s == null) { break; }
                readLineLengths.Add(s.Length);
                readLineLengthTotal += s.Length + eol_char.Length;
            }
        }

        public void Close()
        {
            if (read_filestream != null)
            {
                read_filestream.Dispose();
                read_filestream.Close();
            }
            if (write_filestream != null)
            {
                write_filestream.Dispose();
                write_filestream.Close();
            }
        }

        public bool HasContent()
        {
            if (read_reader == null) { Debug.LogError("has content reader null"); }
            if (read_reader.BaseStream == null) { Debug.LogError("has content base stream null"); }

            if (read_reader.BaseStream != null && read_reader.BaseStream.CanRead)
            {
                if (read_reader.BaseStream.Length > 0)
                {
                    return true;
                }

                //try to merge if write file has any contents
                if (numberWriteBatches > 0)
                {
                    MergeDataFiles();
                }
                return read_reader.BaseStream.Length > 0;
            }
            return false;
        }

        public int NumberOfBatches()
        {
            var currentPosition = 0L;

            int i = 0;
            //save current posiiton
            if (read_reader.BaseStream.CanRead)
            {
                currentPosition = read_reader.BaseStream.Position;

                //reset reader to start. read all lines
                read_reader.BaseStream.Position = 0;
                while (read_reader.ReadLine() != null) { i++; }

                //return to current position
                read_reader.BaseStream.Position = currentPosition;
            }
            //return batch number
            return i / 2;
        }

        //manually keep track of how many write batches there are, instead of constantly opening/closing filestreams
        int numberWriteBatches = 0;

        public bool PeekContent(ref string Destination, ref string body)
        {
            //if there's content in data_read, do that
            //otherwise, if there's content in write, merge files
            //else nothing to do

            string tempDest = string.Empty;
            string tempBody = string.Empty;

            if (HasContent())
            {
                if (readLineLengths.Count > 1)
                {
                    int offset = readLineLengths[readLineLengths.Count - 1] + readLineLengths[readLineLengths.Count - 2] + eol_char.Length * 2;
                    read_reader.BaseStream.Seek(-offset, SeekOrigin.End);

                    try
                    {
                        tempDest = read_reader.ReadLine();
                        tempBody = read_reader.ReadLine();

                        if (!string.IsNullOrWhiteSpace(tempDest) && !string.IsNullOrWhiteSpace(tempBody))
                        {
                            Destination = tempDest;
                            body = tempBody;
                            return true;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    // If there aren't enough lines in readLineLengths, return false
                    return false;
                }
            }
            else if (numberWriteBatches > 0)
            {
                MergeDataFiles();

                // Retry reading the content after merging
                if (readLineLengths.Count > 1)
                {
                    int offset = readLineLengths[readLineLengths.Count - 1] + readLineLengths[readLineLengths.Count - 2] + eol_char.Length * 2;
                    read_reader.BaseStream.Seek(-offset, SeekOrigin.End);

                    try
                    {
                        tempDest = read_reader.ReadLine();
                        tempBody = read_reader.ReadLine();

                        if (!string.IsNullOrWhiteSpace(tempDest) && !string.IsNullOrWhiteSpace(tempBody))
                        {
                            Destination = tempDest;
                            body = tempBody;
                            return true;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            // Return false if no valid data could be read
            return false;
        }

        public void RemoveEmptyLinesFromFile()
        {
            // Read all lines from the read file
            List<string> lines = new List<string>();
            string line;
            while ((line = read_reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
            }

            // Reset the file stream to the beginning before writing back
            read_writer.BaseStream.SetLength(0);
            read_writer.BaseStream.Seek(0, SeekOrigin.Begin);

            // Write the non-empty lines back into the file
            foreach (var nonEmptyLine in lines)
            {
                read_writer.WriteLine(nonEmptyLine);
            }

            read_writer.Flush();
        }

        void MergeDataFiles()
        {
            //read from 'writeable', write into 'readable'
            var write_reader = new StreamReader(write_filestream);
            write_reader.BaseStream.Position = 0;
            read_writer = new StreamWriter(read_filestream);
            read_writer.BaseStream.Seek(0, SeekOrigin.End);
            while (true)
            {
                var s = write_reader.ReadLine();
                if (s == null) { break; }
                // Skip empty or whitespace-only lines
                if (!string.IsNullOrWhiteSpace(s))
                {
                    read_writer.WriteLine(s);
                }
            }
            read_writer.Flush();
            write_filestream.SetLength(0);
            write_filestream.Flush();
            numberWriteBatches = 0;

            readLineLengths.Clear();
            readLineLengthTotal = 0;
            read_reader.BaseStream.Position = 0;

            // Remove empty lines from the file after merging
            RemoveEmptyLinesFromFile();
        }

        public void PopContent()
        {
            if (readLineLengths.Count == 0)
            {
                //this sometimes gets called 1 extra time after receiving a response
                //happens when sending new data - likely starting to read from the cache even though final cached data point is already being uploaded
                return;
            }

            try
            {
                int bodyCharsToRemove = readLineLengths[readLineLengths.Count - 1];
                int urlCharsToRemove = readLineLengths[readLineLengths.Count - 2];
                readLineLengths.RemoveAt(readLineLengths.Count - 1);
                readLineLengths.RemoveAt(readLineLengths.Count - 1);
                readLineLengthTotal -= (bodyCharsToRemove + urlCharsToRemove + eol_char.Length + eol_char.Length);

                read_reader.BaseStream.SetLength(readLineLengthTotal);
                read_reader.BaseStream.Seek(0, SeekOrigin.End);
                read_reader.BaseStream.Flush();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        bool displayedSizeWarning = false;
        public bool CanWrite(string Destination, string body)
        {
            if (write_filestream == null) { return false; }
            if (read_filestream == null) { return false; }

            long totalBytes = read_filestream.Length + write_filestream.Length;
            int newBytes = System.Text.Encoding.UTF8.GetByteCount(Destination) + System.Text.Encoding.UTF8.GetByteCount(body);

            if (newBytes + totalBytes > Cognitive3D.Cognitive3D_Preferences.Instance.LocalDataCacheSize)
            {
                if (!displayedSizeWarning)
                {
                    displayedSizeWarning = true;
                    Debug.LogError("[Cognitive3D] Data Cache reached size limit!");
                }
                return false;
            }
            displayedSizeWarning = false;
            return true;
        }

        public bool CanWrite(string content)
        {
            if (write_filestream == null) { return false; }
            if (read_filestream == null) { return false; }

            long totalBytes = read_filestream.Length + write_filestream.Length;
            int newBytes = System.Text.Encoding.UTF8.GetByteCount(content);

            if (newBytes + totalBytes > Cognitive3D.Cognitive3D_Preferences.Instance.LocalDataCacheSize)
            {
                if (!displayedSizeWarning)
                {
                    displayedSizeWarning = true;
                    Debug.LogError("[Cognitive3D] Data Cache reached size limit!");
                }
                return false;
            }
            displayedSizeWarning = false;
            return true;
        }

        public bool WriteContent(string Destination, string body)
        {
            writer.WriteLine(Destination);
            writer.WriteLine(body);
            writer.Flush();
            numberWriteBatches++;
            return true;
        }

        public bool WriteContent(string content)
        {
            // For content that contains both URL and Body together (used for android plugin)
            writer.WriteLine(content);
            writer.Flush();
            numberWriteBatches++;
            return true;
        }

        public float GetCacheFillAmount()
        {
            long totalBytes = 0;

            if (Cognitive3D_Preferences.Instance.LocalDataCacheSize <= 0) { return 1; }
            if (write_filestream != null) { totalBytes += write_filestream.Length; }
            if (read_filestream != null) { totalBytes += read_filestream.Length; }

            return totalBytes / Cognitive3D_Preferences.Instance.LocalDataCacheSize;
        }
    }
}
