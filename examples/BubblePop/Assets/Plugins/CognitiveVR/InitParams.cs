namespace CognitiveVR 
{
	public delegate void NotificationListener(string message, bool wasLaunchedBy);

	/// <summary>
	/// The InitParams class is a helper class used during initialization of CognitiveVR.
	/// 
	/// Use the factory method CognitiveVR.InitParams.create to create an instance of this class.
	/// </summary>
	public class InitParams 
	{
		public NotificationListener OnNotification { get; set; }

		internal string customerId { get; private set; }
		internal EntityInfo deviceInfo { get; private set; }
		internal EntityInfo userInfo { get; private set; }
		internal int requestTimeout { get; private set; }
		internal string host { get; private set; }
		internal bool logEnabled { get; private set; }
		internal string notificationHost { get; private set; }
		internal int notificationSmallIcon { get; private set; }
		internal bool notificationAlwaysPost { get; private set; }
		internal bool notificationDisableAutoClear { get; private set; }

		/// <summary>
		/// Create a CognitiveVR.InitParams with the given parameters. 
		/// 
		/// <b>Note:</b>You should be able to use C# 'named parameters' so that you only need to specify the values that you want to override.
		/// </summary>
		/// <param name="customerId">CognitiveVR customer id (contact CognitiveVR if you don't have this)</param>
		/// <param name="deviceInfo">Initial device info, if available</param>
		/// <param name="userInfo">Initial user info, if available</param>
		/// <param name="requestTimeout">The timeout value to use for outgoing http requests</param>
		/// <param name="host">Host setting (generally reserved for CognitiveVR developers)</param>
		/// <param name="logEnabled">If set to <c>true</c> logging enabled</param>
		public static InitParams create(
			string customerId,
			EntityInfo deviceInfo = null,
			EntityInfo userInfo = null,
			int requestTimeout = Constants.DEFAULT_REQUEST_TIMEOUT,
			string host = "https://data.cognitivevr.io",
			bool logEnabled = false,
			string notificationHost = "https://notification.cognitivevr.io",
			bool notificationDisableAutoClear = false,
			int notificationSmallIcon = 0,
			bool notificationAlwaysPost = false) 
		{
			InitParams initParams = new InitParams();

			if(null == deviceInfo)
				deviceInfo = EntityInfo.createDeviceInfo();
			if(null == userInfo)
				userInfo = EntityInfo.createUserInfo(null);

			initParams.customerId = customerId;
			initParams.deviceInfo = deviceInfo;
			initParams.userInfo = userInfo;
			initParams.requestTimeout = requestTimeout;
			initParams.host = host;
			initParams.logEnabled = logEnabled;
			initParams.notificationHost = notificationHost;
			initParams.notificationSmallIcon = notificationSmallIcon;
			initParams.notificationAlwaysPost = notificationAlwaysPost;
			initParams.notificationDisableAutoClear = notificationDisableAutoClear;

			return initParams;
		}

		private InitParams() {}
	}
}
