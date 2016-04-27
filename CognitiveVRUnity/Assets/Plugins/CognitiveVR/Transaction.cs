using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using CognitiveVR.External.MiniJSON; 

namespace CognitiveVR
{
	/// <summary>
	/// The Transaction class is a helper class used for sending information about a transaction to CognitiveVR.
	/// 
	/// Use the factory method CognitiveVR.Instrumentation.Transaction to create an instance of this class.
	/// </summary>
	public class Transaction
	{
		/// <summary>
		/// Designates the conditions by which a CognitiveVR.Transaction may timeout
		/// </summary>
		public enum TimeoutMode 
		{
			/// <summary>
			/// The transaction will be kept 'open' only by direct interactions with itself
			/// </summary>
			Transaction,

			/// <summary>
			/// The transaction will be kept 'open' by ANY transaction for the current device/user
			/// </summary>
			Any
		}

		/// <summary>
		/// Report any known state about the transaction in key-value pairs
		/// </summary>
		/// <returns>The transaction itself (to support a builder-style implementation)</returns>
		/// <param name="properties">A key-value object representing the transaction state we want to report. This can be a nested object structure.</param>
		public Transaction setProperties(Dictionary<string, object> properties)
		{
			if(null != properties)
			{
				foreach(KeyValuePair<string, object> kvp in properties)
					this._state.Add(kvp.Key, kvp.Value);
			}
			return this;
		}

		/// <summary>
		/// Report a single piece of known state about the transaction
		/// </summary>
		/// <returns>The transaction itself (to support a builder-style implementation)</returns>
		/// <param name="key">Key for transaction state property</param>
		/// <param name="value">Value for transaction state property</param>
		public Transaction setProperty(string key, object value)
		{
			this._state.Add(key, value);
			return this;
		}

		/// <summary>
		/// Send telemetry to report the beginning of a transaction, including any state properties which have been set.
		/// </summary>
		/// <param name="timeout">How long to keep the transaction 'open' without new activity</param>
		/// <param name="mode">The type of activity which will keep the transaction open</param>
		public void begin(double timeout = 0, TimeoutMode mode = TimeoutMode.Transaction)
		{
            InstrumentationSubsystem.beginTransaction(_category, (TimeoutMode.Any == mode) ? "ANY" : "TXN", timeout, _transactionId, _state);

            _state = new Dictionary<string, object>();
		}

		/// <summary>
		/// Send telemetry to report an update to a transaction, including any state properties which have been set.
		/// </summary>
		/// <param name="progress">A value between 1 and 99, which should increase between subsequent calls to update</param>
		public void update(int progress)
		{
            InstrumentationSubsystem.updateTransaction(_category, progress, _transactionId, _state);

            _state = new Dictionary<string, object>();
		}

		/// <summary>
		/// Send telemetry to report an end to a transaction, including any state properties which have been set.
		/// </summary>
		/// <param name="result">CognitiveVR.Constants.TXN_SUCCESS, CognitiveVR.Constants.TXN_ERROR, or any application defined string describing the result</param>
		public void end(string result = Constants.TXN_SUCCESS)
		{
            InstrumentationSubsystem.endTransaction(_category, result, _transactionId, _state);

			_state = new Dictionary<string, object>();
		}

		/// <summary>
		/// Send telemetry to report an instantaneous transaction, including any state properties which have been set.
		/// </summary>
		/// <param name="result">CognitiveVR.Constants.TXN_SUCCESS, CognitiveVR.Constants.TXN_ERROR, or any application defined string describing the result</param>
		public void beginAndEnd(string result = Constants.TXN_SUCCESS)
		{
            InstrumentationSubsystem.endTransaction(_category, result, _transactionId, _state);

			_state = new Dictionary<string, object>();
		}
		
		protected string _category = null;
		protected string _transactionId = null;
		protected Dictionary<string, object> _state = new Dictionary<string, object>();
		
		internal Transaction(string category, string transactionId = null) { this._category = category; this._transactionId = transactionId; }
	}

	/// <summary>
	/// A utility class to bridge between CognitiveVR.Transaction and plugin Transactions while still supporting a builder style implementation
	/// </summary>
	public abstract class TransactionBase<T> : CognitiveVR.Transaction where T : CognitiveVR.Transaction
	{
		public TransactionBase(string category, string transactionId) : base(category, transactionId) { }
		public new T setProperties(Dictionary<string, object> properties) { return (T) base.setProperties(properties); }
		public new T setProperty(string key, object value) { return (T) base.setProperty(key, value); }
	}
}
