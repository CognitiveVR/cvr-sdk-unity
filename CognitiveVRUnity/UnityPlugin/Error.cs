namespace CognitiveVR
{
	/// <summary>
	/// All of the errors the application may see from CognitiveVR
	/// </summary>
	public enum Error
	{
		/// <summary>
		/// Success (no error)
		/// </summary>
		Success = 0,
		
		/// <summary>
		/// Generic error
		/// </summary>
		Generic = -1,
		
		/// <summary>
		/// CognitiveVR has not been initialized
		/// </summary>
		NotInitialized = -2,
		
		/// <summary>
		/// CognitiveVR has already been initialized
		/// </summary>
		AlreadyInitialized = -3,
		
		/// <summary>
		/// Invalid arguments passed into a function
		/// </summary>
		InvalidArgs = -4,
		
		/// <summary>
		/// Invalid configuation prior to initialization
		/// </summary>
		MissingId = -5,
		
		/// <summary>
		/// A web request timed out
		/// </summary>
		RequestTimedOut = -6,
		LastKnown = RequestTimedOut,

		/// <summary>
		/// Occurs when an error string cannot be parsed into a proper error
		/// </summary>
		Unknown = -99
	}
}

public static class ErrorExtension
{
    [System.Obsolete("ErrorExtension.toCognitiveVRError() is no longer used")]
	public static CognitiveVR.Error toCognitiveVRError(this string value)
	{
		int intVal;
		if(int.TryParse(value, out intVal))
		{
			if(intVal <= (int) CognitiveVR.Error.Success && intVal >= (int) CognitiveVR.Error.LastKnown)
			{
				return (CognitiveVR.Error) intVal;
			}
		}
		
		return CognitiveVR.Error.Unknown;
	}
}

