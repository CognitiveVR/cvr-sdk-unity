using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using CognitiveVR;

//visibility changes in a coroutine

namespace CognitiveVR
{
    public class ExitPollPanel : MonoBehaviour
    {
        struct ExitPollResponse
        {
            public string customerId;
            public string sessionId;
            public string pollState; //what are the poll states?
            public ExitPollQuestion[] pollValues;
            public string sceneId;
            public double timestamp; //this
            public System.Collections.Generic.KeyValuePair<string, object>[] properties;
        }

        struct ExitPollQuestion
        {
            public string question;
            public string answer;
        }

        System.Action _closeAction;

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
        public AnimationCurve XScale;
        public AnimationCurve YScale;
        public float PopupTime = 0.2f;

        [Tooltip("Mask for what things the exit poll will 'hit' and appear in front of if too close to a surface")]
        public LayerMask LayerMask;

        [Tooltip("Use to HMD Y position instead of spawning the poll directly ahead of the player")]
        public bool LockYPosition;

        [Tooltip("If this window is not in the player's line of sight, rotate around the player toward their facing")]
        public bool RotateToStayOnScreen = false;

        public float ResponseDelayTime = 2;
        public static float NextResponseTime { get; private set; }

        [Tooltip("Automatically close this window if there is no response after this many seconds")]
        public float TimeOut = 10;
        float _remainingTime;

        public bool DisplayReticule;
        GameObject _reticule;

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

        static ExitPollPanel _inst;
        static ExitPollPanel _instance
        {
            get
            {
                if (_inst == null)
                {
                    _inst = Instantiate(Resources.Load<GameObject>("ExitPollPanel")).GetComponent<ExitPollPanel>();
                }
                return _inst;
            }
        }

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

        [Tooltip("Update the position of the Exit Poll prefab if the player teleports")]
        public bool StickyWindow;
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

        /// <summary>
        /// Instantiate and position the exit poll at an arbitrary point
        /// </summary>
        /// <param name="closeAction">called when the player answers or the question is skipped/timed out</param>
        public static void Initialize(System.Action closeAction, Vector3 position)
        {
            if (CognitiveVR_Manager.HMD == null)
            {
                if (closeAction != null)
                    closeAction.Invoke();
                return;
            }

            bool enabled = Tuning.getVar<bool>("ExitPollEnabled", true);
            if (!enabled)
            {
                Util.logDebug("TuningVariable ExitPollEnabled==false");
                if (closeAction != null)
                    closeAction.Invoke();
                return;
            }

            _instance._closeAction = closeAction;
            _instance.gameObject.SetActive(true);
            NextResponseTime = _instance.ResponseDelayTime + Time.time;
            _instance._remainingTime = _instance.TimeOut;
            _instance.UpdateTimeoutBar();

            //set position and rotation
            _instance.transform.position = position;
            _instance._panel.rotation = Quaternion.LookRotation(position - CognitiveVR_Manager.HMD.position, Vector3.up);


            //set up actions
            _instance.PositiveButtonScript.enabled = true;
            _instance.NegativeButtonScript.enabled = true;
            if (_instance.CloseButtonScript)
                _instance.CloseButtonScript.enabled = true;

            /*_instance.PositiveButton.SetAction(() => _instance.Answer(true));
            _instance.NegativeButton.SetAction(() => _instance.Answer(false));
            if (_instance.CloseButton != null)
            {
                _instance.CloseButton.SetAction(() => _instance.Close());
            }*/

            //fetch a question
            _instance.StartCoroutine(_instance.FetchQuestion());
            _instance._lastRootPosition = _instance.root.position;
        }

