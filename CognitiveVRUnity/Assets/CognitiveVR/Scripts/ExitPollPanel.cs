using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using CognitiveVR;
using CognitiveVR.Json;

//visibility changes in a coroutine
namespace CognitiveVR
{
    namespace Json
    {
        //these are filled/emptied from json, so may not be directly referenced
#pragma warning disable 0649
        class ExitPollRequest
        {
            public string customerId;
            public string sessionId; //sessionID for scene explorer
            public ExitPollTuningQuestion[] pollValues;
            public int timestamp;
            public ExitPollProperties[] properties = new ExitPollProperties[0];
        }

        [System.Serializable]
        class ExitPollProperties
        {
            public string name;
            public string value;
        }

        [System.Serializable]
        class ExitPollTuningQuestion
        {
            public string question;
            public string answer;
        }

        class ExitPollResponse
        {
            public string pollId;
        }
#pragma warning restore 0649
    }

    public enum ExitPollPanelType
    {
        ExitPollMicrophonePanel,
        ExitPollQuestionPanel,
    }

    public class ExitPollPanel : MonoBehaviour
    {
        System.Action _finalCloseAction;
        System.Action _closeAction;

        public string ExitPollQuestion = "ExitPollQuestion";

        [Header("Components")]
        public Text Title;
        public Text Question;

        public MonoBehaviour PositiveButtonScript;
        public MonoBehaviour NegativeButtonScript;
        public MonoBehaviour CloseButtonScript;
        public Image TimeoutBar;

        [Header("Display")]
        [Tooltip("Try to position the ExitPoll Panel this many meters in front of the player")]
        public float DisplayDistance = 3;
        [Tooltip("If the position is invalid (ex, in a wall), the panel moves closer to the camera. If the distance is below this value, skip the question")]
        public float MinimumDisplayDistance = 1;
        [Tooltip("Mask for what things the exit poll will 'hit' and appear in front of if too close to a surface")]
        public LayerMask LayerMask;
        public float PopupTime = 0.2f;
        public AnimationCurve XScale;
        public AnimationCurve YScale;

        //when the user finishes answering the question or finishes closing the window
        bool _completed = false;

        public float ResponseDelayTime = 2;
        public static float NextResponseTime { get; private set; }

        [Tooltip("Automatically close this window if there is no response after this many seconds")]
        public float TimeOut = 10;
        float _remainingTime;
        bool _allowTimeout = true;

        [Header("Display Options")]
        [Tooltip("Use to HMD Y position instead of spawning the poll directly ahead of the player")]
        public bool LockYPosition;

        [Tooltip("Create a simple reticule while the ExitPoll Panel is visible")]
        public bool DisplayReticule;
        GameObject _reticule;

        [Tooltip("If this window is not in the player's line of sight, rotate around the player toward their facing")]
        public bool RotateToStayOnScreen = false;

        [Tooltip("Update the position of the Exit Poll prefab if the player teleports")]
        public bool StickyWindow;

        Transform _t;
        Transform _transform
        {
            get
            {
                if (_t == null)
                {
                    _t = transform;
                }
                return _t;
            }
        }

        private static ExitPollPanel _instance;

        Transform _p;
        Transform _panel
        {
            get
            {
                if (_p == null)
                {
                    _p = _transform.GetChild(0);
                }
                return _p;
            }
        }

        Transform _root;
        Transform root
        {
            get
            {
                if (_root == null)
                    if (CognitiveVR_Manager.HMD == null) _root = transform;
                    else { _root = CognitiveVR_Manager.HMD.root; }
                return _root;
            }
        }
        Vector3 _lastRootPosition;

        public static string PollID { get; private set; }

        /// <summary>
        /// Instantiate and position the exit poll at an arbitrary point
        /// </summary>
        /// <param name="closeAction">called when the player answers or the question is skipped/timed out</param>
        /// <param name="position">where to instantiate the exitpoll window</param>
        /// <param name="exitpollType">what kind of window to instantiate. microphone will automatically appear last</param>
        public static void Initialize(System.Action closeAction, Vector3 position, ExitPollPanelType exitpollType = ExitPollPanelType.ExitPollQuestionPanel)
        {
            if (CognitiveVR_Manager.HMD == null)
            {
                if (closeAction != null)
                    closeAction.Invoke();
                _instance.Close(true);
                return;
            }

            bool enabled = Tuning.getVar<bool>("ExitPollEnabled", true);
            if (!enabled)
            {
                Util.logDebug("TuningVariable ExitPollEnabled==false");
                if (closeAction != null)
                    closeAction.Invoke();
                _instance.Close(true);
                return;
            }

            _instance = Instantiate(Resources.Load<GameObject>(exitpollType.ToString())).GetComponent<ExitPollPanel>();

            _instance._transform.position = position;

            PostInitialize(closeAction, exitpollType);
        }

