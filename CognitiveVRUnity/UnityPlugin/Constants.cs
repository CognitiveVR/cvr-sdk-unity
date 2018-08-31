using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CognitiveVR
{
    public static class Constants
    {
        private const string version = "0";

        //editor urls
        //GET dynamic object manifest
        public static string GETDYNAMICMANIFEST(int versionid)
        {
            return String.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version,"/versions/", versionid, "/objects"); //changed api to data
        }

        //POST dynamic object manifest
        public static string POSTDYNAMICMANIFEST(string sceneid, int versionnumber)
        {
            return String.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/objects/", sceneid,"?version=", versionnumber);
        }
        //POST dynamic object mesh data
        public static string POSTDYNAMICOBJECTDATA(string sceneid, int versionnumber, string exportdirectory)
        {
            return String.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/objects/", sceneid,"/",exportdirectory,"?version=", versionnumber);
        }

        //GET scene settings and read scene version
        public static string GETSCENEVERSIONS(string sceneid)
        {
            return String.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/scenes/", sceneid); //changed api to data
        }

        //POST scene screenshot
        public static string POSTSCREENSHOT(string sceneid, int versionnumber)
        {
            return String.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/scenes/", sceneid,"/screenshot?version=", versionnumber);
        }

        //POST upload decimated scene
        public static string POSTNEWSCENE()
        {
            return CognitiveVR_Preferences.Instance.Protocol + "://" + CognitiveVR_Preferences.Instance.Gateway + "/v"+ version+"/scenes";
        }

        //POST upload and replace existing scene
        public static string POSTUPDATESCENE(string sceneid)
        {
            return String.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/scenes/", sceneid);
        }
        
        //GET github api to get latest release data
        public const string GITHUB_SDKVERSION = "https://api.github.com/repos/cognitivevr/cvr-sdk-unity/releases/latest";

        //UNITY where the user goes to download the sdk
        public const string GITHUB_RELEASES = "https://github.com/CognitiveVR/cvr-sdk-unity/releases";

        //GET media source list
        public static string GETMEDIASOURCELIST()
        {
            return string.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/media");
        }

        //session urls
        internal static void Initialize()
        {
            dynamicUrl = string.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/dynamics/");
            gazeUrl = string.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/gaze/");
            eventUrl = string.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/events/");
            sensorUrl = string.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/sensors/");
        }
        static string dynamicUrl;
        static string gazeUrl;
        static string eventUrl;
        static string sensorUrl;
        //POST dynamics json data to scene explorer
        public static string POSTDYNAMICDATA (string sceneid, int versionnumber)
        {
            return string.Concat(dynamicUrl, sceneid, "?version=",versionnumber.ToString());
        }

        //POST gaze json data to scene explorer
        public static string POSTGAZEDATA(string sceneid, int versionnumber)
        {
            return string.Concat(gazeUrl, sceneid, "?version=", versionnumber.ToString());
        }

        //POST event json data to scene explorer
        public static string POSTEVENTDATA(string sceneid, int versionnumber)
        {
            return string.Concat(eventUrl, sceneid, "?version=", versionnumber.ToString());
        }

        //POST sensor json data to scene explorer
        public static string POSTSENSORDATA(string sceneid, int versionnumber)
        {
            return string.Concat(sensorUrl, sceneid, "?version=", versionnumber.ToString());
        }


        //GET request question set
        public static string GETEXITPOLLQUESTIONSET(string hookname)
        {
            return string.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version, "/questionSetHooks/", hookname, "/questionSet");
        }
        //POST question set responses
        public static string POSTEXITPOLLRESPONSES(string questionsetname, int questionsetversion)
        {
            return string.Concat(CognitiveVR_Preferences.Instance.Protocol, "://", CognitiveVR_Preferences.Instance.Gateway, "/v", version,"/questionSets/", questionsetname, "/",questionsetversion.ToString(), "/responses");
        }
    }
}
