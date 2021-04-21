using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

//uploads session data from local cache during editor
//make sure to support uploading + reading in parallel
//CONSIDER 2 files? a 'writeable' file and a 'readable' file. move writeable to readable when 'idle'?
//OR queue caches instead of stacking them? if failed, put on end instead of back in position

namespace CognitiveVR
{
	public interface ICache
	{
		//how many requests are in the cache (url lines + content lines)
		int NumberOfBatches();

		//simple version of peek - just returns if there's anything there
		//false if file doesn't exist
		bool HasContent();

		//return true if there's content to 'get'. false if not
		//implementation may also move the streamreader/index
		bool PeekContent(ref string Destination, ref string body);

		//returns true if successfully saved, false otherwise
		bool WriteContent(string Destination, string body);

		//removes destination and body
		//implementation may be at 0, or just after peek content. may write new file before exiting
		void PopContent();

		//called when cache is closed. don't count on this happening!
		//could be used for InPlaceCache, which would be best for uploading from the editor
		void Close();
	}

	//reads contents of file ONCE at constructor. otherwise uses writer and list to queue through requests
	//pretty simple. lots of garbage generated. high memory requirement for large caches
	public class ListCacheReader : ICache
	{
		string filepath;
		List<string> data;

		public ListCacheReader(string cacheDirectory)
		{
			try
			{
				filepath = cacheDirectory + "data";
				data = new List<string>();
				if (!Directory.Exists(cacheDirectory))
					Directory.CreateDirectory(cacheDirectory);

				using (StreamReader sr = new StreamReader(filepath))
				{
					while (true)
					{
						string s = sr.ReadLine();
						if (s == null) { break; }
						data.Add(s);
					}
				}
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

		public bool HasContent()
		{
			return data.Count > 0;
		}

		public int NumberOfBatches()
		{
			return data.Count / 2;
		}

		//read lines from 0 index
		public bool PeekContent(ref string Destination, ref string body)
		{
			try
			{
				Destination = data[0];
				body = data[1];
				return true;
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
				return false;
			}
		}

		//pop from 0 index of data
		public void PopContent()
		{
			data.RemoveAt(0);
			data.RemoveAt(0);

			using (StreamWriter sw = new StreamWriter(filepath))
			{
				sw.BaseStream.Position = 0;
				foreach (var v in data)
				{
					sw.WriteLine(v);
				}
			}
		}

		public bool WriteContent(string Destination, string body)
		{
			data.Add(Destination);
			data.Add(body);
			//append to end, not overwrite
			using (StreamWriter sw = new StreamWriter(filepath, true))
			{
				sw.WriteLine(Destination);
				sw.WriteLine(body);
			}
			return true;
		}

		public void Close()
		{

		}
	}

	//no IO thread
	//opens streamreader whenever contents of cache need to be checked. inefficient, but OK for editor use
	public class BasicCacheReader : ICache
	{
		string filepath;

		//cache directory expected to be Application.persistentDataPath + "/c3dlocal/
		public BasicCacheReader(string cacheDirectory)
		{
			try
			{
				filepath = cacheDirectory + "data";
				if (!Directory.Exists(cacheDirectory))
					Directory.CreateDirectory(cacheDirectory);

			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

		public bool HasContent()
		{
			if (!System.IO.File.Exists(filepath)) { return false; }
			using (StreamReader sr = new StreamReader(filepath))
			{
				return sr.ReadLine() != null;
			}
		}

		public int NumberOfBatches()
		{
			using (StreamReader sr = new StreamReader(filepath))
			{
				int i = 0;
				while (sr.ReadLine() != null) { i++; }
				return i / 2;
			}
		}

		//read lines from start of file
		public bool PeekContent(ref string Destination, ref string body)
		{
			try
			{
				using (StreamReader sr = new StreamReader(filepath))
				{
					Destination = sr.ReadLine();
					body = sr.ReadLine();
				}
				return true;
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
				return false;
			}
		}

		//pop from 0 index
		public void PopContent()
		{
			using (StreamReader sr = new StreamReader(filepath))
			{
				sr.ReadLine();
				sr.ReadLine();

				//write to a new file. rename afterward
				using (StreamWriter sw = new StreamWriter(filepath + "2"))
				{
					while (true)
					{
						string s = sr.ReadLine();
						if (s == null) { break; }
						sw.WriteLine(s);
					}
				}
			}
			System.IO.File.Delete(filepath);
			System.IO.File.Move(filepath + "2", filepath);
		}

		public bool WriteContent(string Destination, string body)
		{
			//append to end, not overwrite
			using (StreamWriter sw = new StreamWriter(filepath, true))
			{
				sw.WriteLine(Destination);
				sw.WriteLine(body);
			}
			return true;
		}

		public void Close()
		{

		}
	}

	//try uploading each batch in data. if failed, append to data2. on close, replace data with data2
	//1 streamwriter, 1 streamreader
	//NOT FINISHED
	//CONSIDER IO thread
	public class InPlaceCacheReader : ICache
	{
		string filepath;
		StreamReader sr;
		StreamWriter sw;
		int startingLineCount;

		//cache directory expected to be Application.persistentDataPath + "/c3dlocal/
		public InPlaceCacheReader(string cacheDirectory)
		{
			try
			{
				filepath = cacheDirectory + "data";
				if (!Directory.Exists(cacheDirectory))
					Directory.CreateDirectory(cacheDirectory);

				using (StreamReader temp = new StreamReader(filepath))
				{
					int i = 0;
					while (sr.ReadLine() != null) { i++; }
					startingLineCount = i;
				}

				sr = new StreamReader(filepath);
				sw = new StreamWriter(filepath+"2");
				

			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

		public bool HasContent()
		{
			//TODO check this is correct
			return sr.BaseStream.Length > 0;
		}

		public int NumberOfBatches()
		{
			return startingLineCount / 2;
			//TODO best way of counting lines with existing open streamreader?
		}

		//reading from whatever current index is
		public bool PeekContent(ref string Destination, ref string body)
		{
			try
			{
				Destination = sr.ReadLine();
				body = sr.ReadLine();
				return true;
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
				return false;
			}
		}

		public void PopContent()
		{
			//simply don't write to data2
		}

		//write lines to data2
		public bool WriteContent(string Destination, string body)
		{
			sw.WriteLine(Destination);
			sw.WriteLine(body);
			return true;
		}

		public void Close()
		{
			if (sr != null) { sr.Dispose(); }
			if (sw != null) { sw.Dispose(); }

			System.IO.File.Delete(filepath);
			System.IO.File.Move(filepath + "2", filepath);
		}
	}
}