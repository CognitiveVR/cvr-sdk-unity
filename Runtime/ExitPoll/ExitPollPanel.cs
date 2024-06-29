using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Cognitive3D;
using Cognitive3D.Json;

//component for displaying the gui panel and returning the response to the exitpoll question set
namespace Cognitive3D
{
    [AddComponentMenu("Cognitive3D/Internal/Exit Poll Panel")]
    public class ExitPollPanel : MonoBehaviour
    {
        [Header("Components")]
        public Text Title;
        public Text Question;
        public Text QuestionNumber;
        public Text errorMessage;

        //used when scaling and rotating
        public Transform PanelRoot;
        public VirtualButton confirmButton;

        [Header("Display")]
        public AnimationCurve XScale;
        public AnimationCurve YScale;
        float PopupTime = 0.2f;

        [Header("Boolean Settings")]
        public VirtualButton positiveButton;
        public VirtualButton negativeButton;

        [Header("Multiple Choice Settings")]
        public GameObject[] AnswerButtons;

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

        //delays input so player can understand the popup interface before answering
        float ResponseDelayTime = 0.1f;
        float NextResponseTime;

        ExitPollSet QuestionSet;

        bool _isclosing; //has timed out/answered/skipped but still animating?

        //used for sticky window - reposition window if player teleports
        Transform _root;
        Transform root
        {
            get
            {
                if (_root == null)
                    if (GameplayReferences.HMD == null) _root = transform;
                    else { _root = GameplayReferences.HMD.root; }
                return _root;
            }
        }
        Vector3 _lastRootPosition;

        //indicates which question this panel should record the answer to
        int PanelId;

        #region Initialization

        /// <summary>
        /// Instantiate and position the exit poll at an arbitrary point
        /// </summary>
        /// <param name="closeAction">called when the player answers or the question is skipped/timed out</param>
        /// <param name="position">where to instantiate the exitpoll window</param>
        /// <param name="exitpollType">what kind of window to instantiate. microphone will automatically appear last</param>
        public void Initialize(Dictionary<string,string> properties,int panelId, ExitPollSet questionset, int numQuestionsInSet)
        {
            QuestionSet = questionset;
            PanelId = panelId;
            NextResponseTime = ResponseDelayTime + Time.time;

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

            if (QuestionNumber != null)
            {
                QuestionNumber.text = $"Question {panelId + 1} of {numQuestionsInSet}";
            }

            if (properties["type"] == "MULTIPLE")
            {
                string[] split = properties["csvanswers"].Split('|');
                for (int i = 0; i < split.Length; i++)
                {
                    AnswerButtons[i].GetComponentInChildren<Text>().text = split[i];
                }
                for (int i = split.Length; i<AnswerButtons.Length; i++)
                {
                    if (AnswerButtons[i] == null) { continue; }
                    AnswerButtons[i].SetActive(false);
                }
            }
            else if (properties["type"] == "SCALE")
            {
                int resultbegin = 0;
                int.TryParse(properties["start"], out resultbegin);

                int resultend = 0;
                int.TryParse(properties["end"], out resultend);
                if (resultend == 0)
                {
                    Cognitive3D.Util.logDebug("ExitPoll Panel number of integer buttons to display == 0. skip this question");
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

                var mic = GetComponentInChildren<MicrophoneButton>();
                mic.RecordTime = result;
                mic.SetExitPollQuestionSet(questionset);
            }

            _isclosing = false;

            StartCoroutine(_SetVisible(true));
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
                if (minValue > i) //turn off lower buttons
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
                if (i > maxValue) //turn off higher buttons
                {
                    ContentRoot.GetChild(i).gameObject.SetActive(false);
                }
                else //ensure valid buttons are turned on
                {
                    ContentRoot.GetChild(i).gameObject.SetActive(true);
                }
            }

            if (UseDynamicSpacing)
            {
                var group = ContentRoot.GetComponent<HorizontalLayoutGroup>();
                if (group != null)
                {
                    group.spacing = Mathf.Lerp(MaximumSpacing, MinimumSpacing, (maxValue - minValue + 1) / (maxValue + 1f));
                }
            }
        }

#endregion

