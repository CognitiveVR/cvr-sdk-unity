using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D;
using System.IO;
using System.Text;

namespace Cognitive3D
{
    /// <summary>
    /// Local data cache stored as pairs of lines (url, then body) in two files.
    /// New data is appended to data_write; data_write is merged into data_read, which is uploaded from the end (newest first).
    /// </summary>
    internal class DualFileCache : ICache
    {
        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        //stack of line lengths of the READ file in bytes (excluding line breaks)
        List<int> readLineLengths = new List<int>();
        //number of bytes in the READ file (including line breaks)
        long readLineLengthTotal;
        //number of bytes in a line break
        int eolByteCount;

        FileStream read_filestream;
        StreamReader read_reader;
        StreamWriter read_writer;

        FileStream write_filestream;
        StreamWriter writer;

        //manually keep track of how many write batches there are, instead of constantly opening/closing filestreams
        int numberWriteBatches = 0;

        public DualFileCache(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            //only one instance can use the cache at a time. a second game instance on the same computer (for example, when testing multiplayer)
            //throws here and runs without a local cache, instead of writing into the same files and corrupting them
            try
            {
                read_filestream = new FileStream(path + "data_read", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                write_filestream = new FileStream(path + "data_write", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            }
            catch
            {
                Close();
                throw;
            }

            read_reader = new StreamReader(read_filestream, Utf8NoBom, true, 4096, true);
            read_writer = new StreamWriter(read_filestream, Utf8NoBom, 4096, true);
            writer = new StreamWriter(write_filestream, Utf8NoBom, 4096, true);
            eolByteCount = Utf8NoBom.GetByteCount(read_writer.NewLine);

            //get number of batches in write file
            numberWriteBatches = ReadAllLines(write_filestream).Count / 2;

            //copy all lines into data_write, then delete
            bool legacyDataExists = false;
            if (File.Exists(path + "data"))
            {
                legacyDataExists = true;
                using (var legacyReader = new StreamReader(path + "data"))
                {
                    while (true)
                    {
                        var url = legacyReader.ReadLine();
                        var content = legacyReader.ReadLine();
                        if (string.IsNullOrEmpty(url)) { break; }
                        if (string.IsNullOrEmpty(content)) { break; }
                        WriteContent(url, content);
                    }
                }
                File.Delete(path + "data");
            }

            //merge write file into read file (if there's content in the write file)
            //otherwise validate the read file and stack its line lengths
            if (legacyDataExists || numberWriteBatches > 0)
                MergeDataFiles();
            else
                RebuildReadFile(null);
        }

        public void Close()
        {
            if (read_filestream != null)
            {
                read_filestream.Dispose();
                read_filestream = null;
            }
            if (write_filestream != null)
            {
                write_filestream.Dispose();
                write_filestream = null;
            }
        }

        public bool HasContent()
        {
            if (read_filestream == null || !read_filestream.CanRead) { return false; }

            if (read_filestream.Length > 0)
            {
                return true;
            }

            //try to merge if write file has any contents
            if (numberWriteBatches > 0)
            {
                MergeDataFiles();
            }
            return read_filestream.Length > 0;
        }

        public int NumberOfBatches()
        {
            return readLineLengths.Count / 2;
        }

        public bool PeekContent(ref string Destination, ref string body, ref bool sendAsBytes)
        {
            //if there's content in data_read, do that
            //otherwise, if there's content in write, merge files (in HasContent)
            //else nothing to do
            if (!HasContent()) { return false; }

            //second attempt only happens after rebuilding the read file, which leaves only valid entries
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string tempDest;
                string tempBody;
                if (readLineLengths.Count > 1 && TryReadLastEntry(out tempDest, out tempBody) && IsValidEntry(tempDest, tempBody))
                {
                    Destination = tempDest;
                    body = tempBody;
                    sendAsBytes = tempDest.Contains("audio");
                    return true;
                }

                //the file doesn't match the tracked line lengths or the entry is corrupted (for example, written by multiple game instances)
                RebuildReadFile(null);
                if (readLineLengths.Count < 2) { return false; }
            }

            return false;
        }

        bool TryReadLastEntry(out string destination, out string body)
        {
            destination = null;
            body = null;
            try
            {
                long entryBytes = readLineLengths[readLineLengths.Count - 1] + readLineLengths[readLineLengths.Count - 2] + eolByteCount * 2;
                long entryStart = readLineLengthTotal - entryBytes;
                if (entryStart < 0 || readLineLengthTotal != read_filestream.Length) { return false; }

                read_filestream.Seek(entryStart, SeekOrigin.Begin);
                //the reader buffers ahead, so it must be cleared after moving the stream position
                read_reader.DiscardBufferedData();
                destination = read_reader.ReadLine();
                body = read_reader.ReadLine();
                return destination != null && body != null;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        void MergeDataFiles()
        {
            //read from 'writeable', append into 'readable'
            var writeLines = ReadAllLines(write_filestream);
            RebuildReadFile(writeLines);

            write_filestream.SetLength(0);
            write_filestream.Flush();
            numberWriteBatches = 0;
        }

        /// <summary>
        /// Rewrites the read file with only valid url/body pairs (plus any additional lines to append)
        /// and recalculates the tracked line lengths
        /// </summary>
        void RebuildReadFile(List<string> appendLines)
        {
            var lines = ReadAllLines(read_filestream);
            if (appendLines != null) { lines.AddRange(appendLines); }

            int droppedLines;
            var validLines = GetValidLines(lines, out droppedLines);
            if (droppedLines > 0)
            {
                Util.logWarning("Removed " + droppedLines + " corrupted line(s) from the local data cache");
            }

            read_filestream.SetLength(0);
            read_filestream.Position = 0;
            readLineLengths.Clear();
            readLineLengthTotal = 0;
            foreach (var line in validLines)
            {
                read_writer.WriteLine(line);
                int byteCount = Utf8NoBom.GetByteCount(line);
                readLineLengths.Add(byteCount);
                readLineLengthTotal += byteCount + eolByteCount;
            }
            read_writer.Flush();
            read_reader.DiscardBufferedData();
        }

        static List<string> ReadAllLines(FileStream stream)
        {
            var lines = new List<string>();
            stream.Position = 0;
            using (var reader = new StreamReader(stream, Utf8NoBom, true, 4096, true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines;
        }

        /// <summary>
        /// Returns the lines that form valid url/body pairs, in order. Skips empty lines, orphaned urls or bodies, and truncated lines
        /// </summary>
        internal static List<string> GetValidLines(List<string> lines, out int droppedLines)
        {
            var validLines = new List<string>(lines.Count);
            droppedLines = 0;
            int i = 0;
            while (i < lines.Count)
            {
                if (i + 1 < lines.Count && IsValidEntry(lines[i], lines[i + 1]))
                {
                    validLines.Add(lines[i]);
                    validLines.Add(lines[i + 1]);
                    i += 2;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(lines[i])) { droppedLines++; }
                    i++;
                }
            }
            return validLines;
        }

        internal static bool IsValidEntry(string destination, string body)
        {
            if (!IsUrl(destination)) { return false; }
            if (string.IsNullOrWhiteSpace(body) || IsUrl(body)) { return false; }

            //audio is uploaded as base64 bytes
            if (destination.Contains("audio")) { return true; }

            //everything else is json. checking both ends catches lines that were cut off
            string trimmed = body.Trim();
            return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) || (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
        }

        static bool IsUrl(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) { return false; }
            return line.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase) || line.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase);
        }

        public void PopContent()
        {
            if (readLineLengths.Count < 2)
            {
                //this sometimes gets called 1 extra time after receiving a response
                //happens when sending new data - likely starting to read from the cache even though final cached data point is already being uploaded
                return;
            }
            if (read_filestream == null) { return; }

            try
            {
                int bodyBytesToRemove = readLineLengths[readLineLengths.Count - 1];
                int urlBytesToRemove = readLineLengths[readLineLengths.Count - 2];
                readLineLengths.RemoveAt(readLineLengths.Count - 1);
                readLineLengths.RemoveAt(readLineLengths.Count - 1);
                readLineLengthTotal -= (bodyBytesToRemove + urlBytesToRemove + eolByteCount * 2);

                read_filestream.SetLength(readLineLengthTotal);
                read_filestream.Seek(0, SeekOrigin.End);
                read_filestream.Flush();
                read_reader.DiscardBufferedData();
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
            if (write_filestream == null) { return false; }
            if (Destination == null || body == null) { return false; }

            // Sanitize: cache format requires exactly 2 lines per entry.
            // Any embedded newlines would corrupt the line-pair structure.
            bool hadNewlines =
                Destination.Contains("\n") || Destination.Contains("\r") ||
                body.Contains("\n") || body.Contains("\r");
            if (hadNewlines)
            {
                Util.logWarning("Newline characters detected in cache entry data. Newlines will be stripped from Destination/body before writing to the local data cache. This may indicate an upstream formatting issue.");
            }

            Destination = Destination.Replace("\n", "").Replace("\r", "");
            body = body.Replace("\n", "").Replace("\r", "");

            if (!IsValidEntry(Destination, body))
            {
                Util.logWarning("Skipped writing an invalid entry to the local data cache: " + Destination);
                return false;
            }

            //always append. the file can also be written outside this class (android plugin)
            writer.BaseStream.Seek(0, SeekOrigin.End);
            writer.WriteLine(Destination);
            writer.WriteLine(body);
            writer.Flush();
            numberWriteBatches++;
            return true;
        }

        public bool WriteContent(string content)
        {
            if (write_filestream == null) { return false; }

            // For content that contains both URL and Body together (used for android plugin)
            // Pairs are validated when the write file is merged into the read file
            writer.BaseStream.Seek(0, SeekOrigin.End);
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

            return (float)totalBytes / Cognitive3D_Preferences.Instance.LocalDataCacheSize;
        }
    }
}
