using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;


namespace Cognitive3D
{
    public class ExitPollSet
    {
        public ExitPollPanel CurrentExitPollPanel;

        public ExitPollParameters myparameters;

        double StartTime;
        private float noTrackingCountdown;
        private const float NO_TRACKING_COUNTDOWN_LIMIT = 35;
        private readonly string FALLBACK_TO_HMD_POINTER = $"Controller or hands not found for {NO_TRACKING_COUNTDOWN_LIMIT}! Using HMD Pointer.";

#region Begin, End, Update
        public void BeginExitPoll(ExitPollParameters parameters)
        {
            if (!Cognitive3D_Manager.IsInitialized)
            {
                Util.logDebug("Cannot display exitpoll. Session has not begun");
                Cleanup(false);
                return;
            }

            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            InputTracking.trackingLost += ExitPollPointer.OnTrackingLost;
            InputTracking.trackingAcquired += ExitPollPointer.OnTrackingRegained;
            
            myparameters = ExitPollPointer.currentExitPollParameters = parameters;
            noTrackingCountdown = 0;
            switch (parameters.PointerType)
            {
                case ExitPollManager.PointerType.HMD:
                    ExitPollPointer.SetUpHMDAsPointer(parameters.HMDPointerPrefab);
                    break;

                case ExitPollManager.PointerType.ControllersAndHands:
                    ExitPollPointer.SetupControllerAsPointer(parameters.PointerControllerPrefab, parameters.PointerActivationButton, parameters.PointerLineWidth, parameters.PointerGradient);
                    break;

                case ExitPollManager.PointerType.Custom:
                    // Check docs for best practices
                    break;
            }
            
            //this should take all previously set variables (from functions) and create an exitpoll parameters object
            currentPanelIndex = 0;
            if (string.IsNullOrEmpty(myparameters.Hook))
            {
                Cleanup(false);
                Util.logDebug("Cognitive3D Exit Poll. You haven't specified a question hook to request!");
                return;
            }

            if (Cognitive3D_Manager.Instance != null)
            {
                Cognitive3D.NetworkManager.GetExitPollQuestions(myparameters.Hook, QuestionSetResponse, 3);
            }
            else
            {
                Util.logDebug("Cannot display exitpoll. Cognitive3DManager not present in scene");
                Cleanup(false);
            }
        }

        /// <summary>
        /// When you manually need to close the Exit Poll question set manually OR <br/>
        /// when requesting a new exit poll question set when one is already active
        /// </summary>
        public void EndQuestionSet(int timeToWait)
        {
            panelProperties.Clear();
            if (CurrentExitPollPanel != null)
            {
                CurrentExitPollPanel.CloseError(timeToWait);
            }
            OnPanelError();
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // Increment counter if controller pointer exists and tracking type is none
            if (GameplayReferences.PointerController != null && GameplayReferences.GetCurrentTrackedDevice() == GameplayReferences.TrackingType.None)
            {
                noTrackingCountdown += deltaTime;
                
                // Limit reached: destroy controller pointer and fallback to HMD pointer
                if (noTrackingCountdown >= NO_TRACKING_COUNTDOWN_LIMIT)
                {
                    noTrackingCountdown = 0;
                    GameObject.Destroy(GameplayReferences.PointerController);
                    ExitPollPointer.SetUpHMDAsPointer(myparameters.HMDPointerPrefab);
                    ExitPollPointer.DisplayControllerError(true, FALLBACK_TO_HMD_POINTER);
                }
            }
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
            InputTracking.trackingLost -= ExitPollPointer.OnTrackingLost;
            InputTracking.trackingAcquired -= ExitPollPointer.OnTrackingRegained;
        }
#endregion

        /// <summary>
        /// A list containing information on each panel <br/>
        /// The kv pairs in the dictionary are the keys and values of each of the fields on the panel
        /// </summary>
        List<Dictionary<string, string>> panelProperties = new List<Dictionary<string, string>>();

        int questionSetVersion;
        string QuestionSetName;
        string QuestionSetId; //questionsetname:questionsetversion
        ExitPollData questionSet;

        //IMPROVEMENT this should grab a question received and cached on Cognitive3DManager Init
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
                questionSet = JsonUtility.FromJson<ExitPollData>(text);
            }
            catch
            {
                Util.logDebug("Exit poll Question response not formatted correctly! invoke end action");
                Cleanup(false);
                return;
            }

            if (questionSet.questions == null || questionSet.questions.Length == 0)
            {
                Util.logDebug("Exit poll Question response empty! invoke end action");
                Cleanup(false);
                return;
            }

            QuestionSetId = questionSet.id;
            QuestionSetName = questionSet.name;
            questionSetVersion = questionSet.version;

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
        Dictionary<string, object> eventProperties = new Dictionary<string, object>();

        //called from panel when a panel closes (after timeout, on close or on answer)
        public void OnPanelClosed(int panelId, string key, int objectValue)
        {
            eventProperties.Add(key, objectValue);
            responseProperties[panelId].ResponseValue = objectValue;
            currentPanelIndex++;
            IterateToNextQuestion();
        }

