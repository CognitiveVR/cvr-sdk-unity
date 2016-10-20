using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using CognitiveVR;

namespace CognitiveVR
{
    public class ExitPollPanel : MonoBehaviour
    {
        [Header("Components")]
        public Text Title;
        public Text Question;

        public GazeButton PositiveButton;
        public GazeButton NegativeButton;
        public GazeButton CloseButton;

        [Header("Display")]
        public float DisplayDistance = 3;

        [Tooltip("Use to HMD Y position instead of spawning the poll directly ahead of the player")]
        public bool LockYPosition;

        [Tooltip("Automatically close this window if there is no response after this many seconds")]
        public float TimeOut = 10;

        public AnimationCurve XScale;
        public AnimationCurve YScale;
        public float PopupTime = 0.2f;

        [Header("Question")]
        public string TitleText;
        [Multiline]
        public string QuestionText;


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

        static ExitPollPanel _panel;
        static ExitPollPanel _instance
        {
            get
            {
                if (_panel == null)
                {
                    _panel = Instantiate(Resources.Load<GameObject>("ExitPollPanel")).GetComponent<ExitPollPanel>();
                }
                return _panel;
            }
        }

        public static void Initialize(System.Action closeAction)
        {
            if (CognitiveVR_Manager.HMD == null) { return; } //TODO fail message

            //set position and rotation
            Vector3 position = CognitiveVR_Manager.HMD.position + CognitiveVR_Manager.HMD.forward * _instance.DisplayDistance;
            _instance.transform.rotation = CognitiveVR_Manager.HMD.rotation;
            if (_instance.LockYPosition)
            {
                Vector3 modifiedForward = CognitiveVR_Manager.HMD.forward;
                modifiedForward.y = 0;
                modifiedForward.Normalize();

                position = CognitiveVR_Manager.HMD.position + modifiedForward * _instance.DisplayDistance;
                _instance.transform.rotation = Quaternion.LookRotation(CognitiveVR_Manager.HMD.position - position, Vector3.up);
            }


            //set up actions
            _instance.PositiveButton.SetAction(() => _instance.Answer(true));
            _instance.NegativeButton.SetAction(() => _instance.Answer(false));
            if (_instance.CloseButton != null)
            {
                _instance.CloseButton.SetAction(() => _instance.Close());
            }

            //fetch a question
            _instance.StartCoroutine(_instance.FetchQuestion(closeAction));
        }

        //close action is called immediately if fetching the question fails
        IEnumerator FetchQuestion(System.Action closeAction)
        {
            //ask server

            //tuning variable
            string response = Tuning.getVar<string>("ExitPoll", string.Empty);
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
                    if (closeAction != null)
                        closeAction.Invoke();
                }

                yield break;
            }

            //hard coded
            Title.text = TitleText;
            Question.text = QuestionText;
            SetVisible(true);

            yield return null;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SetVisible(true);
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                SetVisible(false);
            }
            //debug only
        }

        public void SetVisible(bool visible)
        {
            //runs x/y scale through animation curve
            StartCoroutine(DoSetVisible(visible));
        }

        IEnumerator DoSetVisible(bool visible)
        {
            float normalizedTime = 0;
            if (visible)
            {
                while (normalizedTime < 1)
                {
                    normalizedTime += Time.deltaTime / PopupTime;
                    _transform.localScale = new Vector3(XScale.Evaluate(normalizedTime), YScale.Evaluate(normalizedTime));
                    yield return null;
                }
                _transform.localScale = Vector3.one;
            }
            else
            {
                normalizedTime = 1;
                while (normalizedTime > 0)
                {
                    normalizedTime -= Time.deltaTime / PopupTime;
                    _transform.localScale = new Vector3(XScale.Evaluate(normalizedTime), YScale.Evaluate(normalizedTime));
                    yield return null;
                }
                _transform.localScale = Vector3.zero;
            }
        }

        void Answer(bool positive)
        {
            //send something to a server somwhere

            Instrumentation.Transaction("ExitPoll").setProperty("Question", Question.text).setProperty("Answer", positive).beginAndEnd();


            Close();
        }

        void Close()
        {
            SetVisible(false);
        }
    }
}