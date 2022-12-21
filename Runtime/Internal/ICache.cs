using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

//uploads session data from local cache during editor
//make sure to support uploading + reading in parallel
//CONSIDER 2 files? a 'writeable' file and a 'readable' file. move writeable to readable when 'idle'?
//OR queue caches instead of stacking them? if failed, put on end instead of back in position

namespace Cognitive3D
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

		bool CanWrite(string destination, string content);

		//returns 0-1 indicating how much space is available based on the maximum cache size. 1 is full
		float GetCacheFillAmount();
	}
}