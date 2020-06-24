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
        public enum PointerSource
        {
            HMD,
            RightHand,
            LeftHand,
            Other
        }
        public enum SpawnType
        {
            World,
            PlayerRelative
        }
        public enum PointerType
        {
            HMDPointer,
            ControllerPointer,
            CustomPointer,
            SceneObject
        }

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

        public static ExitPollParameters NewExitPoll(string hookName)
        {
            var CurrentExitPollParams = new ExitPollParameters();
            CurrentExitPollParams.Hook = hookName;
            return CurrentExitPollParams;
        }
        public static ExitPollParameters NewExitPoll(string hookName, ExitPollParameters parameters)
        {
            parameters.Hook = hookName;
            return parameters;
        }
    }

    //creates a series of exit poll panels from question set constructed on the dashboard
    public class ExitPollSet
    {
        public ExitPollPanel CurrentExitPollPanel;

        public ExitPollParameters myparameters;

        GameObject pointerInstance = null;
        double StartTime;

        public void BeginExitPoll(ExitPollParameters parameters)
        {
            myparameters = parameters;

            //spawn pointers if override isn't set
            if (parameters.PointerType == ExitPoll.PointerType.SceneObject)
            {
                //spawn nothing. something in the scene is already set   
                pointerInstance = parameters.PointerOverride;
            }
            else if (parameters.PointerType == ExitPoll.PointerType.HMDPointer)
            {
                GameObject prefab = Resources.Load<GameObject>("HMDPointer");
                if (prefab != null)
                    pointerInstance = GameObject.Instantiate(prefab);
                else
                    Debug.LogError("Spawning Exitpoll HMD Pointer, but cannot find prefab \"HMDPointer\" in Resources!");
            }
            else if (parameters.PointerType == ExitPoll.PointerType.ControllerPointer)
            {
                GameObject prefab = Resources.Load<GameObject>("ControllerPointer");
                if (prefab != null)
                    pointerInstance = GameObject.Instantiate(prefab);
                else
                    Debug.LogError("Spawning Exitpoll Controller Pointer, but cannot find prefab \"ControllerPointer\" in Resources!");
            }
            else if (parameters.PointerType == ExitPoll.PointerType.CustomPointer)
            {
                if (parameters.PointerOverride != null)
                    pointerInstance = GameObject.Instantiate(parameters.PointerOverride);
                else
                    Debug.LogError("Spawning Exitpoll Pointer, but cannot pointer override prefab is null!");
            }
            
            if (pointerInstance != null)
            {
                if (parameters.PointerParent == ExitPoll.PointerSource.HMD)
                {
                    //parent to hmd and zero position
                    pointerInstance.transform.SetParent(GameplayReferences.HMD);
                    pointerInstance.transform.localPosition = Vector3.zero;
                    pointerInstance.transform.localRotation = Quaternion.identity;
                }
                else if (parameters.PointerParent == ExitPoll.PointerSource.RightHand)
                {
                    Transform t = null;
                    if (GameplayReferences.GetController(true, out t))
                    {
                        pointerInstance.transform.SetParent(t);
                        pointerInstance.transform.localPosition = Vector3.zero;
                        pointerInstance.transform.localRotation = Quaternion.identity;
                    }
                }
                else if (parameters.PointerParent == ExitPoll.PointerSource.LeftHand)
                {
                    Transform t = null;
                    if (GameplayReferences.GetController(false, out t))
                    {
                        pointerInstance.transform.SetParent(t);
                        pointerInstance.transform.localPosition = Vector3.zero;
                        pointerInstance.transform.localRotation = Quaternion.identity;
                    }
                }
                else if (parameters.PointerParent == ExitPoll.PointerSource.Other)
                {
                    if (parameters.PointerParentOverride != null)
                    {
                        pointerInstance.transform.SetParent(parameters.PointerParentOverride);
                        pointerInstance.transform.localPosition = Vector3.zero;
                        pointerInstance.transform.localRotation = Quaternion.identity;
                    }
                }
            }

            //this should take all previously set variables (from functions) and create an exitpoll parameters object

            currentPanelIndex = 0;
            if (string.IsNullOrEmpty(myparameters.Hook))
            {
                Cleanup(false);
                Util.logDebug("CognitiveVR Exit Poll. You haven't specified a question hook to request!");
                return;
            }

            if (CognitiveVR_Manager.Instance != null)
            {
                CognitiveVR.NetworkManager.GetExitPollQuestions(myparameters.Hook, QuestionSetResponse, 3);
            }
            else
            {
                Util.logDebug("Cannot display exitpoll. cognitiveVRManager not present in scene");
                Cleanup(false);
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

        //how to display all the panels and their properties. dictionary is <panelType,panelContent>
        List<Dictionary<string, string>> panelProperties = new List<Dictionary<string, string>>();

        int questionSetVersion;
        string QuestionSetName;

        string QuestionSetId; //questionsetname:questionsetversion

        Json.ExitPollSetJson questionSet;

        //IMPROVEMENT this should grab a question received and cached on CognitiveVRManager Init
        //build a collection of panel properties from the response
        void QuestionSetResponse(int responsecode, string error,string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                //question timeout or not found
                Cleanup(false);
                return;
            }


            //build all the panel properties
            try
            {
                questionSet = JsonUtility.FromJson<Json.ExitPollSetJson>(text);
            }
            catch
            {
                CognitiveVR.Util.logDebug("Exit poll Question response not formatted correctly! invoke end action");
                Cleanup(false);
                return;
            }

            if (questionSet.questions == null || questionSet.questions.Length == 0)
            {
                CognitiveVR.Util.logDebug("Exit poll Question response empty! invoke end action");
                Cleanup(false);
                return;
            }

            QuestionSetId = questionSet.id;
            QuestionSetName = questionSet.name;
            questionSetVersion = questionSet.version;

            //foreach (var question in json.questions)
            for (int i = 0; i < questionSet.questions.Length; i++)
            {
                Dictionary<string, string> questionVariables = new Dictionary<string, string>();
                if (!questionVariables.ContainsKey("title"))
                {
                    questionVariables.Add("title", questionSet.title);
                }
                questionVariables.Add("question", questionSet.questions[i].title);
                questionVariables.Add("type", questionSet.questions[i].type);
                responseProperties.Add(new ResponseContext(questionSet.questions[i].type));
                questionVariables.Add("maxResponseLength", questionSet.questions[i].maxResponseLength.ToString());

                if (!string.IsNullOrEmpty(questionSet.questions[i].minLabel))
                    questionVariables.Add("minLabel", questionSet.questions[i].minLabel);
                if (!string.IsNullOrEmpty(questionSet.questions[i].maxLabel))
                    questionVariables.Add("maxLabel", questionSet.questions[i].maxLabel);
                //put this into a csv string?

                if (questionSet.questions[i].range != null) //range question
                {
                    questionVariables.Add("start", questionSet.questions[i].range.start.ToString());
                    questionVariables.Add("end", questionSet.questions[i].range.end.ToString());
                }

                string csvMultipleAnswers = "";
                if (questionSet.questions[i].answers != null) //multiple choice question
                {
                    for (int j = 0; j < questionSet.questions[i].answers.Length; j++)
                    {
                        if (questionSet.questions[i].answers[j].answer.Length == 0) { continue; }
                        //IMPROVEMENT include support for custom icons on multiple choice answers. requires dashboard feature + sending png data
                        csvMultipleAnswers += questionSet.questions[i].answers[j].answer + "|";
                    }
                }
                if (csvMultipleAnswers.Length > 0)
                {
                    csvMultipleAnswers = csvMultipleAnswers.Remove(csvMultipleAnswers.Length - 1); //last pipe
                    questionVariables.Add("csvanswers", csvMultipleAnswers);
                }
                panelProperties.Add(questionVariables);
            }

            if (myparameters.OnBegin != null)
                myparameters.OnBegin.Invoke();

            StartTime = Util.Timestamp();
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
            Cleanup(false);
            CognitiveVR.Util.logDebug("Exit poll OnPanelError - HMD is null, manually closing question set or new exit poll while one is active");
        }

        int currentPanelIndex = 0;
        int panelCount = 0;
        void IterateToNextQuestion()
        {
            if (GameplayReferences.HMD == null)
            {
                Cleanup(false);
                return;
            }

            bool useLastPanelPosition = false;
            Vector3 lastPanelPosition = Vector3.zero;
            Quaternion lastPanelRotation = Quaternion.identity;

            //close current panel
            if (CurrentExitPollPanel != null)
            {
                lastPanelPosition = CurrentExitPollPanel.transform.position;
                lastPanelRotation = CurrentExitPollPanel.transform.rotation;
                useLastPanelPosition = true;
                //CurrentExitPollPanel = null;
            }

            if (!useLastPanelPosition)
            {
                if (!GetSpawnPosition(out lastPanelPosition))
                {
                    CognitiveVR.Util.logDebug("no last position set. invoke endaction");
                    Cleanup(false);
                    return;
                }
            }

            //if next question, display that
            if (panelProperties.Count > 0 && currentPanelIndex < panelProperties.Count)
            {
                //DisplayPanel(panelProperties[currentPanelIndex], panelCount, lastPanelPosition);
                var prefab = GetPrefab(panelProperties[currentPanelIndex]);
                if (prefab == null)
                {
                    Util.logError("couldn't find prefab " + panelProperties[currentPanelIndex]);
                    Cleanup(false);
                    return;
                }

                Vector3 spawnPosition = lastPanelPosition;
                Quaternion spawnRotation = lastPanelRotation;

                if (currentPanelIndex == 0)
                {
                    //figure out world spawn position
                    if (myparameters.UseOverridePosition || myparameters.ExitpollSpawnType == ExitPoll.SpawnType.World)
                        spawnPosition = myparameters.OverridePosition;
                    if (myparameters.UseOverrideRotation || myparameters.ExitpollSpawnType == ExitPoll.SpawnType.World)
                        spawnRotation = myparameters.OverrideRotation;
                }
                var newPanelGo = GameObject.Instantiate<GameObject>(prefab,spawnPosition,spawnRotation);

                CurrentExitPollPanel = newPanelGo.GetComponent<ExitPollPanel>();

                if (CurrentExitPollPanel == null)
                {
                    Debug.LogError(newPanelGo.gameObject.name + " does not have ExitPollPanel component!");
                    GameObject.Destroy(newPanelGo);
                    Cleanup(false);
                    return;
                }
                CurrentExitPollPanel.Initialize(panelProperties[currentPanelIndex], panelCount, this);

                if (myparameters.ExitpollSpawnType == ExitPoll.SpawnType.World && myparameters.UseAttachTransform)
                {
                    if (myparameters.AttachTransform != null)
                    {
                        newPanelGo.transform.SetParent(myparameters.AttachTransform);
                    }
                }
            }
            else //finished everything format and send
            {
                SendResponsesAsTransaction(); //for personalization api
                var responses = FormatResponses();
                NetworkManager.PostExitpollAnswers(responses, QuestionSetName, questionSetVersion); //for exitpoll microservice
                CurrentExitPollPanel = null;
                Cleanup(true);
            }
            panelCount++;
        }

        void SendResponsesAsTransaction()
        {
            var exitpollEvent = new CustomEvent("cvr.exitpoll");
            exitpollEvent.SetProperty("userId", CognitiveVR.Core.DeviceId);
            if (!string.IsNullOrEmpty(Core.ParticipantId))
            {
                exitpollEvent.SetProperty("participantId", CognitiveVR.Core.ParticipantId);
            }
            exitpollEvent.SetProperty("questionSetId", QuestionSetId);
            exitpollEvent.SetProperty("hook", myparameters.Hook);
            exitpollEvent.SetProperty("duration", Util.Timestamp() - StartTime);

            var scenesettings = Core.TrackingScene;
            if (scenesettings != null && !string.IsNullOrEmpty(scenesettings.SceneId))
            {
                exitpollEvent.SetProperty("sceneId", scenesettings.SceneId);
            }

            foreach (var property in transactionProperties)
            {
                exitpollEvent.SetProperty(property.Key, property.Value);
            }
            exitpollEvent.Send(CurrentExitPollPanel.transform.position);
            Core.InvokeSendDataEvent();
        }

        //puts responses from questions into json for exitpoll microservice
        string FormatResponses()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");
            JsonUtil.SetString("userId", CognitiveVR.Core.DeviceId, builder);
            builder.Append(",");
            if (!string.IsNullOrEmpty(Core.ParticipantId))
            {
                JsonUtil.SetString("participantId", CognitiveVR.Core.ParticipantId, builder);
                builder.Append(",");
            }
            if (!string.IsNullOrEmpty(Core.LobbyId))
            {
                JsonUtil.SetString("lobbyId", Core.LobbyId, builder);
                builder.Append(",");
            }
            JsonUtil.SetString("questionSetId", QuestionSetId, builder);
            builder.Append(",");
            JsonUtil.SetString("sessionId", Core.SessionID, builder);
            builder.Append(",");
            JsonUtil.SetString("hook", myparameters.Hook, builder);
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

        GameObject GetPrefab(Dictionary<string, string> properties)
        {
            GameObject prefab = null;
            switch (properties["type"])
            {
                case "HAPPYSAD":
                    if (myparameters.HappyPanelOverride != null)
                    {
                        prefab = myparameters.HappyPanelOverride;
                    }
                    else
                    {
                        prefab = ExitPoll.ExitPollHappySad;
                    }
                    break;
                case "SCALE":
                    if (myparameters.ScalePanelOverride != null)
                    {
                        prefab = myparameters.ScalePanelOverride;
                    }
                    else
                    {
                        prefab = ExitPoll.ExitPollScale;
                    }
                    break;
                case "MULTIPLE":
                    if (myparameters.MultiplePanelOverride != null)
                    {
                        prefab = myparameters.MultiplePanelOverride;
                    }
                    else
                    {
                        prefab = ExitPoll.ExitPollMultiple;
                    }
                    break;
                case "VOICE":
                    if (myparameters.VoicePanelOverride != null)
                    {
                        prefab = myparameters.VoicePanelOverride;
                    }
                    else
                    {
                        prefab = ExitPoll.ExitPollVoice;
                    }
                    break;
                case "THUMBS":
                    if (myparameters.ThumbsPanelOverride != null)
                    {
                        prefab = myparameters.ThumbsPanelOverride;
                    }
                    else
                    {
                        prefab = ExitPoll.ExitPollThumbs;
                    }
                    break;
                case "BOOLEAN":
                    if (myparameters.BoolPanelOverride != null)
                    {
                        prefab = myparameters.BoolPanelOverride;
                    }
                    else
                    {
                        prefab = ExitPoll.ExitPollTrueFalse;
                    }
                    break;
            }
            return prefab;
        }

        /// <summary>
        /// returns true if there's a valid position
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        bool GetSpawnPosition(out Vector3 pos)
        {
            pos = Vector3.zero;
            if (GameplayReferences.HMD == null) //no hmd? fail
            {
                return false;
            }

            //set position and rotation
            Vector3 spawnPosition = GameplayReferences.HMD.position + GameplayReferences.HMD.forward * myparameters.DisplayDistance;

            if (myparameters.LockYPosition)
            {
                Vector3 modifiedForward = GameplayReferences.HMD.forward;
                modifiedForward.y = 0;
                modifiedForward.Normalize();

                spawnPosition = GameplayReferences.HMD.position + modifiedForward * myparameters.DisplayDistance;
            }

            RaycastHit hit = new RaycastHit();

            if (myparameters.PanelLayerMask.value != 0)
            {
                //test slightly in front of the player's hmd
                Collider[] colliderHits = Physics.OverlapSphere(GameplayReferences.HMD.position + Vector3.forward * 0.5f, 0.5f, myparameters.PanelLayerMask);
                if (colliderHits.Length > 0)
                {
                    Util.logDebug("ExitPoll.Initialize hit collider " + colliderHits[0].gameObject.name + " too close to player. Skip exit poll");
                    //too close! just fail the popup and keep playing the game
                    return false;
                }

                //ray from player's hmd position
                if (Physics.SphereCast(GameplayReferences.HMD.position, 0.5f, spawnPosition - GameplayReferences.HMD.position, out hit, myparameters.DisplayDistance, myparameters.PanelLayerMask))
                {
                    if (hit.distance < myparameters.MinimumDisplayDistance)
                    {
                        Util.logDebug("ExitPoll.Initialize hit collider " + hit.collider.gameObject.name + " too close to player. Skip exit poll");
                        //too close! just fail the popup and keep playing the game
                        return false;
                    }
                    else
                    {
                        spawnPosition = GameplayReferences.HMD.position + (spawnPosition - GameplayReferences.HMD.position).normalized * (hit.distance);
                    }
                }
            }

            pos = spawnPosition;
            return true;
        }

        //calls end actions
        //calls parameter end events
        //destroys spawned pointers
        void Cleanup(bool completedSuccessfully)
        {
            if (pointerInstance != null)
            {
                if (myparameters.PointerType != ExitPoll.PointerType.SceneObject)
                {
                    GameObject.Destroy(pointerInstance);
                }
                else //if pointertype == SceneObject
                {
                    if (myparameters.PointerParent != ExitPoll.PointerSource.Other)
                    {
                        //unparent
                        pointerInstance.transform.SetParent(null);
                    }
                    else
                    {
                        if (myparameters.PointerParentOverride == null)
                        {
                            //not parented at startup. don't unparent
                        }
                        else
                        {
                            pointerInstance.transform.SetParent(null);
                        }
                    }
                }
            }

            if (myparameters.OnComplete != null && completedSuccessfully == true)
                myparameters.OnComplete.Invoke();

            if (myparameters.OnClose != null)
                myparameters.OnClose.Invoke();

            if (myparameters.EndAction != null)
            {
                myparameters.EndAction.Invoke(completedSuccessfully);
            }
        }
    }
}