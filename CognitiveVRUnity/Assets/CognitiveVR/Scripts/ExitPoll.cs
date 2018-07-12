using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;

namespace CognitiveVR
{
    namespace Json
    {
        [System.Serializable]
        public class ExitPollSetJson
        {
            public string customerId;
            public string id;
            public string name;
            public int version;
            public string title;
            public string status;

            public ExitPollSetJsonEntry[] questions;

            //this is what the panel will display
            [System.Serializable]
            public class ExitPollSetJsonEntry
            {
                public string title;
                public string type;
                //voice
                public int maxResponseLength;
                //scale
                public string minLabel;
                public string maxLabel;
                public ExitPollScaleRange range;
                //multiple choice
                public ExitPollSetJsonEntryAnswer[] answers;

                [System.Serializable]
                public class ExitPollScaleRange
                {
                    public int start;
                    public int end;
                }

                [System.Serializable]
                public class ExitPollSetJsonEntryAnswer
                {
                    public string answer;
                    public bool icon;
                }
            }
        }
    }

    //static class for requesting exitpoll question sets with multiple panels
    public static class ExitPoll
    {
        private static GameObject _exitPollHappySad;
        public static GameObject ExitPollHappySad
        {
            get
            {
                if (_exitPollHappySad == null)
                    _exitPollHappySad = Resources.Load<GameObject>("ExitPollHappySad");
                return _exitPollHappySad;
            }
        }
        private static GameObject _exitPollTrueFalse;
        public static GameObject ExitPollTrueFalse
        {
            get
            {
                if (_exitPollTrueFalse == null)
                    _exitPollTrueFalse = Resources.Load<GameObject>("ExitPollBoolean");
                return _exitPollTrueFalse;
            }
        }
        private static GameObject _exitPollThumbs;
        public static GameObject ExitPollThumbs
        {
            get
            {
                if (_exitPollThumbs == null)
                    _exitPollThumbs = Resources.Load<GameObject>("ExitPollThumbs");
                return _exitPollThumbs;
            }
        }
        private static GameObject _exitPollScale;
        public static GameObject ExitPollScale
        {
            get
            {
                if (_exitPollScale == null)
                    _exitPollScale = Resources.Load<GameObject>("ExitPollScale");
                return _exitPollScale;
            }
        }

        private static GameObject _exitPollMultiple;
        public static GameObject ExitPollMultiple
        {
            get
            {
                if (_exitPollMultiple == null)
                    _exitPollMultiple = Resources.Load<GameObject>("ExitPollMultiple");
                return _exitPollMultiple;
            }
        }

        private static GameObject _exitPollVoice;
        public static GameObject ExitPollVoice
        {
            get
            {
                if (_exitPollVoice == null)
                    _exitPollVoice = Resources.Load<GameObject>("ExitPollVoice");
                return _exitPollVoice;
            }
        }

        private static GameObject _exitPollReticle;
        public static GameObject ExitPollReticle
        {
            get
            {
                if (_exitPollReticle == null)
                    _exitPollReticle = Resources.Load<GameObject>("ExitPollReticle");
                return _exitPollReticle;
            }
        }

        public static ExitPollSet CurrentExitPollSet;
        public static ExitPollSet NewExitPoll(string hookName)
        {
            if (CurrentExitPollSet != null)
            {
                CurrentExitPollSet.EndQuestionSet();
            }
            CurrentExitPollSet = new ExitPollSet();
            CurrentExitPollSet.RequestQuestionHookName = hookName;
            return CurrentExitPollSet;
        }
    }

    //creates a series of exit poll panels from question set constructed on the dashboard
    public class ExitPollSet
    {
        public ExitPollPanel CurrentExitPollPanel;

        System.Action EndAction;
        public ExitPollSet SetEndAction(System.Action endAction)
        {
            EndAction = endAction;
            return this;
        }

        public ExitPollSet AddEndAction(System.Action endAction)
        {
            if (EndAction == null)
            {
                EndAction = endAction;
            }
            else
            {
                EndAction += endAction;
            }
            return this;
        }