        /// <summary>
        /// Instantiate and position the exit poll in front of the player.
        /// </summary>
        /// <param name="closeAction">called when the player answers or the question is skipped/timed out</param>
        public static void Initialize(System.Action closeAction)
        {
            if (CognitiveVR_Manager.HMD == null)
            {
                if (closeAction != null)
                    closeAction.Invoke();
                return;
            }

            //set position and rotation
            Vector3 tempPosition = CognitiveVR_Manager.HMD.position + CognitiveVR_Manager.HMD.forward * _instance.DisplayDistance;

            if (_instance.LockYPosition)
            {
                Vector3 modifiedForward = CognitiveVR_Manager.HMD.forward;
                modifiedForward.y = 0;
                modifiedForward.Normalize();

                tempPosition = CognitiveVR_Manager.HMD.position + modifiedForward * _instance.DisplayDistance;
            }

            RaycastHit hit = new RaycastHit();
            if (Physics.SphereCast(CognitiveVR_Manager.HMD.position, 0.5f, tempPosition - CognitiveVR_Manager.HMD.position, out hit, _instance.DisplayDistance, _instance.LayerMask))
            {
                if (hit.distance < _instance.MinimumDisplayDistance)
                {
                    //too close! just fail the popup and keep playing the game
                    if (closeAction != null)
                        closeAction.Invoke();
                    Debug.Log("too close to camera!");
                    return;
                }
                else
                {
                    tempPosition = CognitiveVR_Manager.HMD.position + (tempPosition - CognitiveVR_Manager.HMD.position).normalized * hit.distance;
                }
            }

            Initialize(closeAction, tempPosition);
        }

        //close action is called immediately if fetching the question fails
        IEnumerator FetchQuestion()
        {
            //TODO ask question server

            //tuning variable
            string response = Tuning.getVar<string>("ExitPollQuestion", "");
            if (!string.IsNullOrEmpty(response))
            {
                //parse out title and question
                string[] tuningQuestion = response.Split('|');
                if (tuningQuestion.Length == 2)
                {
                    Title.text = tuningQuestion[0];
                    Question.text = tuningQuestion[1];
                    SetVisible(true);
                    Debug.Log("fetch question from tuning variable! " + Question);
                }
                else
                {
                    //debug tuning variable incorrect format. should be title|question
                    if (_closeAction != null)
                        _closeAction.Invoke();
                    Util.logDebug("TuningVariable ExitPollQuestion is in the wrong format! should be 'title|question'");
                }

                yield break;
            }

            Debug.LogWarning("couldn't get tuning variable question!");

            yield return null;
        }

        public void SetVisible(bool visible)
        {
            Debug.Log("set visible");
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
                _instance.gameObject.SetActive(false);
                if (_reticule)
                {
                    Destroy(_reticule);
                }
            }
        }

        void Update()
        {
            //don't try to close again if the question has already started closing
            if (_closeAction == null) { return; }
            if (CognitiveVR_Manager.HMD == null)
            {
                Close();
                return;
            }
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
                Close();
                return;
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

                        _transform.RotateAround(CognitiveVR_Manager.HMD.position, rotateAxis, rotateSpeed * Time.deltaTime); //lerp this based on how far off forward is
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
                        _transform.rotation = Quaternion.Lerp(_transform.rotation, CognitiveVR_Manager.HMD.rotation, 0.1f);
                    }
                }
            }
        }

        void UpdateTimeoutBar()
        {
            if (TimeoutBar)
                TimeoutBar.fillAmount = _remainingTime / TimeOut;
        }

        public void Answer(bool positive)
        {
            //transaction
            Instrumentation.Transaction("ExitPoll").setProperty("Question", Question.text).setProperty("Answer", positive).beginAndEnd();


            //question
            ExitPollQuestion q = new ExitPollQuestion();
            q.answer = positive.ToString();
            q.question = Question.text;

            //response details
            ExitPollResponse r = new ExitPollResponse();
            r.customerId = CognitiveVR_Preferences.Instance.CustomerID;
            r.pollState = "OPEN";
            r.pollValues = new ExitPollQuestion[1] { q };
            r.properties = null;
            var key = CognitiveVR_Preferences.Instance.FindScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            if (key != null)
            {
                r.sceneId = key.SceneKey;
                //r.sessionId = Core.SessionId; //NEED TO GET THIS FROM INIT
                r.timestamp = (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;

                //TODO send this to a server somwhere
                string s = JsonUtility.ToJson(r, true);
                Debug.Log(s);
            }

            Close();
        }

        public void Close()
        {
            //disable button actions
            PositiveButtonScript.enabled = false;
            NegativeButtonScript.enabled = false;
            if (CloseButtonScript)
                CloseButtonScript.enabled = false;

            //call close action
            if (_closeAction != null)
                _closeAction.Invoke();

            _closeAction = null;

            //make invisible
            SetVisible(false);
        }
    }
}