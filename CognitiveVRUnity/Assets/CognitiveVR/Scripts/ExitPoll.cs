using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;

namespace CognitiveVR
{
    namespace Json
    {
        public class ExitPollSetJson
        {
            public string id;
            public ExitPollSetJsonEntry[] questions;

            //this is what the panel will display
            public class ExitPollSetJsonEntry
            {
                public string title;
                public string type;
                public ExitPollSetJsonEntryAnswer[] answers;

                public class ExitPollSetJsonEntryAnswer
                {
                    public string answer;
                }
            }
        }
    }

    //static class for requesting exitpoll question sets with multiple panels
    public static class ExitPoll
    {
        private static GameObject _exitPoolBoolean;
        public static GameObject ExitPollBoolean
        {
            get
            {
                if (_exitPoolBoolean == null)
                    _exitPoolBoolean = Resources.Load<GameObject>("ExitPollBoolean");
                return _exitPoolBoolean;
            }
        }
        private static GameObject _exitPoolInteger;
        public static GameObject ExitPollInteger
        {
            get
            {
                if (_exitPoolInteger == null)
                    _exitPoolInteger = Resources.Load<GameObject>("ExitPollInteger");
                return _exitPoolInteger;
            }
        }

        private static GameObject _exitPoolMultiple;
        public static GameObject ExitPollMultiple
        {
            get
            {
                if (_exitPoolMultiple == null)
                    _exitPoolMultiple = Resources.Load<GameObject>("ExitPollMultiple");
                return _exitPoolMultiple;
            }
        }

        private static GameObject _exitPoolVoice;
        public static GameObject ExitPollVoice
        {
            get
            {
                if (_exitPoolVoice == null)
                    _exitPoolVoice = Resources.Load<GameObject>("ExitPollVoice");
                return _exitPoolVoice;
            }
        }

        private static GameObject _exitPoolReticle;
        public static GameObject ExitPollReticle
        {
            get
            {
                if (_exitPoolReticle == null)
                    _exitPoolReticle = Resources.Load<GameObject>("ExitPollReticle");
                return _exitPoolReticle;
            }
        }

        public static ExitPollSet CurrentExitPollSet;
        public static ExitPollSet NewExitPoll()
        {
            CurrentExitPollSet = new ExitPollSet();
            return CurrentExitPollSet;
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

        //how to display all the panels and their properties
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
                EndAction.Invoke();
                Debug.Log("You haven't specified a question hook to request!");
                return;
            }

            CognitiveVR_Manager.Instance.StartCoroutine(RequestQuestions());
        }

        //build a collection of panel properties from the response
        IEnumerator RequestQuestions()
        {
            //spiderboss/hooks/questionsets. ask hook by id what their questionset is
            //CognitiveVR_Preferences.Instance.CustomerID
            string url = "http://data.cognitivevr.io/hooks/" + RequestQuestionHookName + "/questions";

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
                EndAction.Invoke();
            }
            else
            {
                //build all the panel properties
                Json.ExitPollSetJson json = JsonUtility.FromJson<Json.ExitPollSetJson>(www.text);

                if (json.questions.Length == 0)
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
                    //put this into a csv string?
                    string csvMultipleAnswers = "";
                    for (int j = 0; j< json.questions[i].answers.Length;j++)
                    {
                        csvMultipleAnswers += json.questions[i].answers[j] + "|";
                    }
                    if (csvMultipleAnswers.Length > 0)
                    {
                        csvMultipleAnswers = csvMultipleAnswers.Remove(csvMultipleAnswers.Length - 1);
                        questionVariables.Add("csvanswers", csvMultipleAnswers);
                    }
                    panelProperties.Add(questionVariables);
                }