        public string RequestQuestionHookName = "";
        public void Begin()
        {
            currentPanelIndex = 0;
            if (string.IsNullOrEmpty(RequestQuestionHookName))
            {
                if (EndAction != null)
                {
                    EndAction.Invoke();
                }
                OverridePosition = null;
                OverrideRotation = null;
                if (_pointer) _pointer.SetVisible(false);
                ExitPoll.CurrentExitPollSet = null;
                Util.logDebug("CognitiveVR Exit Poll. You haven't specified a question hook to request!");
                return;
            }

            if (CognitiveVR_Manager.Instance != null)
            {
                //CognitiveVR_Manager.Instance.StartCoroutine(RequestQuestions());
                //hooks/questionsets. ask hook by id what their questionset is
                string url = Constants.GETEXITPOLLQUESTIONSET(RequestQuestionHookName);

                CognitiveVR.NetworkManager.GetExitPollQuestions(url, RequestQuestionHookName, QuestionSetResponse, 3);
            }
            else
            {
                Util.logDebug("Cannot display exitpoll. cognitiveVRManager not present in scene");
                if (EndAction != null)
                {
                    EndAction.Invoke();
                }
                OverridePosition = null;
                OverrideRotation = null;
                if (_pointer) _pointer.SetVisible(false);
                ExitPoll.CurrentExitPollSet = null;
            }
        }

        /// <summary>
        /// when you manually need to close the Exit Poll question set manually OR
        /// when requesting a new exit poll question set when one is already active
        /// </summary>
        public void EndQuestionSet()
        {
            panelProperties.Clear();
            if (CurrentExitPollPanel != null)
            {
                CurrentExitPollPanel.CloseError();
            }
            OnPanelError();
        }

        ExitPollPointer _pointer;

        //how to display all the panels and their properties. dictionary is <panelType,panelContent>
        List<Dictionary<string, string>> panelProperties = new List<Dictionary<string, string>>();

        int questionSetVersion;
        string QuestionSetName;

        string QuestionSetId; //questionsetname:questionsetversion

        //TODO this should grab a question received and cached on CognitiveVRManager Init
        //build a collection of panel properties from the response
        void QuestionSetResponse(int responsecode, string error,string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                //question timeout or not found
                if (EndAction != null)
                {
                    EndAction.Invoke();
                }
                OverridePosition = null;
                OverrideRotation = null;
                if (_pointer) _pointer.SetVisible(false);
                ExitPoll.CurrentExitPollSet = null;
                return;
            }


            //build all the panel properties
            Json.ExitPollSetJson json;
            try
            {
                 json = JsonUtility.FromJson<Json.ExitPollSetJson>(text);
            }
            catch
            {
                CognitiveVR.Util.logDebug("Exit poll Question response not formatted correctly! invoke end action");
                if (EndAction != null)
                {
                    EndAction.Invoke();
                }
                OverridePosition = null;
                OverrideRotation = null;
                if (_pointer) _pointer.SetVisible(false);
                ExitPoll.CurrentExitPollSet = null;
                return;
            }

            if (json.questions == null || json.questions.Length == 0)
            {
                CognitiveVR.Util.logDebug("Exit poll Question response empty! invoke end action");

                if (EndAction != null)
                {
                    EndAction.Invoke();
                }
                OverridePosition = null;
                OverrideRotation = null;
                if (_pointer) _pointer.SetVisible(false);
                ExitPoll.CurrentExitPollSet = null;
                return;
            }

            QuestionSetId = json.id;
            QuestionSetName = json.name;
            questionSetVersion = json.version;

            if (ControllerPointer != null)
            {
                _pointer = ControllerPointer.GetComponentInChildren<ExitPollPointer>();
                if (_pointer == null)
                {
                    _pointer = ControllerPointer.AddComponent<ExitPollPointer>();
                }
                _pointer.SetVisible(true);
            }

