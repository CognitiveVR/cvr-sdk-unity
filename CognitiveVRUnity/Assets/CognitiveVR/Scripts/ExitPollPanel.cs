using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using CognitiveVR;
using CognitiveVR.Json;

//component for displaying the gui panel and returning the response to the exitpoll question set
namespace CognitiveVR
{
    public class ExitPollPanel : MonoBehaviour
    {
        [Header("Components")]
        public Text Title;
        public Text Question;
        public Image TimeoutBar;

        [Header("Display")]
        public AnimationCurve XScale;
        public AnimationCurve YScale;
        float PopupTime = 0.2f;

        [Header("Multiple Choice Settings")]
        public GameObject AnswerButton;
        public Transform ContentRoot;

        [Header("Integer Settings")]
        [Tooltip("Apply a gradient to the buttons")]
        public Gradient IntegerGradient;

        //when the user finishes answering the question or finishes closing the window
        //bool _completed = false;
        
        //delays input so player can understand the popup interface before answering
        float ResponseDelayTime = 2;
        float NextResponseTime;
        public bool NextResponseTimeValid
        {
            get
            {
                return NextResponseTime < Time.time;
            }
        }

        ExitPollSet QuestionSet;

        GameObject _reticule;
        float _remainingTime; //before timeout
        bool _isclosing; //has timed out/answered/skipped but still animating?
        bool _allowTimeout; //used by microphone to disable timeout

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

        //used when scaling and rotating
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

        //used for sticky window - reposition window if player teleports
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
        int PanelId;

        /// <summary>
        /// Instantiate and position the exit poll at an arbitrary point
        /// </summary>
        /// <param name="closeAction">called when the player answers or the question is skipped/timed out</param>
        /// <param name="position">where to instantiate the exitpoll window</param>
        /// <param name="exitpollType">what kind of window to instantiate. microphone will automatically appear last</param>
        public void Initialize(Dictionary<string,string> properties,int panelId, ExitPollSet questionset)
        {
            QuestionSet = questionset;
            PanelId = panelId;
            NextResponseTime = ResponseDelayTime + Time.time;
            UpdateTimeoutBar();

            _transform.rotation = Quaternion.LookRotation(_transform.position - CognitiveVR_Manager.HMD.position, Vector3.up);

            //display question from properties

            if (Title != null)
            {
                string title = "Title";
                properties.TryGetValue("title", out title);
                Title.text = title;
            }

            if (Question != null)
            {
                string question = "Question";
                properties.TryGetValue("question", out question);
                Question.text = question;
            }

            if (properties["type"] == "multiple")
            {
                string[] split = properties["csvanswers"].Split('|');
                List<GameObject> AnswerButtons = new List<GameObject>();
                AnswerButtons.Add(AnswerButton);
                for (int i = 1; i<split.Length; i++)
                {
                    AnswerButtons.Add((GameObject)Instantiate(AnswerButton, ContentRoot));
                }
                for (int i = 0; i<split.Length; i++)
                {
                    SetMutltipleChoiceButton(split[i],i,AnswerButtons[i]);
                }
            }
            else if (properties["type"] == "integer")
            {
                Debug.Log("TODO set number of buttons based on integer question maximum value (ex 1-5, 1-10)");
            }

            StartCoroutine(_SetVisible(true));
        }

        void SetMutltipleChoiceButton(string text, int id, GameObject button)
        {
            var gb = button.GetComponentInChildren<GazeButton>();
            UnityEngine.Events.UnityAction buttonclicked = () => { this.AnswerInt(id); };
            gb.OnLook.AddListener(buttonclicked);
            button.GetComponentInChildren<Text>().text = text;
        }