        /// <summary>
        /// Instantiate and position the exit poll in front of the player.
        /// </summary>
        /// <param name="closeAction">called when the player answers or the question is skipped/timed out</param>
        /// <param name="exitpollType">what kind of window to instantiate. microphone will automatically appear last</param>
        public static void Initialize(System.Action closeAction, ExitPollPanelType exitpollType = ExitPollPanelType.ExitPollQuestionPanel)
        {
            if (CognitiveVR_Manager.HMD == null) //no hmd? fail
            {
                if (closeAction != null)
                    closeAction.Invoke();
                _instance.Close(true);
                return;
            }

            bool enabled = Tuning.getVar<bool>("ExitPollEnabled", true);
            if (!enabled) //exit poll set to false? fail
            {
                Util.logDebug("TuningVariable ExitPollEnabled==false");
                if (closeAction != null)
                    closeAction.Invoke();
                _instance.Close(true);
                return;
            }

            _instance = Instantiate(Resources.Load<GameObject>(exitpollType.ToString())).GetComponent<ExitPollPanel>();

            //set position and rotation
            Vector3 spawnPosition = CognitiveVR_Manager.HMD.position + CognitiveVR_Manager.HMD.forward * _instance.DisplayDistance;

            if (_instance.LockYPosition)
            {
                Vector3 modifiedForward = CognitiveVR_Manager.HMD.forward;
                modifiedForward.y = 0;
                modifiedForward.Normalize();

                spawnPosition = CognitiveVR_Manager.HMD.position + modifiedForward * _instance.DisplayDistance;
            }

            RaycastHit hit = new RaycastHit();

            //test slightly in front of the player's hmd
            Collider[] colliderHits = Physics.OverlapSphere(CognitiveVR_Manager.HMD.position + Vector3.forward * 0.5f, 0.5f, _instance.LayerMask);
            if (colliderHits.Length > 0)
            {
                Util.logDebug("ExitPoll.Initialize hit collider " + colliderHits[0].gameObject.name + " too close to player. Skip exit poll");
                //too close! just fail the popup and keep playing the game
                if (closeAction != null)
                    closeAction.Invoke();
                _instance.Close(true);
                return;
            }

            //ray from player's hmd position
            if (Physics.SphereCast(CognitiveVR_Manager.HMD.position, 0.5f, spawnPosition - CognitiveVR_Manager.HMD.position, out hit, _instance.DisplayDistance, _instance.LayerMask))
            {
                if (hit.distance < _instance.MinimumDisplayDistance)
                {
                    Util.logDebug("ExitPoll.Initialize hit collider " + hit.collider.gameObject.name + " too close to player. Skip exit poll");
                    //too close! just fail the popup and keep playing the game
                    if (closeAction != null)
                        closeAction.Invoke();
                    _instance.Close(true);
                    return;
                }
                else
                {
                    spawnPosition = CognitiveVR_Manager.HMD.position + (spawnPosition - CognitiveVR_Manager.HMD.position).normalized * (hit.distance);
                }
            }

            _instance._transform.position = spawnPosition;

            PostInitialize(closeAction, exitpollType);
        }

        static void PostInitialize(System.Action closeAction, ExitPollPanelType exitpollType)
        {
            //initialize variables
            if (exitpollType == ExitPollPanelType.ExitPollQuestionPanel)
            {
                System.Action microphoneAction = () => ExitPollPanel.Initialize(closeAction, _instance._transform.position, ExitPollPanelType.ExitPollMicrophonePanel);
                _instance._finalCloseAction = closeAction;
                _instance._closeAction = microphoneAction;
            }
            else
            {
                _instance._finalCloseAction = closeAction;
                _instance._closeAction = closeAction;
            }
            
            //_instance._closeAction = closeAction;
            _instance.gameObject.SetActive(true);
            NextResponseTime = _instance.ResponseDelayTime + Time.time;
            _instance._remainingTime = _instance.TimeOut;
            _instance._allowTimeout = true;
            _instance.UpdateTimeoutBar();

            //set position and rotation
            //_instance.transform.position = _instance._transform.position;
            _instance._panel.rotation = Quaternion.LookRotation(_instance._transform.position - CognitiveVR_Manager.HMD.position, Vector3.up);


            //set up actions
            _instance.PositiveButtonScript.enabled = true;
            _instance.NegativeButtonScript.enabled = true;
            if (_instance.CloseButtonScript)
                _instance.CloseButtonScript.enabled = true;

            //fetch a question
            _instance.StartCoroutine(_instance.FetchQuestion());
            _instance._lastRootPosition = _instance.root.position;
        }

