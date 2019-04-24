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
		None = 0,
		
		/// <summary>
		/// CognitiveVR has not been initialized
		/// </summary>
		NotInitialized,
		
		/// <summary>
		/// CognitiveVR has already been initialized
		/// </summary>
		AlreadyInitialized,
		
		/// <summary>
		/// Invalid arguments passed into a function
		/// </summary>
		InvalidArgs
	}
}