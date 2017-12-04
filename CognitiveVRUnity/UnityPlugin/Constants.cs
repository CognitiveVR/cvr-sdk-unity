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
        public const string SCENEEXPLORERAPI_OBJECTS = "https://sceneexplorer.com/api/objects/";
        //GET dynamic object manifest                           Constants.SCENEEXPLORERAPI_OBJECTS :sceneId
        //POST dynamic object manifest                          Constants.SCENEEXPLORERAPI_OBJECTS :sceneId ?version= :version
        //POST dynamic object mesh data                         Constants.SCENEEXPLORERAPI_OBJECTS :sceneId / :exportDirectory

        public const string SCENEEXPLORERAPI_SCENES = "https://sceneexplorer.com/api/scenes/";
        //GET scene settings and read scene version             Constants.SCENEEXPLORERAPI_SCENES :sceneId / settings
        //POST scene screenshot                                 Constants.SCENEEXPLORERAPI_SCENES :sceneid / screenshot?version= :version
        //POST upload decimated scene                           Constants.SCENEEXPLORERAPI_SCENES

        public const string SCENEEXPLORERAPI_TOKENS = "https://sceneexplorer.com/api/tokens/";
        //GET auth token from dynamic object manifest response  Constants.SCENEEXPLORERAPI_TOKENS :sceneId

        public const string SCENEEXPLORER_SCENE = "https://sceneexplorer.com/scene/";
        //UNITY used to open scenes on sceneexplorer            Constants.SCENEEXPLORER_SCENE :sceneId

        public const string API_SESSIONS = "https://api.cognitivevr.io/sessions";
        //POST used to log into the editor

        public const string DASH_NEWPRODUCT = "https://dashboard.cognitivevr.io/admin/products/create";
        //UNITY opens dashboard page to create a new product

        
        
        public const string GITHUB_SDKVERSION = "https://api.github.com/repos/cognitivevr/cvr-sdk-unity/releases/latest";
        //GET github api to get latest release data

        public const string GITHUB_RELEASES = "https://github.com/CognitiveVR/cvr-sdk-unity/releases";
        //UNITY where the user goes to download the sdk



        //session urls


        public const string DYNAMICS_URL = "https://sceneexplorer.com/api/dynamics/";
        //POST dynamics json data to scene explorer             Constants.DYNAMICS_URL :sceneId

        public const string GAZE_URL = "https://sceneexplorer.com/api/gaze/";
        //POST gaze json data to scene explorer                 Constants.GAZE_URL :sceneId

        public const string EVENTS_URL = "https://sceneexplorer.com/api/events/";
        //POST event json data to scene explorer                Constants.EVENTS_URL :sceneId

        public const string SENSORS_URL = "https://sceneexplorer.com/api/sensors/";
        //POST sensor json data to scene explorer               Constants.SENSORS_URL :sceneId

        public const string DATA_HOST = "https://data.cognitivevr.io";
        //POST used in core initialization, personalization (tuning), data collector
        //TODO this is handed off by lots of different components. should just put this url into the actual urls that get sent. THIS CAN NOT CHANGE DURING RUNTIME

        public const string NOTIFICATIONS_HOST = "https://notification.cognitivevr.io";
        //unused

        public const string API_PRODUCTS = "https://api.cognitivevr.io/products/";
        //GET request question set                              Constants.API_PRODUCTS :customerID / questionSetHooks / :hookName / questionSet
        //POST question set responses                           Constants.API_PRODUCTS :customerID / questionSets / :questionSetName / :questionSetVersion / responses
    }
}