                IterateToNextQuestion();
            }
        }

        //after a panel has been answered, the responses from each panel

        //these go to the dashboard
        Dictionary<string, object> transactionProperties = new Dictionary<string, object>();
        Dictionary<string, string> voiceResponses = new Dictionary<string, string>();

        //called from panel when a panel closes (after timeout, on close or on answer)
        public void OnPanelClosed(string key, object objectValue)
        {
            if (key != "voice")
            {
                transactionProperties.Add(key,objectValue);
            }
            else
            {
                transactionProperties.Add(key, "voice");
            }
            IterateToNextQuestion();
        }

        public void OnPanelClosedVoice(string key, string base64voice)
        {
            voiceResponses.Add(key, base64voice);
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
                CurrentExitPollPanel = null;
            }

            if (!useLastPanelPosition)
            {
                if (!GetSpawnPosition(out lastPanelPosition))
                {
                    Debug.Log("no last position set. invoke endaction and exit");
                    EndAction.Invoke();
                    return;
                }
            }

            //if next question, display that
            if (panelProperties.Count > 0)
            {
                DisplayPanel(panelProperties[0], PanelCount, lastPanelPosition);
                panelProperties.RemoveAt(0);
            }
            else
            {
                SendResponsesAsTransaction();
                var responses = FormatResponses();
                SendQuestionResponses(responses);
            }
            PanelCount++;
        }

        void SendResponsesAsTransaction()
        {
            var exitpoll = Instrumentation.Transaction("cvr.exitpoll");
            exitpoll.setProperty("userId", CognitiveVR.Core.userId);
            exitpoll.setProperty("questionSetId", QuestionSetId);
            exitpoll.setProperty("hookName", RequestQuestionHookName);
            
            foreach (var property in transactionProperties)
            {
                exitpoll.setProperty(property.Key, property.Value);
            }
            exitpoll.beginAndEnd(CurrentExitPollPanel.transform.position);
        }

        //puts responses from questions into json
        string FormatResponses()
        {
            //overwrite base64 data into json
            foreach (var response in voiceResponses)
            {
                transactionProperties[response.Key] = response.Value;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");
            builder.Append(JsonUtil.SetString("userId",CognitiveVR.Core.userId));
            builder.Append(",");
            builder.Append(JsonUtil.SetString("questionSetId", QuestionSetId));
            builder.Append(",");
            builder.Append(JsonUtil.SetString("hookName", RequestQuestionHookName));
            foreach (var property in transactionProperties)
            {
                if (property.Value.GetType() == typeof(string))
                    builder.Append(JsonUtil.SetString(property.Key, (string)property.Value));
                else
                    builder.Append(JsonUtil.SetObject(property.Key, property.Value));
                builder.Append(",");
            }
            builder.Remove(builder.Length - 1,1); //remove comma
            builder.Append("}");

            return builder.ToString();
        }

        //the responses of all the questions in the set put together in a string and uploaded somewhere
        //each question is already sent as a transaction
        void SendQuestionResponses(string responses)
        {
            Debug.Log("all questions answered! format string and send responses!");

            string url = "https://api.cognitivevr.io/" + QuestionSetId + "/responses";
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(responses);

            Debug.Log("ExitPoll Request\n" + responses);

            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            //WWW www = new UnityEngine.WWW(url, bytes, headers);
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
                case "boolean":
                    prefab = ExitPoll.ExitPollBoolean;
                    break;
                case "integer":
                    prefab = ExitPoll.ExitPollInteger;
                    break;
                case "multiple":
                    prefab = ExitPoll.ExitPollMultiple;
                    break;
                case "voice":
                    prefab = ExitPoll.ExitPollVoice;

                    break;
            }
            if (prefab == null)
            {
                Debug.Log("couldn't find prefab " + properties["name"]);
                EndAction.Invoke();
                return;
            }

            var newPanelGo = GameObject.Instantiate<GameObject>(prefab);
            var panel = newPanelGo.GetComponent<ExitPollPanel>();

            panel.Initialize(properties, panelId, this);
        }

        float DisplayDistance = 3;

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
                    EndAction.Invoke();
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