        IEnumerator _SetVisible(bool visible)
        {
            float normalizedTime = 0;
            if (visible)
            {
                while (normalizedTime < 1)
                {
                    normalizedTime += Time.deltaTime / PopupTime;
                    PanelRoot.localScale = new Vector3(XScale.Evaluate(normalizedTime), YScale.Evaluate(normalizedTime), XScale.Evaluate(normalizedTime));
                    yield return null;
                }
                PanelRoot.localScale = Vector3.one;
                var gazeButtons = GetComponentsInChildren<IPointerFocus>(true);
                for (int i = 0; i< gazeButtons.Length;i++)
                {
                    gazeButtons[i].MonoBehaviour.enabled = true;
                }
                var microphoneButton = GetComponentInChildren<MicrophoneButton>(true);
                if (microphoneButton != null)
                {
                    microphoneButton.enabled = true;
                }
            }
            else
            {
                var gazeButtons = GetComponentsInChildren<IPointerFocus>();
                for (int i = 0; i < gazeButtons.Length; i++)
                {
                    gazeButtons[i].MonoBehaviour.enabled = false;
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
                    PanelRoot.localScale = new Vector3(XScale.Evaluate(normalizedTime), YScale.Evaluate(normalizedTime), XScale.Evaluate(normalizedTime));
                    PanelRoot.localPosition += transform.forward * Time.deltaTime* 0.1f;
                    yield return null;
                }
                PanelRoot.localScale = Vector3.zero;
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

        #region Updates

        IEnumerator CloseAfterWaitForSpecifiedTime(int seconds, int value)
        {
            PanelRoot.gameObject.SetActive(false);
            yield return new WaitForSeconds(seconds);
            QuestionSet.OnPanelClosed(PanelId, "Answer" + PanelId, value);
            Close();
        }        
        
        void Update()
        {
            //don't activate anything if the question has already started closing
            if (_isclosing) { return; }
            if (GameplayReferences.HMD == null)
            {
                Close();
                return;
            }
            if (QuestionSet.myparameters.StickWindow)
            {
                if (Vector3.SqrMagnitude(_lastRootPosition - root.position) > 0.1f)
                {
                    Vector3 delta = _lastRootPosition - root.position;
                    transform.position -= delta;

                    _lastRootPosition = root.position;
                }
            }

            if (QuestionSet.myparameters.LockYPosition)
            {
                Vector3 pos = transform.position;
                pos.y = GameplayReferences.HMD.position.y;
                transform.position = pos;
            }

            if (QuestionSet.myparameters.RotateToStayOnScreen)
            {
                float maxDot = 0.9f;
                float maxRotSpeed = 360;

                if (QuestionSet.myparameters.LockYPosition)
                {
                    Vector3 camforward = GameplayReferences.HMD.forward;
                    camforward.y = 0;
                    camforward.Normalize();

                    Vector3 toCube = transform.position - GameplayReferences.HMD.position;
                    toCube.y = 0;
                    toCube.Normalize();

                    float dot = Vector3.Dot(camforward, toCube);

                    if (dot < maxDot)
                    {
                        Vector3 rotateAxis = Vector3.down;

                        Vector3 camRightYlock = GameplayReferences.HMD.right;
                        camRightYlock.y = 0;
                        camRightYlock.Normalize();

                        float rotateSpeed = Mathf.Lerp(maxRotSpeed, 0, dot);
                        float directionDot = Vector3.Dot(camRightYlock, toCube);
                        if (directionDot < 0)
                            rotateSpeed *= -1;

                        transform.RotateAround(GameplayReferences.HMD.position, rotateAxis, rotateSpeed * Time.deltaTime); //lerp this based on how far off forward is
                    }
                }
                else
                {
                    Vector3 toCube = (transform.position - GameplayReferences.HMD.position).normalized;
                    float dot = Vector3.Dot(GameplayReferences.HMD.forward, toCube);
                    if (dot < maxDot)
                    {
                        Vector3 rotateAxis = Vector3.Cross(toCube, GameplayReferences.HMD.forward);
                        float rotateSpeed = Mathf.Lerp(maxRotSpeed, 0, dot);

                        transform.RotateAround(GameplayReferences.HMD.position, rotateAxis, rotateSpeed * Time.deltaTime); //lerp this based on how far off forward is
                    }
                }

                //clamp distance to player
                float dist = Vector3.Distance(transform.position, GameplayReferences.HMD.position);
                if (dist > QuestionSet.myparameters.DisplayDistance)
                {
                    Vector3 vector = (transform.position - GameplayReferences.HMD.position).normalized * QuestionSet.myparameters.DisplayDistance;
                    transform.position = vector + GameplayReferences.HMD.position;
                }
                else if (dist < QuestionSet.myparameters.MinimumDisplayDistance)
                {
                    Vector3 vector = (transform.position - GameplayReferences.HMD.position).normalized * QuestionSet.myparameters.MinimumDisplayDistance;
                    transform.position = vector + GameplayReferences.HMD.position;
                }

                transform.LookAt(transform.position*2 - GameplayReferences.HMD.position); //look in the direction of the panel (inverse of looking at hmd)
            }
        }
        #endregion

        #region Button Actions

        private bool lastBoolAnswer;
        private int lastIntAnswer;
        private string lastRecordedVoice;
        //answer from boolean, thumbs up/down, happy/sad buttons
        private void AnswerBool(bool positive)
        {
            if (_isclosing) { return; }
            int responseValue = 0;
            if (positive)
                responseValue = 1;
            StartCoroutine(CloseAfterWaitForSpecifiedTime(1, responseValue));
        }

        //called directly from MicrophoneButton when recording is complete
        public void AnswerMicrophone(string base64wav)
        {
            if (_isclosing) { return; }
            confirmButton.SetConfirmEnabled();
            lastRecordedVoice = base64wav;
        }

        //called from exitpoll when this panel needs to be cleaned up. does not set response in question set
        public void CloseError(int timeToWait = 1)
        {
            if (_isclosing) { return; }
            StartCoroutine(CloseAfterWaitForSpecifiedTime(timeToWait, short.MinValue));
        }

        public void DisplayError(bool display)
        {
            errorMessage.gameObject.SetActive(display);
        }

        public void DisplayError(bool display, string errorText)
        {
            if (!string.IsNullOrEmpty(errorText))
            {
                errorMessage.text = errorText;
            }
            errorMessage.gameObject.SetActive(display);
        }

        // This will be called from the editor	
        // We have separate functions for positive and negative	
        //      because we can only pass in one argument, and we need to know the image to modify	
        // DO NOT DELETE
        public void AnswerBoolPositive(VirtualButton button)
        {
            positiveButton.SetSelect(true);
            negativeButton.SetSelect(false);
            lastBoolAnswer = true;
            confirmButton.SetConfirmEnabled();
        }

        // This will be called from the editor	
        // We have separate functions for positive and negative	
        //      because we can only pass in one argument, and we need to know the image to modify
        // DO NOT DELETE
        public void AnswerBoolNegative(VirtualButton button)
        {
            negativeButton.SetSelect(true);
            positiveButton.SetSelect(false);
            lastBoolAnswer = false;
            confirmButton.SetConfirmEnabled();
        }

        // DO NOT DELETE
        public void ConfirmBoolAnswer()
        {
            AnswerBool(lastBoolAnswer);
        }

        // from scale, multiple choice buttons
        // DO NOT DELETE	
        public void AnswerInt(int value)
        {
            if (_isclosing) { return; }
            confirmButton.SetConfirmEnabled();
            lastIntAnswer = value;
        }

        // DO NOT DELETE
        public void ConfirmIntAnswer()
        {
            StartCoroutine(CloseAfterWaitForSpecifiedTime(1, lastIntAnswer));
        }

        // DO NOT DELETE
        public void SelectOption(VirtualButton button)
        {
            foreach (GameObject obj in AnswerButtons)
            {
                obj.GetComponentInChildren<VirtualButton>().SetSelect(false);
            }
            button.SetSelect(true);
        }

        // DO NOT DELETE
        public void ConfirmMicrophoneAnswer()
        {
            StartCoroutine(CloseAfterWaitForSpecifiedTimeVoice(1, lastRecordedVoice));
        }

        // DO NOT DELETE
        IEnumerator CloseAfterWaitForSpecifiedTimeVoice(int seconds, string base64)
        {
            PanelRoot.gameObject.SetActive(false);
            yield return new WaitForSeconds(seconds);
            QuestionSet.OnPanelClosedVoice(PanelId, "Answer" + PanelId, base64);
            Close();
        }

        #endregion

        //closes the panel with an invalid number that won't be associated with an answer	
        public void CloseButton()
        {
            if (_isclosing) { return; }
            StartCoroutine(CloseAfterWaitForSpecifiedTime(1, short.MinValue));
        }

        //closes the panel with an invalid number that won't be associated with an answer	
        public void Timeout()
        {
            if (_isclosing) { return; }
            StartCoroutine(CloseAfterWaitForSpecifiedTime(1, short.MinValue));
        }

        //close the window visually. informing the question set has already been completed
        void Close()
        {
            _isclosing = true;
            StartCoroutine(_SetVisible(false));
        }
    }
}