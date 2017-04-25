using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// If you are using Steamworks.net (https://steamworks.github.io/installation/) you can use this code to pass in your user's steam id
/// 
/// This is initially disabled so this package will compile if you do NOT have Steamworks.net in your project
/// Below should be perfectly working code if you have Steamworks.net in your project - just un-comment!
/// </summary>

namespace CognitiveVR.Components
{
    public class SteamworksUser : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);
            
            //Steamworks.SteamAPI.Init(); //doesn't have to be called here, but Steamworks must be Initialized before you call GetSteamID()
            //EntityInfo user = CognitiveVR.EntityInfo.createUserInfo(Steamworks.SteamUser.GetSteamID().ToString());
            //Core.registerUser(user, delegate (Error error) { });
        }

        public static bool GetWarning() { return true; }

        public static string GetDescription()
        {
            return "Create a unique user from Steamworks.net plugin\nThis requires attention before this will function!";
        }
    }
}