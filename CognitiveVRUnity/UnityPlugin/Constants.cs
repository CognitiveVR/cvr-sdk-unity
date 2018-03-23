using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CognitiveVR
{
    //testdata.cognitive3d.com

    public static class Constants
    {
        private const string domain = "data.cognitive3d.com";
        private const string version = "0";

        //editor urls
        //GET dynamic object manifest
        public static string GETDYNAMICMANIFEST(int versionid)
        {
            return String.Concat("https://",domain,"/v", version,"/versions/", versionid, "/objects"); //changed api to data
        }

        //POST dynamic object manifest
        public static string POSTDYNAMICMANIFEST(string sceneid, int versionnumber)
        {
            return String.Concat("https://", domain, "/v", version, "/objects/", sceneid,"?version=", versionnumber);
        }
        //POST dynamic object mesh data
        public static string POSTDYNAMICOBJECTDATA(string sceneid, int versionnumber, string exportdirectory)
        {
            return String.Concat("https://", domain, "/v", version, "/objects/", sceneid,"/",exportdirectory,"?version=", versionnumber);
        }

        //GET scene settings and read scene version
        public static string GETSCENEVERSIONS(string sceneid)
        {
            return String.Concat("https://", domain, "/v", version, "/scenes/", sceneid); //changed api to data
        }

        //POST scene screenshot
        public static string POSTSCREENSHOT(string sceneid, int versionnumber)
        {
            return String.Concat("https://", domain, "/v", version, "/scenes/", sceneid,"/screenshot?version=", versionnumber);
        }

        //POST upload decimated scene
        public static string POSTNEWSCENE()
        {
            return "https://" + domain + "/v"+ version+"/scenes";
        }

        //POST upload and replace existing scene
        public static string POSTUPDATESCENE(string sceneid)
        {
            return String.Concat("https://", domain, "/v", version, "/scenes/", sceneid);
        }

        //UNITY used to open scenes on sceneexplorer
        public const string SCENEEXPLORER_SCENE = "https://sceneexplorer.com/scene/";

        //UNITY opens dashboard page to create a new product
        public const string DASH_NEWPRODUCT = "https://dashboard.cognitivevr.io/admin/products/create";



        //GET github api to get latest release data
        public const string GITHUB_SDKVERSION = "https://api.github.com/repos/cognitivevr/cvr-sdk-unity/releases/latest";

        //UNITY where the user goes to download the sdk
        public const string GITHUB_RELEASES = "https://github.com/CognitiveVR/cvr-sdk-unity/releases";

        public const string DASHBOARD = "http://dashboard.cognitivevr.io";


        //session urls

        //POST dynamics json data to scene explorer
        public static string POSTDYNAMICDATA (string sceneid, int versionnumber)
        {
            return string.Concat("https://", domain, "/v", version, "/dynamics/", sceneid, "?version=",versionnumber);
        }

        //POST gaze json data to scene explorer
        public static string POSTGAZEDATA(string sceneid, int versionnumber)
        {
            return string.Concat("https://", domain, "/v", version, "/gaze/", sceneid, "?version=", versionnumber);
        }

        //POST event json data to scene explorer
        public static string POSTEVENTDATA(string sceneid, int versionnumber)
        {
            return string.Concat("https://", domain, "/v", version, "/events/", sceneid, "?version=", versionnumber);
        }

        //POST sensor json data to scene explorer
        public static string POSTSENSORDATA(string sceneid, int versionnumber)
        {
            return string.Concat("https://", domain, "/v", version, "/sensors/", sceneid, "?version=", versionnumber);
        }


        //GET request question set
        public static string GETEXITPOLLQUESTIONSET(string hookname)
        {
            return string.Concat("https://", domain, "/v", version, "/questionSetHooks/", hookname, "/questionSet");
        }
        //POST question set responses
        public static string POSTEXITPOLLRESPONSES(string questionsetname, int questionsetversion)
        {
            return string.Concat("https://", domain, "/v", version,"/questionSets/", questionsetname, "/",questionsetversion, "/responses");
        }
    }
}
