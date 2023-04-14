using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//static and constant strings used throughout the SDK

namespace Cognitive3D
{
    internal static class CognitiveStatics
    {
        private const string version = "0";

        //editor urls
        //GET dynamic object manifest
        internal static string GETDYNAMICMANIFEST(int versionid)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version,"/versions/", versionid, "/objects"); //changed api to data
        }

        //POST dynamic object manifest
        internal static string POSTDYNAMICMANIFEST(string sceneid, int versionnumber)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/objects/", sceneid,"?version=", versionnumber);
        }
        //POST dynamic object mesh data
        internal static string POSTDYNAMICOBJECTDATA(string sceneid, int versionnumber, string exportdirectory)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/objects/", sceneid,"/",exportdirectory,"?version=", versionnumber);
        }

        //GET scene settings and read scene version
        internal static string GETSCENEVERSIONS(string sceneid)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/scenes/", sceneid); //changed api to data
        }

        //POST scene screenshot
        internal static string POSTSCREENSHOT(string sceneid, int versionnumber)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/scenes/", sceneid,"/screenshot?version=", versionnumber);
        }

        //POST upload decimated scene
        internal static string POSTNEWSCENE()
        {
            return Cognitive3D_Preferences.Instance.Protocol + "://" + Cognitive3D_Preferences.Instance.Gateway + "/v"+ version+"/scenes";
        }

        //POST upload and replace existing scene
        internal static string POSTUPDATESCENE(string sceneid)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/scenes/", sceneid);
        }

        internal static string SCENELINK(string sceneid, int versionNumber)
        {
            return "https://app.cognitive3d.com/scenes/" + sceneid + "/v/" + versionNumber + "/insights";
        }

        //GET github api to get latest release data
        internal const string GITHUB_SDKVERSION = "https://api.github.com/repos/CognitiveVR/cvr-sdk-unity/releases/latest";

        //UNITY where the user goes to download the sdk
        internal const string GITHUB_RELEASES = "https://github.com/CognitiveVR/cvr-sdk-unity/releases";

        //GET media source list
        internal static string GETMEDIASOURCELIST()
        {
            return string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/media");
        }

        //session urls
        internal static void Initialize()
        {
            if (!string.IsNullOrEmpty(ApplicationKey)) { return; }
            dynamicUrl = string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/dynamics/");
            gazeUrl = string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/gaze/");
            eventUrl = string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/events/");
            sensorUrl = string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/sensors/");
            fixationUrl = string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/fixations/");
            ApplicationKey = "APIKEY:DATA " + Cognitive3D_Preferences.Instance.ApplicationKey;
        }
        private static string dynamicUrl;
        private static string gazeUrl;
        private static string eventUrl;
        private static string sensorUrl;
        private static string fixationUrl;
        internal static string ApplicationKey;
        //POST dynamics json data to scene explorer
        
        internal static void Reset()
        {
            ApplicationKey = string.Empty;
        }

        internal static string POSTDYNAMICDATA (string sceneid, int versionnumber)
        {
            return string.Concat(dynamicUrl, sceneid, "?version=",versionnumber.ToString());
        }

        //POST gaze json data to scene explorer
        internal static string POSTGAZEDATA(string sceneid, int versionnumber)
        {
            return string.Concat(gazeUrl, sceneid, "?version=", versionnumber.ToString());
        }

        //POST event json data to scene explorer
        internal static string POSTEVENTDATA(string sceneid, int versionnumber)
        {
            return string.Concat(eventUrl, sceneid, "?version=", versionnumber.ToString());
        }

        //POST sensor json data to scene explorer
        internal static string POSTSENSORDATA(string sceneid, int versionnumber)
        {
            return string.Concat(sensorUrl, sceneid, "?version=", versionnumber.ToString());
        }

        //POST fixation json data to scene explorer
        internal static string POSTFIXATIONDATA(string sceneid, int versionnumber)
        {
            return string.Concat(fixationUrl, sceneid, "?version=", versionnumber.ToString());
        }


        //GET request question set
        internal static string GETEXITPOLLQUESTIONSET(string hookname)
        {
            return string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/questionSetHooks/", hookname, "/questionSet");
        }
        //POST question set responses
        internal static string POSTEXITPOLLRESPONSES(string questionsetname, int questionsetversion)
        {
            return string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version,"/questionSets/", questionsetname, "/",questionsetversion.ToString(), "/responses");
        }
    }
}
