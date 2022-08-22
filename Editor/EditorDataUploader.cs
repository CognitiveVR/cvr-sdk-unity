using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

namespace CognitiveVR
{
    public class EditorDataUploader
	{
		//limit number of attempted uploads to how much data is in cache
		//otherwise, could have unending loop in editor (esp. if invalid data)
		int numberOfBatches;
		int attemptedUploads;

		ICache cacheSource;
		public EditorDataUploader(ICache cache)
        {
			cacheSource = cache;
			numberOfBatches = cache.NumberOfBatches();
			EditorApplication.update += Editor_Update;
		}

		UnityWebRequest uploadRequest;

        void Editor_Update()
        {
			if (uploadRequest == null)
			{
				string destination = string.Empty;
				string content = string.Empty;
				if (cacheSource.PeekContent(ref destination, ref content))
				{
					if (!string.IsNullOrEmpty(destination))
					{
						//wait for post response
						var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(content);
						uploadRequest = UnityWebRequest.Put(destination, bytes);
						uploadRequest.method = "POST";
						uploadRequest.SetRequestHeader("Content-Type", "application/json");
						uploadRequest.SetRequestHeader("X-HTTP-Method-Override", "POST");
						uploadRequest.SetRequestHeader("Authorization", "APIKEY:DATA " + CognitiveVR_Preferences.Instance.ApplicationKey);
						uploadRequest.Send();
						attemptedUploads++;

						if (CognitiveVR_Preferences.Instance.EnableDevLogging)
							Util.logDevelopment("EDITOR Upload From Cache " + destination + " " + content);
					}
				}
			}
			else if (uploadRequest.isDone)
            {
				int responseCode = (int)uploadRequest.responseCode;
				var headers = uploadRequest.GetResponseHeaders();
				bool hasRequestTimeHeader = false;
				if (headers != null)
					hasRequestTimeHeader = headers.ContainsKey("cvr-request-time");
				if (!hasRequestTimeHeader)
                {
					//request captured by portal
					responseCode = 307;
				}

				if (responseCode == 500)
                {
					//IMPROVEMENT check for invalid json format. if invalid, mailto support
                }

				if (responseCode == 200)
				{
					cacheSource.PopContent();
				}
				else
				{
					//pop from cache + write back to cache. cycles data to not get stuck
					string destination = string.Empty;
					string content = string.Empty;
					if (cacheSource.PeekContent(ref destination, ref content))
                    {
						cacheSource.PopContent();
						cacheSource.WriteContent(destination, content);
                    }
				}

				uploadRequest = null;

				if (attemptedUploads > numberOfBatches)
                {
					//everything has been attempted to be uploaded
					Util.logDebug("Editor attempted to upload everything");
					EditorApplication.update -= Editor_Update;
					cacheSource.Close();
				}
				if (!cacheSource.HasContent())
                {
					Util.logDebug("Editor has no more session data to upload");
					EditorApplication.update -= Editor_Update;
					cacheSource.Close();
				}
            }
        }
	}
}