            //foreach (var question in json.questions)
            for (int i = 0; i < json.questions.Length; i++)
            {
                Dictionary<string, string> questionVariables = new Dictionary<string, string>();
                if (!questionVariables.ContainsKey("title"))
                {
                    questionVariables.Add("title", json.title);
                }
                questionVariables.Add("question", json.questions[i].title);
                questionVariables.Add("type", json.questions[i].type);
                responseProperties.Add(new ResponseContext(json.questions[i].type));
                questionVariables.Add("maxResponseLength", json.questions[i].maxResponseLength.ToString());

                if (!string.IsNullOrEmpty(json.questions[i].minLabel))
                    questionVariables.Add("minLabel", json.questions[i].minLabel);
                if (!string.IsNullOrEmpty(json.questions[i].maxLabel))
                    questionVariables.Add("maxLabel", json.questions[i].maxLabel);
                //put this into a csv string?

                if (json.questions[i].range != null)
                {
                    questionVariables.Add("start", json.questions[i].range.start.ToString());
                    questionVariables.Add("end", json.questions[i].range.end.ToString());
                }

                string csvMultipleAnswers = "";
                if (json.questions[i].answers != null)
                {
                    for (int j = 0; j < json.questions[i].answers.Length; j++)
                    {
                        if (json.questions[i].answers[j].answer.Length == 0) { continue; }
                        //TODO include support for custom icons on multiple choice answers
                        csvMultipleAnswers += json.questions[i].answers[j].answer + "|";
                    }
                }
                if (csvMultipleAnswers.Length > 0)
                {
                    csvMultipleAnswers = csvMultipleAnswers.Remove(csvMultipleAnswers.Length - 1); //last pipe
                    questionVariables.Add("csvanswers", csvMultipleAnswers);
                }
                panelProperties.Add(questionVariables);
            }

            IterateToNextQuestion();
            
        }

        //after a panel has been answered, the responses from each panel in a format to be sent to exitpoll microservice
        public class ResponseContext
        {
            public string QuestionType;
            public object ResponseValue;
            public ResponseContext(string questionType)
            {
                QuestionType = questionType;
            }
        }
        List<ResponseContext> responseProperties = new List<ResponseContext>();

        //these go to personalization api
        Dictionary<string, object> transactionProperties = new Dictionary<string, object>();

        //called from panel when a panel closes (after timeout, on close or on answer)
        public void OnPanelClosed(int panelId, string key, int objectValue)
        {
            transactionProperties.Add(key, objectValue);
            responseProperties[panelId].ResponseValue = objectValue;
            currentPanelIndex++;
            IterateToNextQuestion();
        }

        public void OnPanelClosedVoice(int panelId, string key, string base64voice)
        {
            transactionProperties.Add(key, 0);
            responseProperties[panelId].ResponseValue = base64voice;
            currentPanelIndex++;
            IterateToNextQuestion();
        }

        /// <summary>
        /// Use EndQuestionSet to close the active panel and immediately end the question set
        /// this sets the current panel to null and calls endaction. it assumes the panel closes itself
        /// </summary>
        public void OnPanelError()
        {
            //SendResponsesAsTransaction(); //for personalization api
            //var responses = FormatResponses();
            //SendQuestionResponses(responses); //for exitpoll microservice
            CurrentExitPollPanel = null;
            if (EndAction != null)
            {
                EndAction.Invoke();
            }
            OverridePosition = null;
            OverrideRotation = null;
            if (_pointer) _pointer.SetVisible(false);
            ExitPoll.CurrentExitPollSet = null;
            CognitiveVR.Util.logDebug("Exit poll OnPanelError - HMD is null, manually closing question set or new exit poll while one is active");
        }

        int currentPanelIndex = 0;
        int panelCount = 0;
        void IterateToNextQuestion()
        {
            bool useLastPanelPosition = false;
            Vector3 lastPanelPosition = Vector3.zero;

            //close current panel
            if (CurrentExitPollPanel != null)
            {
                lastPanelPosition = CurrentExitPollPanel.transform.position;
                useLastPanelPosition = true;
                //CurrentExitPollPanel = null;
            }

            if (!useLastPanelPosition)
            {
                if (!GetSpawnPosition(out lastPanelPosition))
                {
                    CognitiveVR.Util.logDebug("no last position set. invoke endaction");
                    if (EndAction != null)
                    {
                        EndAction.Invoke();
                    }
                    OverridePosition = null;
                    OverrideRotation = null;
                    if (_pointer) _pointer.SetVisible(false);
                    ExitPoll.CurrentExitPollSet = null;
                    return;
                }
            }

            //if next question, display that
            if (panelProperties.Count > 0 && currentPanelIndex < panelProperties.Count)
            {
                DisplayPanel(panelProperties[currentPanelIndex], panelCount, lastPanelPosition);
                //panelProperties.RemoveAt(0);
            }
            else //finished everything format and send
            {
                SendResponsesAsTransaction(); //for personalization api
                var responses = FormatResponses();
                SendQuestionResponses(responses); //for exitpoll microservice
                CurrentExitPollPanel = null;
                if (EndAction != null)
                {
                    EndAction.Invoke();
                }
                OverridePosition = null;
                OverrideRotation = null;
                if (_pointer) _pointer.SetVisible(false);
                ExitPoll.CurrentExitPollSet = null;
            }
            panelCount++;
        }

