using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CognitiveVR
{
	public class Util
	{
		private const string LOG_TAG = "com.cvr.cognitivevr: ";
		private static bool sLogEnabled = false;
		private static IDictionary<string, object> sDeviceAndAppInfo = new Dictionary<string, object>();
        internal static IDictionary<string, object> getDeviceAndAppInfo() { return sDeviceAndAppInfo; }
        internal static string getSDKName(string namePrefix) { return namePrefix; }

        private static HashSet<string> sValidCurrencyCodes = new HashSet<string>();
        private static IDictionary<string, HashSet<string>> sCurrencyCodesBySymbol = new Dictionary<string, HashSet<string>>();

		public static void setLogEnabled(bool value)
		{
			sLogEnabled = value;
		}

		// Internal logging.  These can be enabled by calling Util.setLogEnabled(true)
		public static void logDebug(string msg)
		{
			if (sLogEnabled)
			{
				Debug.Log(LOG_TAG + msg);
			}
		}
		
		public static void logError(Exception e)
		{
			if (sLogEnabled)
			{
				Debug.LogException(e);
			}
		}
		
		public static void logError(string msg)
		{
			if (sLogEnabled)
			{
				Debug.LogError(LOG_TAG + msg);
			}
		}

        internal static void cacheDeviceAndAppInfo()
        {
            // Clear out any previously set data
            sDeviceAndAppInfo.Clear();

            // Get the platform from the runtime
            // Note that android/kindle, iOS, and WP8 go through a different path, so those platforms are not expected
            switch (Application.platform)
            {
                case RuntimePlatform.BlackBerryPlayer:
                    sDeviceAndAppInfo.Add("cvr.unity.platform", "blackberry");
                    break;
                case RuntimePlatform.FlashPlayer:
                    sDeviceAndAppInfo.Add("cvr.unity.platform", "adobeflash");
                    break;
                case RuntimePlatform.LinuxPlayer:
                    sDeviceAndAppInfo.Add("cvr.unity.platform", "linux");
                    break;
                case RuntimePlatform.OSXPlayer:
                    sDeviceAndAppInfo.Add("cvr.unity.platform", "mac");
                    break;
                case RuntimePlatform.PS3:
                    sDeviceAndAppInfo.Add("cvr.unity.platform", "ps3");
                    break;
                /*
                case RuntimePlatform.WiiPlayer:
                    sDeviceAndAppInfo.Add("cognitivevr.unity.platform", "wii");
                    break;
                */
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.WindowsEditor:
                    sDeviceAndAppInfo.Add("cvr.unity.platform", "unityeditor");
                    break;
                case RuntimePlatform.WindowsPlayer:
                    sDeviceAndAppInfo.Add("cvr.unity.platform", "windows");
                    break;
                case RuntimePlatform.OSXWebPlayer:
                case RuntimePlatform.WindowsWebPlayer:
                    sDeviceAndAppInfo.Add("cvr.unity.platform", "unitywebplayer");
                    break;
                case RuntimePlatform.XBOX360:
                    sDeviceAndAppInfo.Add("cvr.unity.platform", "xbox360");
                    break;
                default:
                    // Unknown/unexpected
                    sDeviceAndAppInfo.Add("cvr.unity.platform", "unknown");
                    break;

            }

            // Get the rest of the information about the device
            sDeviceAndAppInfo.Add("cvr.app.name", Application.productName);
            sDeviceAndAppInfo.Add("cvr.app.version", Application.version);
            sDeviceAndAppInfo.Add("cvr.unity.version", Application.unityVersion);
            sDeviceAndAppInfo.Add("cvr.device.model", SystemInfo.deviceModel);
            sDeviceAndAppInfo.Add("cvr.device.type", SystemInfo.deviceType.ToString());
            sDeviceAndAppInfo.Add("cvr.device.platform", Application.platform);
            sDeviceAndAppInfo.Add("cvr.device.os", SystemInfo.operatingSystem);
            sDeviceAndAppInfo.Add("cvr.device.graphics.name", SystemInfo.graphicsDeviceName);
            sDeviceAndAppInfo.Add("cvr.device.graphics.type", SystemInfo.graphicsDeviceType.ToString());
            sDeviceAndAppInfo.Add("cvr.device.graphics.vendor", SystemInfo.graphicsDeviceVendor);
            sDeviceAndAppInfo.Add("cvr.device.graphics.version", SystemInfo.graphicsDeviceVersion);
            sDeviceAndAppInfo.Add("cvr.device.graphics.memory", SystemInfo.graphicsMemorySize);
            sDeviceAndAppInfo.Add("cvr.device.processor", SystemInfo.processorType);
            sDeviceAndAppInfo.Add("cvr.device.memory", SystemInfo.systemMemorySize);
            sDeviceAndAppInfo.Add("cvr.vr.enabled", UnityEngine.VR.VRSettings.enabled);
            sDeviceAndAppInfo.Add("cvr.vr.display.model", UnityEngine.VR.VRSettings.enabled && UnityEngine.VR.VRDevice.isPresent ? UnityEngine.VR.VRDevice.model : "");
            sDeviceAndAppInfo.Add("cvr.vr.display.family", UnityEngine.VR.VRSettings.enabled && UnityEngine.VR.VRDevice.isPresent ? UnityEngine.VR.VRDevice.family : "");

        }

        internal static void cacheCurrencyInfo()
        {
            // Clear out any previously set data
            sValidCurrencyCodes.Clear();
            sCurrencyCodesBySymbol.Clear();

            // Cache a set of valid ISO 4217 currency codes and a map of valid currency symbols to ISO 4217 currency codes
            foreach (CultureInfo info in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                try
                {
                    RegionInfo ri = new RegionInfo(info.Name);
                    sValidCurrencyCodes.Add(ri.ISOCurrencySymbol);

                    if (sCurrencyCodesBySymbol.ContainsKey(ri.CurrencySymbol))
                    {
                        sCurrencyCodesBySymbol[ri.CurrencySymbol].Add(ri.ISOCurrencySymbol);
                    }
                    else
                    {
                        HashSet<string> codes = new HashSet<string>();
                        codes.Add(ri.ISOCurrencySymbol);
                        sCurrencyCodesBySymbol.Add(ri.CurrencySymbol, codes);
                    }
                }
                catch (ArgumentException)
                {
                    // Not a valid culture name.  Ok, move along....
                }
            }
        }

        // Given an input currency string, return a string that is valid currency string.
        // This can be either a valid ISO 4217 currency code or a currency symbol (e.g., for real currencies),  or simply any other ASCII string (e.g., for virtual currencies)
        // If one cannot be determined, this method returns "unknown"
        public static string getValidCurrencyString(string currency)
        {
            string validCurrencyStr;

            // First check if the string is already a valid ISO 4217 currency code (i.e., it's in the list of known codes)
            if (sValidCurrencyCodes.Contains(currency.ToUpper()))
            {
                // It is, just return it
                validCurrencyStr = currency.ToUpper();
            }
            else
            {
                // Not a valid currency code, is it a currency symbol?
                HashSet<string> possibleCodes;
                if (sCurrencyCodesBySymbol.TryGetValue(currency.ToUpper(), out possibleCodes))
                {
                    // It's a valid symbol

                    // If there is only one associated currency code, use it
                    if (1 == possibleCodes.Count)
                    {
                        using (IEnumerator<string> iter = possibleCodes.GetEnumerator())
                        {
                            iter.MoveNext();
                            validCurrencyStr = iter.Current;
                        }
                    }
                    else
                    {
                        // Ok, more than one code associated with this symbol
                        // We make a best guess as to the actual currency code based on the user's locale.
                        RegionInfo ri = RegionInfo.CurrentRegion;
                        if (possibleCodes.Contains(ri.ISOCurrencySymbol))
                        {
                            // The locale currency is in the list of possible codes
                            // It's pretty likely that this currency symbol refers to the locale currency, so let's assume that
                            // This is not a perfect solution, but it's the best we can do until Google and Amazon start giving us more than just currency symbols
                            validCurrencyStr = ri.ISOCurrencySymbol;
                        }
                        else
                        {
                            // We have no idea which currency this symbol refers to, so just set it to "unknown"
                            validCurrencyStr = "unknown";
                        }
                    }
                }
                else
                {
                    // This is not a known currencyc sy so mbol,it must be a virtual currency
                    // Strip out any non-ASCII haracters
                    validCurrencyStr = Regex.Replace(currency, @"[^\u0000-\u007F]", string.Empty);
                }
            }

            return validCurrencyStr;
        }

        /// <summary>
		/// Get the Unix timestamp
		/// </summary>
		internal static double Timestamp()
		{
			TimeSpan span = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return span.TotalSeconds;
		}
	}
}