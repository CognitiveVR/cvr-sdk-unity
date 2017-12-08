using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//making a Windows universal build?
//unityplugin rightclick->properties->build->conditional compilation symbols
//add 'NETFX_CORE' and build the dll

namespace CognitiveVR
{
    public static class Constants
    {
        public const int DEFAULT_REQUEST_TIMEOUT = 3000; // in ms
        public const int INIT_TIMEOUT = 10000; // in ms
        public const string TXN_SUCCESS = "success";
        public const string TXN_ERROR = "error";
        public const string ENTITY_TYPE_USER = "USER";
        public const string ENTITY_TYPE_DEVICE = "DEVICE";

        public const string TIMEOUT_MODE_TRANSACTION = "TXN";
        public const string TIMEOUT_MODE_ANY = "ANY";

        public const string PROPERTY_ISNEW = "_COGNITIVEVR_isNew";
        internal const double TIME_RECORDAGAIN = 8.0*60.0*60.0;  // only record each 8 hours


        //editor urls
        //GET dynamic object manifest
        public static string GETDYNAMICMANIFEST(int versionid)
        {
            return String.Concat("https://api.sceneexplorer.com/versions/", versionid, "/objects");
        }

        //POST dynamic object manifest
        public static string POSTDYNAMICMANIFEST(string sceneid, int versionnumber)
        {
            return String.Concat("https://data.sceneexplorer.com/objects/",sceneid,"?version=", versionnumber);
        }
        //POST dynamic object mesh data
        public static string POSTDYNAMICOBJECTDATA(string sceneid, int versionnumber, string exportdirectory)
        {
            return String.Concat("https://data.sceneexplorer.com/objects/",sceneid,"/",exportdirectory,"?version=", versionnumber);
        }


        //GET scene settings and read scene version
        public static string GETSCENEVERSIONS(string sceneid)
        {
            return String.Concat("https://api.sceneexplorer.com/scenes/", sceneid);
        }

        //POST scene screenshot
        public static string POSTSCREENSHOT(string sceneid, int versionnumber)
        {
            return String.Concat("https://data.sceneexplorer.com/scenes/",sceneid,"/screenshot?version=", versionnumber);
        }

        //POST upload decimated scene
        public static string POSTNEWSCENE()
        {
            return "https://data.sceneexplorer.com/scenes";
        }

        //POST upload and replace existing scene
        public static string POSTUPDATESCENE(string sceneid)
        {
            return String.Concat("https://data.sceneexplorer.com/scenes/",sceneid);
        }
        
        //POST get auth token from dynamic object manifest response
        public static string POSTAUTHTOKEN(string sceneid)
        {
            return String.Concat("https://api.sceneexplorer.com/tokens/", sceneid);
        }

        //UNITY used to open scenes on sceneexplorer
        public const string SCENEEXPLORER_SCENE = "https://sceneexplorer.com/scene/";

        //POST used to log into the editor
        public const string API_SESSIONS = "https://api.cognitivevr.io/sessions";

        //UNITY opens dashboard page to create a new product
        public const string DASH_NEWPRODUCT = "https://dashboard.cognitivevr.io/admin/products/create";



        //GET github api to get latest release data
        public const string GITHUB_SDKVERSION = "https://api.github.com/repos/cognitivevr/cvr-sdk-unity/releases/latest";

        //UNITY where the user goes to download the sdk
        public const string GITHUB_RELEASES = "https://github.com/CognitiveVR/cvr-sdk-unity/releases";




        //session urls


        //POST dynamics json data to scene explorer
        public static string POSTDYNAMICDATA (string sceneid, int versionnumber)
        {
            return string.Concat("https://data.sceneexplorer.com/dynamics/", sceneid, "?version=",versionnumber);
        }

        //POST gaze json data to scene explorer
        public static string POSTGAZEDATA(string sceneid, int versionnumber)
        {
            return string.Concat("https://data.sceneexplorer.com/gaze/", sceneid, "?version=", versionnumber);
        }

        //POST event json data to scene explorer
        public static string POSTEVENTDATA(string sceneid, int versionnumber)
        {
            return string.Concat("https://data.sceneexplorer.com/events/", sceneid, "?version=", versionnumber);
        }

        //POST sensor json data to scene explorer
        public static string POSTSENSORDATA(string sceneid, int versionnumber)
        {
            return string.Concat("https://data.sceneexplorer.com/sensors/", sceneid, "?version=", versionnumber);
        }

        //POST used in core initialization, personalization (tuning), data collector
        //TODO this is handed off by lots of different components. should just put this url into the actual urls that get sent. THIS SHOULD NOT CHANGE DURING RUNTIME
        public const string DATA_HOST = "https://data.cognitivevr.io";

        //unused
        public const string NOTIFICATIONS_HOST = "https://notification.cognitivevr.io";


        //GET request question set
        public static string GETEXITPOLLQUESTIONSET(string customerid, string hookname)
        {
            return string.Concat("https://api.cognitivevr.io/products/", customerid, "/questionSetHooks/", hookname, "/questionset");
        }
        //POST question set responses
        public static string POSTEXITPOLLRESPONSES(string customerid, string questionsetname, int questionsetversion)
        {
            return string.Concat("https://api.cognitivevr.io/products/", customerid, "/questionSets/", questionsetname, "/",questionsetversion, "/responses");
        }
    }
}
