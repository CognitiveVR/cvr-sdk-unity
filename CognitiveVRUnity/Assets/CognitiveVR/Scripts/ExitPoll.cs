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
                public int maxResponseLength;
                public ExitPollScaleRange range;
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
        public static ExitPollSet NewExitPoll()
        {
            if (CurrentExitPollSet != null)
            {
                CurrentExitPollSet.EndQuestionSet();
            }
            CurrentExitPollSet = new ExitPollSet();
            return CurrentExitPollSet;
        }

        public static void Initialize()
        {
            //request questions
            //if response, write response to disk
            //else read cached questions from disk
            //CognitiveVR_Manager.InitEvent += ReadQuestions;

            //read answers from disk
            //send answers to microservice
            //CognitiveVR_Manager.InitEvent += WriteAnswers;
        }

        static Dictionary<string, string> ExitPollQuestions = new Dictionary<string, string>();

        private static void ReadQuestions(Error initError)
        {
            CognitiveVR_Manager.InitEvent -= ReadQuestions;
            if (initError == Error.Success)
            {
                //begin request. on complete, save to disk
                //persistentdatapath/exitpollquestions
                CognitiveVR_Manager.Instance.StartCoroutine(RequestQuestions());
            }
            else
            {
                if (CognitiveVR_Manager.Instance.SaveExitPollOnDevice)
                {
                    //read saved exit poll from disk
                    List<string> fileNames = new List<string>();
                    //Application.persistentDataPath/exitpollquestions/

                    for (int i = 0; i < fileNames.Count; i++)
                    {
                        ExitPollQuestions.Add(System.IO.Path.GetFileNameWithoutExtension(fileNames[i]), System.IO.File.ReadAllText(fileNames[i]));
                    }
                }
            }
        }

        private static void WriteAnswers(Error initError)
        {
            CognitiveVR_Manager.InitEvent -= WriteAnswers;
            if (initError != Error.Success)
            {
                //couldn't connect. keep files in cache
            }
            else
            {
                if (CognitiveVR_Manager.Instance.SaveExitPollOnDevice)
                {
                    //connected. send responses
                    CognitiveVR_Manager.Instance.StartCoroutine(SendCachedResponses());
                }
            }
        }

        static IEnumerator RequestQuestions()
        {
            string url = "http://api.cognititivevr.io/customerid/allquestions";
            url = "nowhere";
            WWW responseRequest = new WWW(url);
            yield return responseRequest;
            if (responseRequest.error.Length == 0)
            {

            }
            else
            {
                var questions = responseRequest.text.Split('|');

                //write to question dictionary

                if (CognitiveVR_Manager.Instance.SaveExitPollOnDevice)
                {
                    string dataPath = Application.persistentDataPath;
                    for (int i = 0; i < questions.Length; i++)
                    {
                        Json.ExitPollSetJson json = JsonUtility.FromJson<Json.ExitPollSetJson>(questions[i]);
                        System.IO.File.WriteAllText(dataPath + json.id, questions[i]);
                    }
                }
            }
        }

        public static string GetExitPollQuestion(string questionName)
        {
            if (ExitPollQuestions.ContainsKey(questionName))
                return ExitPollQuestions[questionName];
            return string.Empty;
        }

        static IEnumerator SendCachedResponses()
        {
            var jsonResponses = new List<string>();
            //get all files in directory persistentDataPath/exitpollresponse
            //jsonResponses.Add(Application.persistentDataPath)


            for (int i = jsonResponses.Count - 1; i >= 0; i++)
            {
                //hook name needs to be pulled out of json text file so it knows where to send
                string url;
                //string url = "http://api.cognititivevr.io/"+CognitiveVR_Preferences.Instance.CustomerID+"/"+ +"/responses";
                url = "nowhere";
                WWW responseRequest = new WWW(url);
                yield return responseRequest;
                if (responseRequest.error.Length == 0)
                {
                    //response was fine
                    jsonResponses.RemoveAt(i);
                    //Application.persistentDataPath/exitpollresponse
                    //remove file from persistent data path
                }
            }
        }
    }

    //creates a series of exit poll panels from question set constructed on the dashboard
    public class ExitPollSet
    {
        System.Action EndAction;
        
        //layermask should maybe be static and setable through public function
        LayerMask PanelLayerMask = LayerMask.GetMask("Default", "World", "Ground");
        //min display distance should maybe be static and setable through public function
        float MinimumDisplayDistance = 1;
        float DisplayDistance = 3;

        //how to display all the panels and their properties. dictionary is <panelType,panelContent>
        List<Dictionary<string, string>> panelProperties = new List<Dictionary<string, string>>();

        public ExitPollPanel CurrentExitPollPanel;

        string RequestQuestionHookName = "";
        string QuestionSetId;

        //get the json response from the server when Begin() is called. then construct the panel properties from the response
        public ExitPollSet LoadQuestionFromID(string name)
        {
            RequestQuestionHookName = name;
            return this;
        }

        public ExitPollSet SetEndAction(System.Action endAction)
        {
            EndAction = endAction;
            return this;
        }

        public void Begin()
        {
            if (string.IsNullOrEmpty(RequestQuestionHookName))
            {
                if (EndAction != null)
                {
                    EndAction.Invoke();
                }
                Debug.Log("CognitiveVR Exit Poll. You haven't specified a question hook to request!");
                return;
            }

            CognitiveVR_Manager.Instance.StartCoroutine(RequestQuestions());
        }

        //only called when requesting a new exit poll question set when one is already active
        public void EndQuestionSet()
        {
            panelProperties.Clear();
            CurrentExitPollPanel.CloseButton();
        }

        //TODO this should grab a question received and cached on CognitiveVRManager Init
        //build a collection of panel properties from the response
        IEnumerator RequestQuestions()
        {
            //hooks/questionsets. ask hook by id what their questionset is
            string url = "https://api.cognitivevr.io/products/" + CognitiveVR_Preferences.Instance.CustomerID + "/questionSetHooks/" + RequestQuestionHookName + "/questionSet";

            WWW www = new WWW(url);

            float time = 0;
            while (time < 3) //wait a maximum of 3 seconds
            {
                yield return null;
                if (www.isDone) break;
                time += Time.deltaTime;
            }
            if (!www.isDone)
            {
                CognitiveVR.Util.logDebug("www request question timeout");
                if (EndAction != null)
                {
                    EndAction.Invoke();
                }
            }
            else
            {
                Debug.Log("Exit Poll Question Response:\n" + www.text);
                //build all the panel properties
                Json.ExitPollSetJson json = JsonUtility.FromJson<Json.ExitPollSetJson>(www.text);
                //Json.ExitPollSetJson json = JsonUtility.FromJson<Json.ExitPollSetJson>(ExitPoll.GetExitPollQuestion(RequestQuestionHookName));
                

                if (json.questions == null || json.questions.Length == 0)
                {
                    Debug.Log("Exit poll Question response not formatted correctly!");
                    yield break;
                }

                QuestionSetId = json.id;

                //foreach (var question in json.questions)
                for (int i = 0; i< json.questions.Length; i++)
                {
                    Dictionary<string, string> questionVariables = new Dictionary<string, string>();
                    questionVariables.Add("title", json.questions[i].title);
                    questionVariables.Add("type", json.questions[i].type);
                    responseProperties.Add(new ResponseContext(json.questions[i].type));
                    questionVariables.Add("maxResponseLength", json.questions[i].maxResponseLength.ToString());
                    //put this into a csv string?

                    if (json.questions[i].range != null)
                    {
                        questionVariables.Add("startvalue", json.questions[i].range.start.ToString());
                        questionVariables.Add("endvalue", json.questions[i].range.end.ToString());
                    }

                    string csvMultipleAnswers = "";
                    if (json.questions[i].answers != null)
                    {
                        for (int j = 0; j < json.questions[i].answers.Length; j++)
                        {
                            if (json.questions[i].answers[j].answer.Length == 0) { continue; }
                            //TODO include support for custom icons on multiple choice answers
                            csvMultipleAnswers += json.questions[i].answers[j].answer + "|";
                            Debug.Log("answer " + json.questions[i].answers[j].answer);
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
        public void OnPanelClosed(int panelId, string key, object objectValue)
        {
            if (key != "voice")
            {
                transactionProperties.Add(key,objectValue);
                responseProperties[panelId].ResponseValue = objectValue;
            }
            else
            {
                transactionProperties.Add(key, "voice");
            }
            IterateToNextQuestion();
        }

        public void OnPanelClosedVoice(int panelId, string key, string base64voice)
        {
            //voiceResponses.Add(key, base64voice);
            responseProperties[panelId].ResponseValue = base64voice;
        }

        int PanelCount = 0;
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
                    Debug.Log("no last position set. invoke endaction and exit");
                    if (EndAction != null)
                    {
                        EndAction.Invoke();
                    }
                    return;
                }
            }

            //if next question, display that
            if (panelProperties.Count > 0)
            {
                DisplayPanel(panelProperties[0], PanelCount, lastPanelPosition);
                panelProperties.RemoveAt(0);
            }
            else //finished everything format and send
            {
                SendResponsesAsTransaction(); //for personalization api
                var responses = FormatResponses();
                SendQuestionResponses(responses); //for exitpoll microservice
                CurrentExitPollPanel = null;
            }
            PanelCount++;
        }

        void SendResponsesAsTransaction()
        {
            var exitpoll = Instrumentation.Transaction("cvr.exitpoll");
            exitpoll.setProperty("userId", CognitiveVR.Core.userId);
            exitpoll.setProperty("questionSetId", QuestionSetId);
            exitpoll.setProperty("hook", RequestQuestionHookName);
            
            foreach (var property in transactionProperties)
            {
                exitpoll.setProperty(property.Key, property.Value);
            }
            exitpoll.beginAndEnd(CurrentExitPollPanel.transform.position);
        }

        //puts responses from questions into json for exitpoll microservice
        string FormatResponses()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");
            builder.Append(JsonUtil.SetString("userId",CognitiveVR.Core.userId));
            builder.Append(",");
            builder.Append(JsonUtil.SetString("questionSetId", QuestionSetId));
            builder.Append(",");
            builder.Append(JsonUtil.SetString("hook", RequestQuestionHookName));
            builder.Append(",");

            builder.Append("\"answers\":[");

            for (int i = 0; i<responseProperties.Count; i++)
            {
                var valueString = responseProperties[i].ResponseValue as string;
                if (!string.IsNullOrEmpty(valueString) && valueString == "skip")
                {
                    builder.Append("null,");
                }
                else
                {
                    builder.Append("{");
                    builder.Append(JsonUtil.SetString("type", responseProperties[i].QuestionType));
                    builder.Append(",\"value\":");

                    if (!string.IsNullOrEmpty(valueString))
                    {
                        builder.Append("\"" + valueString + "\"");
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
            builder.Remove(builder.Length - 1,1); //remove comma
            builder.Append("]");
            builder.Append("}");

            return builder.ToString();
        }

        //the responses of all the questions in the set put together in a string and uploaded somewhere
        //each question is already sent as a transaction
        void SendQuestionResponses(string responses)
        {
            Debug.Log("all questions answered! format string and send responses!");

            string url = "https://api.cognitivevr.io/" + CognitiveVR_Preferences.Instance.CustomerID + "/" + RequestQuestionHookName + "/responses";
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(responses);

            Debug.Log("ExitPoll Send Answers\n" + responses);

            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            WWW www = new UnityEngine.WWW(url, bytes, headers);
        }

        public bool UseTimeout { get; private set; }
        public float Timeout { get; private set; }
        /// <summary>
        /// set a maximum time that a question will be displayed. if this is passed, the question closes automatically
        /// </summary>
        /// <param name="allowTimeout"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public ExitPollSet SetTimeout(bool allowTimeout, float timeout)
        {
            UseTimeout = allowTimeout;
            Timeout = timeout;
            return this;
        }

        public bool DisplayReticle { get; private set; }
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

        void DisplayPanel(Dictionary<string, string> properties,int panelId, Vector3 spawnPoint)
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
                Debug.Log("couldn't find prefab " + properties["type"]);
                if (EndAction != null)
                {
                    EndAction.Invoke();
                }
                return;
            }

            var newPanelGo = GameObject.Instantiate<GameObject>(prefab);
            newPanelGo.transform.position = spawnPoint;
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