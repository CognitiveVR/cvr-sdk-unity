namespace Cognitive3D
{
	/// <summary>
	/// All of the errors the application may see from Cognitive3D
	/// </summary>
	public enum Error
	{
		/// <summary>
		/// Success (no error)
		/// </summary>
		None = 0,
		
		/// <summary>
		/// Cognitive3D has not been initialized
		/// </summary>
		NotInitialized,
		
		/// <summary>
		/// Cognitive3D has already been initialized
		/// </summary>
		AlreadyInitialized,
		
		/// <summary>
		/// Invalid arguments passed into a function
		/// </summary>
		InvalidArgs
	}
}