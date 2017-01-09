using System.Collections;
using System.Collections.Generic;

namespace CognitiveVR.Plugins
{
	/// <summary>
	/// A light wrapper around CognitiveVR.Transaction to provide some built-in characteristics for Session transactions
	/// </summary>
	public class SessionTransaction : TransactionBase<SessionTransaction>
	{
		public const double DEFAULT_TIMEOUT = 10.0 * 86400.0; // 10 days

		internal SessionTransaction() : base("Session", null) {}
		public new void begin(double timeout = DEFAULT_TIMEOUT, Transaction.TimeoutMode mode = Transaction.TimeoutMode.Any) { base.begin(timeout, mode); }
	}

	/// <summary>
	/// This CognitiveVR plugin provides a simple interface for instrumenting Session flow in an application.
	/// </summary>
	public class Session
	{
		/// <summary>
		/// Factory method for invoking CognitiveVRPlugins.SessionTransaction methods
		/// </summary>
		public static SessionTransaction Transaction()
		{
			return new SessionTransaction();
		}
	}
}