        void SendResponsesAsTransaction()
        {
            var exitpoll = new CustomEvent("cvr.exitpoll");
            exitpoll.SetProperty("userId", CognitiveVR.Core.UniqueID);
            exitpoll.SetProperty("questionSetId", QuestionSetId);
            exitpoll.SetProperty("hook", RequestQuestionHookName);

            var scenesettings = Core.TrackingScene;
            if (scenesettings != null && !string.IsNullOrEmpty(scenesettings.SceneId))
            {
                exitpoll.SetProperty("sceneId", scenesettings.SceneId);
            }

            foreach (var property in transactionProperties)
            {
                exitpoll.SetProperty(property.Key, property.Value);
            }
            exitpoll.Send(CurrentExitPollPanel.transform.position);
            Core.SendDataEvent();
        }

        //puts responses from questions into json for exitpoll microservice
        string FormatResponses()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");
            JsonUtil.SetString("userId", CognitiveVR.Core.UniqueID, builder);
            builder.Append(",");
            if (!string.IsNullOrEmpty(CognitiveVR_Preferences.LobbyId))
            {
                JsonUtil.SetString("lobbyId", CognitiveVR_Preferences.LobbyId, builder);
                builder.Append(",");
            }
            JsonUtil.SetString("questionSetId", QuestionSetId, builder);
            builder.Append(",");
            JsonUtil.SetString("sessionId", Core.SessionID, builder);
            builder.Append(",");
            JsonUtil.SetString("hook", RequestQuestionHookName, builder);
            builder.Append(",");

            var scenesettings = Core.TrackingScene;
            if (scenesettings != null)
            {
                JsonUtil.SetString("sceneId", scenesettings.SceneId, builder);
                builder.Append(",");
                JsonUtil.SetInt("versionNumber", scenesettings.VersionNumber, builder);
                builder.Append(",");
                JsonUtil.SetInt("versionId", scenesettings.VersionId, builder);
                builder.Append(",");
            }

            builder.Append("\"answers\":[");

            for (int i = 0; i < responseProperties.Count; i++)
            {
                var valueString = responseProperties[i].ResponseValue as string;
                if (!string.IsNullOrEmpty(valueString) && valueString == "skip")
                {
                    builder.Append("null,");
                }
                else
                {
                    builder.Append("{");
                    JsonUtil.SetString("type", responseProperties[i].QuestionType, builder);
                    builder.Append(",\"value\":");

                    if (!string.IsNullOrEmpty(valueString))
                    {
                        builder.Append("\"");
                        builder.Append(valueString);
                        builder.Append("\"");
                    }
                    else if (responseProperties[i].ResponseValue is bool)
                    {
                        builder.Append(((bool)responseProperties[i].ResponseValue).ToString().ToLower());
                    }
                    else if (responseProperties[i].ResponseValue is int)
                    {
                        builder.Append((int)responseProperties[i].ResponseValue);
                    }
                    else
                    {
                        builder.Append("\"\"");
                    }

                    builder.Append("},");
                }
            }
            builder.Remove(builder.Length - 1, 1); //remove comma
            builder.Append("]");
            builder.Append("}");

