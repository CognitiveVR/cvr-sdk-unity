using System.Collections.Generic;

namespace CognitiveVR
{
	/// <summary>
	/// The EntityInfo class is a helper class used during initialization of CognitiveVR.
	/// 
	/// Use the factory methods createUserInfo or createDeviceInfo provided to create an instance of this class.
	/// </summary>
	public class EntityInfo 
	{
		internal string type { get; private set; }
		internal string entityId { get; private set; }
		private bool? isNew { set { if(value.HasValue) { setProperty(Constants.PROPERTY_ISNEW, value.GetValueOrDefault()); } } }
		internal Dictionary<string, object> properties { get; private set; }

		private EntityInfo() {}

		/// <summary>
		/// Factory method for creating an instance of EntityInfo for a user
		/// </summary>
		/// <returns>A populated EntityInfo</returns>
		/// <param name="userId">The user id</param>
		/// <param name="properties">Any initial user state that you want to report to CognitiveVR</param>
		/// <param name="isNew">Explicitly report the user as new or not. Setting a value here will override CognitiveVR's automatic new user detection!</param>
		public static EntityInfo createUserInfo(string userId, Dictionary<string, object> properties = null, bool? isNew = null)
		{
			EntityInfo user = new EntityInfo();
			user.type = Constants.ENTITY_TYPE_USER;
			user.entityId = userId;
			user.properties = properties;
			user.isNew = isNew;

			return user;
		}

		/// <summary>
		/// Factory method for creating an instance of EntityInfo for a device
		/// </summary>
		/// <returns>A populated EntityInfo</returns>
		/// <param name="properties">Any initial device state that you want to report to CognitiveVR</param>
		/// <param name="isNew">Explicitly report the device as new or not. Setting a value here will override CognitiveVR's automatic new device detection!</param>
		public static EntityInfo createDeviceInfo(Dictionary<string, object> properties = null, bool? isNew = null)
		{
			EntityInfo device = new EntityInfo();
			device.type = Constants.ENTITY_TYPE_DEVICE;
			device.properties = properties;
			device.isNew = isNew;

			return device;
		}

		/// <summary>
		/// Overrides the entityId. Typically this is only used in the case where an application wants specific control over device ids.
		/// </summary>
		/// <returns>itself (to support a builder-style pattern)</returns>
		/// <param name="entityId">The id to report to CognitiveVR</param>
		public EntityInfo overrideId(string entityId)
		{
			this.entityId = entityId;
			return this;
		}

		/// <summary>
		/// For setting an single property of initial entity state.  Useful for cases where the implementor does not want to build a full dictionary to pass to the create* factory method.
		/// </summary>
		/// <returns>itself (to support a builder-style pattern)</returns>
		/// <param name="key">Key for entity state property</param>
		/// <param name="value">Value for entity state property</param>
		public EntityInfo setProperty(string key, object value)
		{
			if(null == this.properties)
				this.properties = new Dictionary<string, object>();

			this.properties[key] = value;
			return this;
		}

		/// <summary>
		/// For setting properties after creation. Mainly provided for consistency with other SDK platforms.
		/// </summary>
		/// <returns>itself (to support a builder-style pattern)</returns>
		/// <param name="properties">Any initial user state that you want to report to CognitiveVR</param>
		public EntityInfo setProperties(Dictionary<string, object> properties)
		{
			this.properties = properties;
			return this;
		}
	}
}

