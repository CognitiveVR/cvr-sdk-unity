using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Cognitive3D
{
    public class CustomExitPollManager : MonoBehaviour
    {
        /// <summary>
        /// The hook (set on the dashboard) to access
        /// </summary>
        public string HookName;

        //main properties to consider is 'question'. 'type' is useful for choosing which prefab to spawn
        //multiple choice questions are pipe-separated in 'csvanswers' as 'my answer one|my answer two'
        //see ExitPollPanel.cs for examples of initializing prefabs with these values
        /// <summary>
        /// //The returned list of questions with properties for each
        /// </summary>
        public List<Dictionary<string, string>> QuestionProperties = new List<Dictionary<string, string>>();

        /// <summary>
        /// called when getting the questions fails for any reason
        /// </summary>
        public UnityEngine.Events.UnityEvent OnSetupFailed;

        /// <summary>
        /// called when questions returned and parsed. Accessible in QuestionProperties
        /// </summary>
        public UnityEngine.Events.UnityEvent OnSetupComplete;

        /// <summary>
        /// called when the answers are recorded and sent
        /// </summary>
        public UnityEngine.Events.UnityEvent OnSurveyComplete;

        /// <summary>
        /// called when the microphone begins recording
        /// </summary>
        public UnityEngine.Events.UnityEvent OnMicrophoneRecordingBegin;

        /// <summary>
        /// called when the microphone recording time has expired
        /// </summary>
        public UnityEngine.Events.UnityEvent OnMicrophoneRecordingTimeUp;

#region Internal Variables
        private double StartTime;

        //dictionaries for user answers
        Dictionary<int, ExitPollSet.ResponseContext> responseProperties = new Dictionary<int, ExitPollSet.ResponseContext>();
        Dictionary<string, object> eventProperties = new Dictionary<string, object>();
#endregion
        
        // This function should be called during OnSessionBegin if the user wants to immediately enable the exit poll upon Awake or OnEnable.
        public async Task<ExitPollData> GetExitPollQuestionSets()
        {
            if (Cognitive3D_Manager.Instance == null)
            {
                Util.logDebug("Cannot display exitpoll. Cognitive3DManager not present in scene");
                return null;
            }

            // Using a TaskCompletionSource to wait for the asynchronous callback to complete
            var tcs = new TaskCompletionSource<ExitPollData>();

            Cognitive3D.NetworkManager.GetExitPollQuestions(HookName, (responseCode, error, text) =>
            {
                var exitPollData = ProcessExitPollResponse(responseCode, error, text);
                tcs.SetResult(exitPollData);
            }, 3);

            // Waiting for the result to be available and return it
            return await tcs.Task;
        }

        private ExitPollData ProcessExitPollResponse(int responseCode, string error, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogError("C3D Exitpoll returned empty text");
                OnSetupFailed.Invoke();
                return null;
            }

            ExitPollData _exitPollData;
            try
            {
                _exitPollData = JsonUtility.FromJson<ExitPollData>(text);
            }
            catch
            {
                Debug.LogError("C3D Exitpoll questions not formatted correctly");
                OnSetupFailed.Invoke();
                return null;
            }

            if (_exitPollData.questions == null || _exitPollData.questions.Length == 0)
            {
                Debug.LogError("C3D Exitpoll has no questions");
                OnSetupFailed.Invoke();
                return null;
            }

            OnSetupComplete.Invoke();
            StartTime = Util.Timestamp();

            return _exitPollData;
        }

        /// <summary>
        /// Records a user's answer, to be submitted later
        /// boolean - answerValue is 0 false, 1 true
        /// multiple choice - answerValue starts at 1 for the first option, 2 for the second option, etc to a maximum of 4
        /// scale - answerValue matches the number displayed (scale can allow '0' values)
        /// </summary>
        /// <param name="questionIndex"></param>
        /// <param name="answerValue"></param>
        public void RecordAnswer(int questionIndex, int answerValue)
        {
            string key = "Answer" + questionIndex;

            if (eventProperties.ContainsKey(key))
            {
                eventProperties[key] = answerValue;
            }
            else
            {
                Debug.LogError("Exitpoll expected response not defined for question index " + questionIndex);
            }

            if (responseProperties.ContainsKey(questionIndex))
            {
                responseProperties[questionIndex].ResponseValue = answerValue;
            }
            else
            {
                Debug.LogError("Exitpoll expected response not defined for question index " + questionIndex);
            }
        }
    }
}