            return builder.ToString();
        }

        //the responses of all the questions in the set put together in a string and uploaded to the microservice at api.cognitivevr.io
        //each question is already sent as a transaction
        void SendQuestionResponses(string responses)
        {
            string url = Constants.POSTEXITPOLLRESPONSES(QuestionSetName, questionSetVersion);
            //byte[] bytes = System.Text.Encoding.ASCII.GetBytes(responses);

            CognitiveVR.Util.logDebug("ExitPoll Send Answers\nurl " + url);

            //var headers = new Dictionary<string, string>();//AUTH
            //headers.Add("Content-Type", "application/json");
            //headers.Add("X-HTTP-Method-Override", "POST");
            //headers.Add("Authorization", "APIKEY:DATA " + CognitiveVR_Preferences.Instance.APIKey);

            NetworkManager.Post(url, responses);
        }

        public bool UseTimeout { get; private set; }
        public float Timeout { get; private set; }
        /// <summary>
        /// set a maximum time that a question will be displayed. if this is passed, the question closes automatically
        /// </summary>
        /// <param name="allowTimeout"></param>
        /// <param name="secondsUntilTimeout"></param>
        /// <returns></returns>
        public ExitPollSet SetTimeout(bool allowTimeout, float secondsUntilTimeout)
        {
            UseTimeout = allowTimeout;
            Timeout = secondsUntilTimeout;
            return this;
        }

        private LayerMask _panelLayerMask;// = LayerMask.GetMask("Default", "World", "Ground");
        public LayerMask PanelLayerMask
        {
            get
            {
                return _panelLayerMask;
            }
        }

        //the prefered distance to display an exit poll
        private float _defaultDisplayDistance = 3;
        public float DisplayDistance
        {
            get
            {
                return _defaultDisplayDistance;
            }
        }

        //the minimum distance to display an exit poll. below this value will cancel the exit poll and continue with gameplay
        private float _defaultMinimumDisplayDistance = 1;
        public float MinimumDisplayDistance
        {
            get
            {
                return _defaultMinimumDisplayDistance;
            }
        }

        public ExitPollSet SetDisplayDistance(float preferedDistance, float minimumDistance)
        {
            _defaultMinimumDisplayDistance = Mathf.Max(minimumDistance, 0);
            _defaultDisplayDistance = Mathf.Max(minimumDistance, preferedDistance);

            return this;
        }

        /// <summary>
        /// Set the layers the Exit Poll panel will avoid
        /// </summary>
        /// <param name="layers"></param>
        /// <returns></returns>
        public ExitPollSet SetPanelLayerMask(params string[] layers)
        {
            _panelLayerMask = LayerMask.GetMask(layers);
            return this;
        }

        private bool displayReticule = true;
        public bool DisplayReticle { get { return displayReticule; } private set { displayReticule = value; } }
        /// <summary>
        /// Create a simple reticle while the ExitPoll Panel is visible
        /// </summary>
        /// <param name="useReticle"></param>
        /// <returns></returns>
        public ExitPollSet SetDisplayReticle(bool useReticle)
        {
            DisplayReticle = useReticle;
            return this;
        }

        public GameObject ControllerPointer { get; private set; }
        /// <summary>
        /// Find a pointer object and enable that
        /// </summary>
        /// <param name="visible"></param>
        /// <returns></returns>
        public ExitPollSet SetControllerPointer(GameObject controller)
        {
            ControllerPointer = controller;
            return this;
        }

        public bool LockYPosition { get; private set; }
        /// <summary>
        /// Use to HMD Y position instead of spawning the poll directly ahead of the player
        /// </summary>
        /// <param name="useLockYPosition"></param>
        /// <returns></returns>
        public ExitPollSet SetLockYPosition(bool useLockYPosition)
        {
            LockYPosition = useLockYPosition;
            return this;
        }

        public bool RotateToStayOnScreen { get; private set; }
        /// <summary>
        /// If this window is not in the player's line of sight, rotate around the player toward their facing
        /// </summary>
        /// <param name="useRotateToOnscreen"></param>
        /// <returns></returns>
        public ExitPollSet SetRotateToStayOnScreen(bool useRotateToOnscreen)
        {
            RotateToStayOnScreen = useRotateToOnscreen;
            return this;
        }

        public Vector3? OverridePosition { get; private set; }
        public ExitPollSet SetPosition(Vector3 overridePosition)
        {
            OverridePosition = overridePosition;
            return this;
        }

        public Quaternion? OverrideRotation { get; private set; }
        public ExitPollSet SetRotation(Quaternion overrideRotation)
        {
            OverrideRotation = overrideRotation;
            return this;
        }

        public bool StickWindow { get; private set; }
        /// <summary>
        /// Update the position of the Exit Poll prefab if the player teleports
        /// </summary>
        /// <param name="useStickyWindow"></param>
        /// <returns></returns>
        public ExitPollSet SetStickyWindow(bool useStickyWindow)
        {
            StickWindow = useStickyWindow;
            return this;
        }

        void DisplayPanel(Dictionary<string, string> properties, int panelId, Vector3 spawnPoint)
        {
            GameObject prefab = null;
            switch (properties["type"])
            {
                case "HAPPYSAD":
                    prefab = ExitPoll.ExitPollHappySad;
                    break;
                case "SCALE":
                    prefab = ExitPoll.ExitPollScale;
                    break;
                case "MULTIPLE":
                    prefab = ExitPoll.ExitPollMultiple;
                    break;
                case "VOICE":
                    prefab = ExitPoll.ExitPollVoice;
                    break;
                case "THUMBS":
                    prefab = ExitPoll.ExitPollThumbs;
                    break;
                case "BOOLEAN":
                    prefab = ExitPoll.ExitPollTrueFalse;
                    break;
            }
            if (prefab == null)
            {
                Util.logError("couldn't find prefab " + properties["type"]);
                if (EndAction != null)
                {
                    EndAction.Invoke();
                }
                OverridePosition = null;
                OverrideRotation = null;
                if (_pointer) _pointer.SetVisible(false);
                ExitPoll.CurrentExitPollSet = null;
                return;
            }

            var newPanelGo = GameObject.Instantiate<GameObject>(prefab);
            
            //set position
            if (OverridePosition.HasValue)
                newPanelGo.transform.position = OverridePosition.Value;
            else
                newPanelGo.transform.position = spawnPoint;

            //set rotation
            if (OverrideRotation.HasValue)
                newPanelGo.transform.rotation = OverrideRotation.Value;
            else
                newPanelGo.transform.rotation = Quaternion.LookRotation(newPanelGo.transform.position - CognitiveVR_Manager.HMD.position, Vector3.up);

            CurrentExitPollPanel = newPanelGo.GetComponent<ExitPollPanel>();

            CurrentExitPollPanel.Initialize(properties, panelId, this);
        }

        /// <summary>
        /// returns true if there's a valid position
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        bool GetSpawnPosition(out Vector3 pos)
        {
            pos = Vector3.zero;
            if (CognitiveVR_Manager.HMD == null) //no hmd? fail
            {
                return false;
            }

            //set position and rotation
            Vector3 spawnPosition = CognitiveVR_Manager.HMD.position + CognitiveVR_Manager.HMD.forward * DisplayDistance;

            if (LockYPosition)
            {
                Vector3 modifiedForward = CognitiveVR_Manager.HMD.forward;
                modifiedForward.y = 0;
                modifiedForward.Normalize();

                spawnPosition = CognitiveVR_Manager.HMD.position + modifiedForward * DisplayDistance;
            }

            RaycastHit hit = new RaycastHit();

            //test slightly in front of the player's hmd
            Collider[] colliderHits = Physics.OverlapSphere(CognitiveVR_Manager.HMD.position + Vector3.forward * 0.5f, 0.5f, PanelLayerMask);
            if (colliderHits.Length > 0)
            {
                Util.logDebug("ExitPoll.Initialize hit collider " + colliderHits[0].gameObject.name + " too close to player. Skip exit poll");
                //too close! just fail the popup and keep playing the game
                return false;
            }

            //ray from player's hmd position
            if (Physics.SphereCast(CognitiveVR_Manager.HMD.position, 0.5f, spawnPosition - CognitiveVR_Manager.HMD.position, out hit, DisplayDistance, PanelLayerMask))
            {
                if (hit.distance < MinimumDisplayDistance)
                {
                    Util.logDebug("ExitPoll.Initialize hit collider " + hit.collider.gameObject.name + " too close to player. Skip exit poll");
                    //too close! just fail the popup and keep playing the game
                    return false;
                }
                else
                {
                    spawnPosition = CognitiveVR_Manager.HMD.position + (spawnPosition - CognitiveVR_Manager.HMD.position).normalized * (hit.distance);
                }
            }

            pos = spawnPosition;
            return true;
        }
    }
}