        public void OnPanelClosedVoice(int panelId, string key, string base64voice)
        {
            eventProperties.Add(key, 0);
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
            CurrentExitPollPanel = null;
            Cleanup(false);
            Util.logDebug("Exit poll OnPanelError - HMD is null, manually closing question set or new exit poll while one is active");
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
                    Cognitive3D.Util.logDebug("no last position set. invoke endaction");
                    Cleanup(false);
                    return;
                }
            }

            //if next question, display that
            if (panelProperties.Count > 0 && currentPanelIndex < panelProperties.Count)
            {
                //DisplayPanel(panelProperties[currentPanelIndex], panelCount, lastPanelPosition);
                var prefab = ExitPollUtil.GetPrefab(myparameters, panelProperties[currentPanelIndex]);
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
                    if (myparameters.UseOverridePosition || myparameters.ExitpollSpawnType == ExitPollManager.SpawnType.WorldSpace)
                        spawnPosition = myparameters.OverridePosition;
                    if (myparameters.UseOverrideRotation || myparameters.ExitpollSpawnType == ExitPollManager.SpawnType.WorldSpace)
                        spawnRotation = myparameters.OverrideRotation;
                }

                // Skip voice response if microphone not detected
                var currentPanelProperties = panelProperties[currentPanelIndex];
#if UNITY_WEBGL
                //skip voice questions on webgl - microphone is not supported
                if (currentPanelProperties["type"].Equals("VOICE"))
                {
                    int tempPanelID = panelCount; // OnPanelClosed takes in PanelID, but since panel isn't initialized yet, we use panelCount
                                                  // because that is what PanelID gets set to
                    panelCount++;
                    new Cognitive3D.CustomEvent("c3d.ExitPoll detected no microphones")
                        .SetProperty("Panel ID", tempPanelID)
                        .Send();
                    OnPanelClosed(tempPanelID, "Answer" + tempPanelID, short.MinValue);
                    return;
                }
#else
                if (currentPanelProperties["type"].Equals("VOICE") && Microphone.devices.Length == 0)
                {
                    int tempPanelID = panelCount; // OnPanelClosed takes in PanelID, but since panel isn't initialized yet, we use panelCount
                                                  // because that is what PanelID gets set to
                    panelCount++;
                    new Cognitive3D.CustomEvent("c3d.ExitPoll detected no microphones")
                        .SetProperty("Panel ID", tempPanelID)
                        .Send();
                    OnPanelClosed(tempPanelID, "Answer" + tempPanelID, short.MinValue);
                    return;
                }
#endif

                var newPanelGo = GameObject.Instantiate<GameObject>(prefab,spawnPosition,spawnRotation);
                CurrentExitPollPanel = ExitPollPointer.currentExitPollPanel = newPanelGo.GetComponent<ExitPollPanel>();

                if (CurrentExitPollPanel == null)
                {
                    Debug.LogError(newPanelGo.gameObject.name + " does not have ExitPollPanel component!");
                    GameObject.Destroy(newPanelGo);
                    Cleanup(false);
                    return;
                }
                CurrentExitPollPanel.Initialize(panelProperties[currentPanelIndex], panelCount, this, questionSet.questions.Length);

                if (myparameters.ExitpollSpawnType == ExitPollManager.SpawnType.WorldSpace && myparameters.UseAttachTransform)
                {
                    if (myparameters.AttachTransform != null)
                    {
                        newPanelGo.transform.SetParent(myparameters.AttachTransform);
                    }
                }
            }
            else //finished everything format and send
            {
                SendResponsesAsCustomEvents(); //for personalization api
                //var responses = FormatResponses();

                string responseBody = CoreInterface.SerializeExitpollAnswers(responseProperties, QuestionSetId, myparameters.Hook);


                NetworkManager.PostExitpollAnswers(responseBody, QuestionSetName, questionSetVersion); //for exitpoll microservice
                CurrentExitPollPanel = null;
                Cleanup(true);
            }
            panelCount++;
        }

        void SendResponsesAsCustomEvents()
        {
            var exitpollEvent = new CustomEvent("cvr.exitpoll");
            exitpollEvent.SetProperty("userId", Cognitive3D_Manager.DeviceId);
            if (!string.IsNullOrEmpty(Cognitive3D_Manager.ParticipantId))
            {
                exitpollEvent.SetProperty("participantId", Cognitive3D_Manager.ParticipantId);
            }
            exitpollEvent.SetProperty("questionSetId", QuestionSetId);
            exitpollEvent.SetProperty("hook", myparameters.Hook);
            exitpollEvent.SetProperty("duration", Util.Timestamp() - StartTime);

            var scenesettings = Cognitive3D_Manager.TrackingScene;
            if (scenesettings != null && !string.IsNullOrEmpty(scenesettings.SceneId))
            {
                exitpollEvent.SetProperty("sceneId", scenesettings.SceneId);
            }

            foreach (var property in eventProperties)
            {
                exitpollEvent.SetProperty(property.Key, property.Value);
            }

            //use vector3.zero if CurrentExitPollPanel was never set
            Vector3 position = Vector3.zero;
            if (CurrentExitPollPanel != null)
                position = CurrentExitPollPanel.transform.position;

            exitpollEvent.Send(position);
            Cognitive3D_Manager.FlushData();
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
            if (ExitPollPointer.pointerInstance != null)
            {
                GameObject.Destroy(ExitPollPointer.pointerInstance);
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
