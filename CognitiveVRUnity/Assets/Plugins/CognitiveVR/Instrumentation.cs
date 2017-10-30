using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace CognitiveVR
{
	/// <summary>
	/// For logging application telemetry
	/// </summary>
	public class Instrumentation
	{
		/// <summary>
		/// Factory method for invoking CognitiveVR.Transaction methods
		/// </summary>
		/// <param name="category">The transaction category</param>
		/// <param name="transactionId">Transaction id, if applicable - this is only REQUIRED in situation where multiple transactions in the same category may exist (read: be concurrently begun)</param> 
		public static Transaction Transaction(string category, string transactionId = null)
		{
            return new CognitiveVR.Transaction(category, transactionId);
		}

		/// <summary>
		/// Updates state information about a device
		/// </summary>
		/// <param name="state">A key-value object representing the device state we want to update. This can be a nested object structure.</param>
		public static void updateDeviceState(Dictionary<string, object> state) 
		{
            InstrumentationSubsystem.updateDeviceState(state);
		}

		/// <summary>
		/// Updates state information about a user
		/// </summary>
		/// <param name="state">A key-value object representing the user state we want to update. This can be a nested object structure.</param>
		public static void updateUserState(Dictionary<string, object> state) 
		{
            InstrumentationSubsystem.updateUserState(state);
		}

		/// <summary>
		/// Update a collection balance for the current entity
		/// </summary>
		/// <param name="name">The application-supplied name for the collection</param>
		/// <param name="balance">Current balance</param>
		/// <param name="balanceModification">The amount that the balance is changing by (if known)</param>
		/// <param name="isCurrency">If set to <c>true</c> the collection is treated as an in-app virtual currency</param>
		public static void updateCollection(string name, double balance, double balanceModification, bool isCurrency) 
		{
            InstrumentationSubsystem.updateCollection(name, balance, balanceModification, isCurrency);
		}

        public static void SetMaxTransactions(int max)
        {
            InstrumentationSubsystem.SetMaxTransactions(max);
        }

        /// <summary>
        /// manually send cached transactions
        /// this is also used when quiting the application
        /// </summary>
        public static void SendCachedTransactions()
        {
            InstrumentationSubsystem.SendCachedTransactions();
        }
    }
}