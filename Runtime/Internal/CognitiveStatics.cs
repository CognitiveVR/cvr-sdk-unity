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
        private const string META_SUBSCRIPTION_URL = "https://graph.oculus.com/application/subscriptions";

        //editor urls
        //GET dynamic object manifest
        //https://data.cognitive3d.com/v0/objects
        internal static string GetDynamicManifest(int versionid)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version,"/versions/", versionid, "/objects");
        }

        //POST dynamic object aggregation manifest
        //https://data.cognitive3d.com/v0/objects/[sceneId]?version=[sceneVersionNumber]
        internal static string PostDynamicManifest(string sceneid, int versionnumber)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/objects/", sceneid,"?version=", versionnumber);
        }
        //POST dynamic object mesh data
        //https://data.cognitive3d.com/v0/objects/[sceneId]/[dynamicName]?version=[sceneVersionNumber]
        internal static string PostDynamicObjectData(string sceneid, int versionnumber, string exportdirectory)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/objects/", sceneid,"/",exportdirectory,"?version=", versionnumber);
        }

        //GET scene settings and read scene version
        //https://data.cognitive3d.com/v0/scenes/[sceneId]
        internal static string GetSceneVersions(string sceneid)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/scenes/", sceneid);
        }

        //POST scene screenshot
        //https://data.cognitive3d.com/v0/scenes/[sceneId]/screenshot?version=[sceneVersionNumber]
        internal static string PostScreenshot(string sceneid, int versionnumber)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/scenes/", sceneid,"/screenshot?version=", versionnumber);
        }

        //POST upload decimated scene
        //https://data.cognitive3d.com/v0/scenes/
        internal static string PostNewScene()
        {
            return Cognitive3D_Preferences.Instance.Protocol + "://" + Cognitive3D_Preferences.Instance.Gateway + "/v"+ version+"/scenes";
        }

        //POST upload and replace existing scene
        //https://data.cognitive3d.com/v0/scenes/[sceneId]
        internal static string PostUpdateScene(string sceneid)
        {
            return String.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/scenes/", sceneid);
        }

        //https://app.cognitive3d.com/v3/scenes/[sceneId]/v/[sceneVersionNumber]/viewer
        internal static string GetSceneUrl(string sceneid, int sceneVersionNumber)
        {
            return "https://app.cognitive3d.com/v3/scenes/" + sceneid + "/v/" + sceneVersionNumber + "/viewer";
        }

        //GET github api to get latest release data
        internal const string GITHUB_SDKVERSION = "https://api.github.com/repos/CognitiveVR/cvr-sdk-unity/releases/latest";

        //UNITY where the user goes to download the sdk
        internal const string GITHUB_RELEASES = "https://github.com/CognitiveVR/cvr-sdk-unity/releases";

        //GET media source list
        //https://data.cognitive3d.com/v0/media
        internal static string GetMediaSourceList()
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
            boundaryUrl = string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/boundary/");
            ApplicationKey = "APIKEY:DATA " + Cognitive3D_Preferences.Instance.ApplicationKey;
        }
        private static string dynamicUrl;
        private static string gazeUrl;
        private static string eventUrl;
        private static string sensorUrl;
        private static string fixationUrl;
        private static string boundaryUrl;
        internal static string ApplicationKey;
        //POST dynamics json data to scene explorer
        
        internal static void Reset()
        {
            ApplicationKey = string.Empty;
        }

        internal static string PostDynamicData (string sceneid, int versionnumber)
        {
            return string.Concat(dynamicUrl, sceneid, "?version=",versionnumber.ToString());
        }

        //POST gaze json data to scene explorer
        internal static string PostGazeData(string sceneid, int versionnumber)
        {
            return string.Concat(gazeUrl, sceneid, "?version=", versionnumber.ToString());
        }

        //POST event json data to scene explorer
        internal static string PostEventData(string sceneid, int versionnumber)
        {
            return string.Concat(eventUrl, sceneid, "?version=", versionnumber.ToString());
        }

        //POST sensor json data to scene explorer
        internal static string PostSensorData(string sceneid, int versionnumber)
        {
            return string.Concat(sensorUrl, sceneid, "?version=", versionnumber.ToString());
        }

        //POST fixation json data to scene explorer
        internal static string PostFixationData(string sceneid, int versionnumber)
        {
            return string.Concat(fixationUrl, sceneid, "?version=", versionnumber.ToString());
        }

        //POST boundary json data to scene explorer
        internal static string PostBoundaryData(string sceneid, int versionnumber)
        {
            return string.Concat(boundaryUrl, sceneid, "?version=", versionnumber.ToString());
        }

        //GET request question set
        //https://data.cognitive3d.com/v0/scenes/questionSetHooks/[hookName]/questionSet
        internal static string GetExitpollQuestionSet(string hookname)
        {
            return string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/questionSetHooks/", hookname, "/questionSet");
        }

        //GET request question set
        internal static string GetExitpollHooks()
        {
            return string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/hooks");
        }

        //POST question set responses
        //https://data.cognitive3d.com/v0/scenes/questionSets/[questionSetName]/[questionSetVersion]/responses
        internal static string PostExitpollResponses(string questionsetname, int questionsetversion)
        {
            return string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version,"/questionSets/", questionsetname, "/",questionsetversion.ToString(), "/responses");
        }

        //GET request question set
        internal static string GetRemoteControlsURL(string userId)
        {
            return string.Concat(Cognitive3D_Preferences.Instance.Protocol, "://", Cognitive3D_Preferences.Instance.Gateway, "/v", version, "/appvariables?identifier=", userId);
        }

        /// <summary>
        /// Creates the GET request endpoint with access token and search parameters
        /// </summary>
        /// <returns>The endpoint for meta subscription GET request</returns>
        internal static string MetaSubscriptionContextEndpoint(string accessToken, List<string> queryParams)
        {
            string endpoint = META_SUBSCRIPTION_URL + "?access_token=" + accessToken;
            if (queryParams.Count > 0)
            {
                endpoint += "&fields=";
            }
            for (int i = 0; i < queryParams.Count; i++)
            {
                endpoint += queryParams[i];
                if (i < queryParams.Count - 1)
                {
                    endpoint += ",";
                }
            }
            return endpoint;
        }
    }
}