        //close action is called immediately if fetching the question fails
        IEnumerator FetchQuestion()
        {
            //TODO ask question server

            //tuning variable
            string response = Tuning.getVar<string>(ExitPollQuestion, "");
            if (!string.IsNullOrEmpty(response))
            {
                //parse out title and question
                string[] tuningQuestion = response.Split('|');
                if (tuningQuestion.Length == 2)
                {
                    Title.text = tuningQuestion[0];
                    Question.text = tuningQuestion[1];
                    SetVisible(true);
                }
                else
                {
                    //debug tuning variable incorrect format. should be title|question
                    Close(true);
                    Util.logDebug("ExitPoll TuningVariable "+ ExitPollQuestion+" is in the wrong format! should be 'title|question'");
                }

                yield break;
            }
            else
            {
                //no question set up. use default from prefab
                SetVisible(true);
                yield break;
            }
        }

        public void SetVisible(bool visible)
        {
            //runs x/y scale through animation curve
            StartCoroutine(_SetVisible(visible));
        }

        IEnumerator _SetVisible(bool visible)
        {
            float normalizedTime = 0;
            if (visible)
            {
                if (DisplayReticule)
                {
                    _reticule = Instantiate(Resources.Load<GameObject>("ExitPollReticule"));
                    _reticule.transform.SetParent(CognitiveVR_Manager.HMD);
                    _reticule.transform.localPosition = Vector3.forward * (Vector3.Distance(_transform.position, CognitiveVR_Manager.HMD.position) - 0.5f);
                    _reticule.transform.localRotation = Quaternion.identity;
                }
                while (normalizedTime < 1)
                {
                    normalizedTime += Time.deltaTime / PopupTime;
                    _panel.localScale = new Vector3(XScale.Evaluate(normalizedTime), YScale.Evaluate(normalizedTime));
                    yield return null;
                }
                _panel.localScale = Vector3.one;
            }
            else
            {
                normalizedTime = 1;
                while (normalizedTime > 0)
                {
                    normalizedTime -= Time.deltaTime / PopupTime;
                    _panel.localScale = new Vector3(XScale.Evaluate(normalizedTime), YScale.Evaluate(normalizedTime));
                    yield return null;
                }
                _panel.localScale = Vector3.zero;
                gameObject.SetActive(false);
                if (_reticule)
                {
                    Destroy(_reticule);
                }
                Destroy(gameObject);
            }
        }

        public void DisableTimeout()
        {
            _allowTimeout = false;
            _remainingTime = TimeOut;
            UpdateTimeoutBar();
        }

