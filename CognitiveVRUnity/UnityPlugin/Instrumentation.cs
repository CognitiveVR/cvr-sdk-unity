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
            InstrumentationSubsystem.SendTransactions();
        }
    }
}