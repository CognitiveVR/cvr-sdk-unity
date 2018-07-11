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

        [Header("Scale Settings")]
        [Tooltip("Apply a gradient to the buttons")]
        public Gradient IntegerGradient;
        public Image[] ColorableImages;
        public Text MinLabel;
        public Text MaxLabel;

        public bool UseDynamicSpacing = true;
        public float MinimumSpacing = 0.01f;
        public float MaximumSpacing = 0.4f;

        //when the user finishes answering the question or finishes closing the window
        //bool _completed = false;

        //delays input so player can understand the popup interface before answering
        float ResponseDelayTime = 0.1f;
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
        public bool IsClosing { get { return _isclosing; } }
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

            if (questionset.UseTimeout)
            {
                _remainingTime = questionset.Timeout;
                UpdateTimeoutBar();
            }

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

            if (properties["type"] == "MULTIPLE")
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
                var c = GetComponent<BoxCollider>();
                if (c != null)
                    c.size = new Vector3(2, 0.75f + split.Length * 0.3f, 0.1f);
            }
            else if (properties["type"] == "SCALE")
            {
                int resultbegin = 0;
                int.TryParse(properties["start"], out resultbegin);

                int resultend = 0;
                int.TryParse(properties["end"], out resultend);
                if (resultend == 0)
                {
                    CognitiveVR.Util.logDebug("ExitPoll Panel number of integer buttons to display == 0. skip this question");
                    QuestionSet.OnPanelClosed(PanelId, "Answer" + PanelId, short.MinValue);
                    Destroy(gameObject);
                    return;
                }

                //labels
                if (MinLabel != null)
                {
                    if (properties.ContainsKey("minLabel"))
                    {
                        MinLabel.enabled = true;
                        MinLabel.text = properties["minLabel"];
                    }
                    else
                    {
                        MinLabel.enabled = false;
                    }
                }

                if (MaxLabel != null)
                {
                    if (properties.ContainsKey("maxLabel"))
                    {
                        MaxLabel.enabled = true;
                        MaxLabel.text = properties["maxLabel"];
                    }
                    else
                    {
                        MaxLabel.enabled = false;
                    }
                }

                SetIntegerCount(resultbegin, resultend);
            }
            else if (properties["type"] == "VOICE")
            {
                int result = 0;
                int.TryParse(properties["maxResponseLength"], out result);

                GetComponentInChildren<MicrophoneButton>().RecordTime = result;
                
            }

            _isclosing = false;
            _allowTimeout = true;

            StartCoroutine(_SetVisible(true));
        }

        void SetMutltipleChoiceButton(string text, int id, GameObject button)
        {
            var gb = button.GetComponentInChildren<GazeButton>();
            UnityEngine.Events.UnityAction buttonclicked = () => { this.AnswerInt(id); };
            gb.OnLook.AddListener(buttonclicked);
            button.GetComponentInChildren<Text>().text = text;
        }

        void SetIntegerCount(int minValue, int maxValue)
        {
            if (ContentRoot == null)
            {
                return;
            }

            int totalCount = Mathf.Min(ContentRoot.childCount, maxValue);

            for (int i = 0; i < ContentRoot.childCount; i++)
            {
                if (minValue > i) //turn off
                {
                    ContentRoot.GetChild(i).gameObject.SetActive(false);
                }
                else
                {
                    break;
                }
            }

            for (int i = minValue; i< ContentRoot.childCount;i++)
            {
                if (i > maxValue)
                {
                    ContentRoot.GetChild(i).gameObject.SetActive(false);
                }
                else
                {
                    ContentRoot.GetChild(i).gameObject.SetActive(true);
                    SetIntegerButtonColor(ColorableImages[i], (float)i / totalCount);
                }
            }

            if (UseDynamicSpacing)
            {
                var group = ContentRoot.GetComponent<HorizontalLayoutGroup>();
                if (group != null)
                {
                    group.spacing = Mathf.Lerp(MaximumSpacing, MinimumSpacing, (maxValue - minValue+1) / 11f);
                }
            }
        }

        public void SetIntegerButtonColor(Image image, float gradientValue)
        {
            image.color = IntegerGradient.Evaluate(gradientValue);
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
                    _panel.localScale = new Vector3(XScale.Evaluate(normalizedTime), YScale.Evaluate(normalizedTime), XScale.Evaluate(normalizedTime));
                    yield return null;
                }
                _panel.localScale = Vector3.one;
                var gazeButtons = GetComponentsInChildren<GazeButton>(true);
                for (int i = 0; i< gazeButtons.Length;i++)
                {
                    gazeButtons[i].enabled = true;
                }
                var microphoneButton = GetComponentInChildren<MicrophoneButton>(true);
                if (microphoneButton != null)
                {
                    microphoneButton.enabled = true;
                }
            }
            else
            {
                var gazeButtons = GetComponentsInChildren<GazeButton>();
                for (int i = 0; i < gazeButtons.Length; i++)
                {
                    gazeButtons[i].enabled = false;
                }
                var microphoneButton = GetComponentInChildren<MicrophoneButton>();
                if (microphoneButton != null)
                {
                    microphoneButton.enabled = false;
                }

                normalizedTime = 1;
                while (normalizedTime > 0)
                {
                    normalizedTime -= Time.deltaTime / PopupTime;
                    _panel.localScale = new Vector3(XScale.Evaluate(normalizedTime), YScale.Evaluate(normalizedTime), XScale.Evaluate(normalizedTime));
                    _panel.localPosition += transform.forward * Time.deltaTime* 0.1f;
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
                ExitPoll.CurrentExitPollSet.OnPanelError();
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
                    Timeout();
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
                        _panel.rotation = Quaternion.Lerp(_panel.rotation, Quaternion.LookRotation(toCube, Vector3.up), 0.1f);
                    }
                }

                //clamp distance
                float dist = Vector3.Distance(_transform.position, CognitiveVR_Manager.HMD.position);
                if (dist > QuestionSet.DisplayDistance)
                {
                    Vector3 vector = (_transform.position - CognitiveVR_Manager.HMD.position).normalized * QuestionSet.DisplayDistance;
                    _transform.position = vector + CognitiveVR_Manager.HMD.position;
                }
                else if (dist < QuestionSet.MinimumDisplayDistance)
                {
                    Vector3 vector = (_transform.position - CognitiveVR_Manager.HMD.position).normalized * QuestionSet.MinimumDisplayDistance;
                    _transform.position = vector + CognitiveVR_Manager.HMD.position;
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
            if (_isclosing) { return; }
            int responseValue = 0;
            if (positive)
                responseValue = 1;
            QuestionSet.OnPanelClosed(PanelId, "Answer" + PanelId, responseValue);
            Close();
        }

        //from buttons
        public void AnswerInt(int value)
        {
            if (_isclosing) { return; }
            QuestionSet.OnPanelClosed(PanelId, "Answer" + PanelId, value);
            Close();
        }

        //from buttons
        [System.Obsolete]
        public void AnswerString(string answer)
        {
            CognitiveVR.Util.logError("Exit Poll Panel AnswerString should not be used!");
            if (_isclosing) { return; }

            QuestionSet.OnPanelClosed(PanelId, "Answer" + PanelId, 0);
            Close();
        }

        //called directly from MicrophoneButton when recording is complete
        public void AnswerMicrophone(string base64wav)
        {
            if (_isclosing) { return; }
            QuestionSet.OnPanelClosedVoice(PanelId, "Answer" + PanelId, base64wav);
            Close();
        }

        /// <summary>
        /// from buttons on panel
        /// </summary>
        public void CloseButton()
        {
            if (_isclosing) { return; }
            QuestionSet.OnPanelClosed(PanelId, "Answer" + PanelId, short.MinValue);
            Close();
        }

        public void Timeout()
        {
            if (_isclosing) { return; }
            QuestionSet.OnPanelClosed(PanelId, "Answer" + PanelId, short.MinValue);
            Close();
        }

        //called from exitpoll when this panel needs to be cleaned up. does not set response in question set
        public void CloseError()
        {
            if (_isclosing) { return; }
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