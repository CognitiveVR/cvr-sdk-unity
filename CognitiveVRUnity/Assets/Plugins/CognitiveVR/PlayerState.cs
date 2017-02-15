using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CognitiveVR.Plugins
{
	/// <summary>
	/// A light wrapper around CognitiveVR.Transaction to provide some built-in characteristics for Purchase transactions
	/// </summary>
    [System.Obsolete("PlayerStateTransaction is no longer used")]
	public class PlayerStateTransaction : TransactionBase<PlayerStateTransaction>
	{
		internal PlayerStateTransaction(string transactionId) : base("Player State", transactionId) {}

        /// <summary>
        /// Reports the game time this snapshot was taken
        /// </summary>
        /// <returns>The transaction itself (to support a builder-style implementation)</returns>
        public PlayerStateTransaction setTime(float time) { setProperty("time", time); return this; }

        /// <summary>
        /// Reports the position of the player
        /// </summary>
        /// <returns>The transaction itself (to support a builder-style implementation)</returns>
        public PlayerStateTransaction setPosition(UnityEngine.Vector3 position){setProperty("position", position);return this;}
        
        /// <summary>
		/// Reports the world position the player was looking at
		/// </summary>
		/// <returns>The transaction itself (to support a builder-style implementation)</returns>
        public PlayerStateTransaction setGazePoint(UnityEngine.Vector3 point) { setProperty("gazePoint", point); return this; }
        
        /// <summary>
		/// Reports the direction the player was facing
		/// </summary>
		/// <returns>The transaction itself (to support a builder-style implementation)</returns>
        public PlayerStateTransaction setGazeDirection(UnityEngine.Vector3 dir) { setProperty("gazeDirection", dir); return this; }
        
        /// <summary>
		/// Reports the center of the room in which the player was confined
		/// </summary>
		/// <returns>The transaction itself (to support a builder-style implementation)</returns>
        public PlayerStateTransaction setRoomPosition(UnityEngine.Vector3 position) { setProperty("roomPosition", position); return this; }
	}

    /// <summary>
    /// This CognitiveVR plugin provides a simple interface for instrumenting player state
    /// </summary>
    [System.Obsolete("PlayerState is no longer used")]
    public class PlayerState
    {
		/// <summary>
		/// Factory method for invoking CognitiveVRPlugins.SessionTransaction methods
		/// </summary>
		/// <param name="transactionId">Transaction id, if applicable - this is only REQUIRED in situation where multiple transactions in the same category may exist (read: be concurrently begun)</param> 
		public static PlayerStateTransaction Transaction(string transactionId = null)
		{
			return new PlayerStateTransaction(transactionId);
		}
	}
}