        void Update()
        {
            //don't try to close again if the question has already started closing
            if (_closeAction == null) { return; }
            if (_completed) { return; }
            if (CognitiveVR_Manager.HMD == null)
            {
                Close(true);
                return;
            }
            if (_allowTimeout)
            {
                if (_remainingTime > 0)
                {
                    if (NextResponseTime < Time.time)
                    {
                        _remainingTime -= Time.deltaTime;
                        UpdateTimeoutBar();
                    }
                }
                else
                {
                    PollID = string.Empty;
                    Close();
                    return;
                }
            }
            if (StickyWindow)
            {
                if (Vector3.SqrMagnitude(_lastRootPosition - root.position) > 0.1f)
                {
                    Vector3 delta = _lastRootPosition - root.position;
                    _transform.position -= delta;

                    _lastRootPosition = root.position;
                }
            }
            if (RotateToStayOnScreen)
            {
                float maxDot = 0.9f;
                float maxRotSpeed = 360;

                if (LockYPosition)
                {
                    Vector3 camforward = CognitiveVR_Manager.HMD.forward;
                    camforward.y = 0;
                    camforward.Normalize();

                    Vector3 toCube = _transform.position - CognitiveVR_Manager.HMD.position;
                    toCube.y = 0;
                    toCube.Normalize();

                    float dot = Vector3.Dot(camforward, toCube);
                    Debug.Log(dot);
                    if (dot < maxDot)
                    {
                        Vector3 rotateAxis = Vector3.down;

                        Vector3 camRightYlock = CognitiveVR_Manager.HMD.right;
                        camRightYlock.y = 0;
                        camRightYlock.Normalize();

                        float rotateSpeed = Mathf.Lerp(maxRotSpeed, 0, dot);
                        float directionDot = Vector3.Dot(camRightYlock, toCube);
                        if (directionDot < 0)
                            rotateSpeed *= -1;

                        _panel.RotateAround(CognitiveVR_Manager.HMD.position, rotateAxis, rotateSpeed * Time.deltaTime); //lerp this based on how far off forward is
                    }
                }
                else
                {
                    Vector3 toCube = (_transform.position - CognitiveVR_Manager.HMD.position).normalized;
                    float dot = Vector3.Dot(CognitiveVR_Manager.HMD.forward, toCube);
                    if (dot < maxDot)
                    {
                        Vector3 rotateAxis = Vector3.Cross(toCube, CognitiveVR_Manager.HMD.forward);
                        float rotateSpeed = Mathf.Lerp(maxRotSpeed, 0, dot);

                        _transform.RotateAround(CognitiveVR_Manager.HMD.position, rotateAxis, rotateSpeed * Time.deltaTime); //lerp this based on how far off forward is
                        _panel.rotation = Quaternion.Lerp(_panel.rotation, Quaternion.LookRotation(toCube, CognitiveVR_Manager.HMD.up), 0.1f);
                    }
                    //TODO rotate so window stays relatively vertical
                }
            }
        }

        void UpdateTimeoutBar()
        {
            if (TimeoutBar)
                TimeoutBar.fillAmount = _remainingTime / TimeOut;
        }

        //from buttons
        public void Answer(bool positive)
        {
            if (_completed) { return; }
            _completed = true;
            //question
            ExitPollTuningQuestion question = new ExitPollTuningQuestion();
            question.answer = positive.ToString();
            question.question = Question.text;

            //response details
            ExitPollRequest response = new ExitPollRequest();
            response.customerId = CognitiveVR_Preferences.Instance.CustomerID;
            response.pollValues = new ExitPollTuningQuestion[1] { question };
            response.timestamp = (int)CognitiveVR_Manager.TimeStamp;
            response.sessionId = CognitiveVR_Manager.SessionID;

            string url = "https://api.cognitivevr.io/polls";
            string jsonResponse = JsonUtility.ToJson(response, true);
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(jsonResponse);

            Util.logDebug("ExitPoll Request\n" + jsonResponse);

            StartCoroutine(SendAnswer(bytes, url));
        }

        private IEnumerator SendAnswer(byte[] bytes, string url)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            WWW www = new UnityEngine.WWW(url, bytes, headers);
            yield return www; //10 second timeout by default on unity's www class

            if (!string.IsNullOrEmpty(www.error))
            {
                Util.logError("error response: " + www.error);
                PollID = string.Empty;
            }
            else
            {
                ExitPollResponse response = JsonUtility.FromJson<ExitPollResponse>(www.text);
                Instrumentation.Transaction("ExitPoll").setProperty("pollId", response.pollId).beginAndEnd();
                PollID = response.pollId;
            }

            Close();
        }

        /// <summary>
        /// from buttons on panel
        /// </summary>
        public void CloseButton()
        {
            PollID = string.Empty;
            Close();
        }

        public void Close(bool immediate = false)
        {
            //disable button actions
            PositiveButtonScript.enabled = false;
            NegativeButtonScript.enabled = false;
            if (CloseButtonScript)
                CloseButtonScript.enabled = false;

            if (string.IsNullOrEmpty(PollID))
            {
                if (_finalCloseAction != null)
                {
                    _finalCloseAction.Invoke();
                }
            }
            else
            {
                //call close action
                if (_closeAction != null)
                {
                    _closeAction.Invoke();
                }
            }

            _closeAction = null;

            if (immediate)
            {
                gameObject.SetActive(false);
                if (_reticule)
                {
                    Destroy(_reticule);
                }
                Destroy(gameObject);
            }
            else
            {
                SetVisible(false);
            }
        }
    }
}