        IEnumerator _SetVisible(bool visible)
        {
            float normalizedTime = 0;
            if (visible)
            {
                if (QuestionSet.DisplayReticle)
                {
                    _reticule = Instantiate(ExitPoll.ExitPollReticle);
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

        //called from microphone button when activated
        public void DisableTimeout()
        {
            _allowTimeout = false;
            _remainingTime = QuestionSet.Timeout;
            UpdateTimeoutBar();
        }

        void Update()
        {
            //don't activate anything if the question has already started closing
            if (_isclosing) { return; }
            if (CognitiveVR_Manager.HMD == null)
            {
                Close();
                return;
            }
            if (QuestionSet.UseTimeout && _allowTimeout)
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
                    Close();
                    return;
                }
            }
            if (QuestionSet.StickWindow)
            {
                if (Vector3.SqrMagnitude(_lastRootPosition - root.position) > 0.1f)
                {
                    Vector3 delta = _lastRootPosition - root.position;
                    _transform.position -= delta;

                    _lastRootPosition = root.position;
                }
            }
            if (QuestionSet.RotateToStayOnScreen)
            {
                float maxDot = 0.9f;
                float maxRotSpeed = 360;

                if (QuestionSet.LockYPosition)
                {
                    Vector3 camforward = CognitiveVR_Manager.HMD.forward;
                    camforward.y = 0;
                    camforward.Normalize();

                    Vector3 toCube = _transform.position - CognitiveVR_Manager.HMD.position;
                    toCube.y = 0;
                    toCube.Normalize();

                    float dot = Vector3.Dot(camforward, toCube);
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
                TimeoutBar.fillAmount = _remainingTime / QuestionSet.Timeout;
        }

        //from buttons
        public void AnswerBool(bool positive)
        {
            //write the answer over QuestionSet

            //Dictionary<string, object> response = new Dictionary<string, object>();
            //response.Add("question", Question.text);
            //response.Add("answer", positive);
            //response.Add("close", "answer");

            //Instrumentation.Transaction("cvr.exitpoll").setProperty("question", Question.text).setProperty("answer", positive).beginAndEnd(transform.position); //this goes to scene explorer

            QuestionSet.OnPanelClosed("Answer" + PanelId, positive);
            Close();
        }

        //from buttons
        public void AnswerInt(int value)
        {
            //write the answer over QuestionSet

            //Dictionary<string, object> response = new Dictionary<string, object>();
            //response.Add("question", Question.text);
            //response.Add("answer", value);
            //response.Add("close", "answer");

            //question set id
            //hookid
            //Instrumentation.Transaction("cvr.exitpoll").setProperty("question", Question.text).setProperty("answer", value).beginAndEnd(transform.position); //this goes to scene explorer

            QuestionSet.OnPanelClosed("Answer" + PanelId, value);
            Close();
        }

        //from buttons
        public void AnswerString(string answer)
        {
            //write the answer over QuestionSet

            //Dictionary<string, object> response = new Dictionary<string, object>();
            //response.Add("question", Question.text);
            //response.Add("answer", answer);
            //response.Add("close", "answer");

            //Instrumentation.Transaction("cvr.exitpoll").setProperty("question", Question.text).setProperty("answer", answer).beginAndEnd(transform.position); //this goes to scene explorer

            QuestionSet.OnPanelClosed("Answer"+ PanelId, answer);
            Close();
        }

        //called directly from MicrophoneButton when recording is complete
        public void AnswerMicrophone(string base64wav)
        {
            //write the answer over QuestionSet

            //Dictionary<string, object> response = new Dictionary<string, object>();
            //response.Add("question", Question.text);
            //response.Add("answer", base64wav);
            //response.Add("close", "answer");

            //Instrumentation.Transaction("cvr.exitpoll").setProperty("question", Question.text).beginAndEnd(transform.position); //this goes to scene explorer

            QuestionSet.OnPanelClosedVoice("Answer" + PanelId, base64wav);
            QuestionSet.OnPanelClosed("Answer" + PanelId, "voice");
            Close();
        }

        /// <summary>
        /// from buttons on panel
        /// </summary>
        public void CloseButton()
        {
            QuestionSet.OnPanelClosed("Answer" + PanelId, "skip");
            //QuestionSet.OnPanelClosed(new Dictionary<string, object>() { { "close", "skip" } });
            //Instrumentation.Transaction("cvr.exitpoll").setProperty("question", Question.text).setProperty("close", "skip").beginAndEnd(transform.position); //this goes to scene explorer

            Close();
        }

        public void Timeout()
        {
            QuestionSet.OnPanelClosed("Answer" + PanelId, "skip");
            //QuestionSet.OnPanelClosed(new Dictionary<string, object>() { { "close", "timeout" } });
            //Instrumentation.Transaction("cvr.exitpoll").setProperty("question", Question.text).setProperty("close", "timeout").beginAndEnd(transform.position); //this goes to scene explorer
            Close();
        }

        //close the window visually. informing the question set has already been completed
        void Close()
        {
            _isclosing = true;
            StartCoroutine(_SetVisible(false));
        }
    }
}