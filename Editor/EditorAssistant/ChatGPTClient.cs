using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cognitive3D.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using static Cognitive3D.NetworkManager;

namespace Cognitive3D
{
    public class ChatGPTClient
    {
        [SerializeField] private const string API_KEY = "";
        private const string API_URL = "https://api.openai.com/v1/chat/completions";

        private static string _currentChat;

        public static async Task<string> SendMessageToChatGPT(string userMessage)
        {
            if (string.IsNullOrEmpty(API_KEY))
			{
				throw new System.Exception("Missing Api Key");
			}

            userMessage = ContextProvider.SearchFilesByPrompt(userMessage);
            _currentChat += userMessage;

            var payload = new ChatRequest
            {
                model = "gpt-3.5-turbo",
                messages = new List<ChatMessage>
                {
                    new ChatMessage { role = "user", content = _currentChat }
                },
            };
            string json = JsonConvert.SerializeObject(payload);

            UnityWebRequest request = new UnityWebRequest(API_URL, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Authorization", $"Bearer {API_KEY}");
            request.SetRequestHeader("Content-Type", "application/json");
            var asyncOp = request.SendWebRequest();
			while (!asyncOp.isDone)
			{
				await Task.Yield();
			}

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseJson = request.downloadHandler.text;
                ChatResponse response = JsonConvert.DeserializeObject<ChatResponse>(responseJson);
                _currentChat += response.choices[0].message.content;
                return response.choices[0].message.content;
            }
            else
            {
                Debug.LogError($"Error: {request.responseCode} - {request.error}");
                // if (request.responseCode == 429)
                // {
                //     Debug.LogWarning("Rate limited. Retrying in 5 seconds...");
                //     await Task.Delay(5000);
                //     await SendMessageToChatGPT(userMessage); // Retry
                // }
                return "Error: " + request.error;
            }
        }

        static async Task<bool> IsConnectedToOpenAI()
        {
            UnityWebRequest request = UnityWebRequest.Get("https://api.openai.com/v1/models");
            request.SetRequestHeader("Authorization", $"Bearer {API_KEY}");

            var asyncOp = request.SendWebRequest();
            while (!asyncOp.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("OpenAI API is reachable.");
                return true;
            }
            else
            {
                Debug.LogWarning($"OpenAI API connection failed: {request.responseCode} - {request.error}");
                return false;
            }
        }

        internal static void Clear()
        {
            _currentChat = "";
        }

        // Request/Response helper classes
        [System.Serializable]
        public class ChatRequest
        {
            public string model;
            public List<ChatMessage> messages;
        }

        [System.Serializable]
        public class ChatMessage
        {
            public string role;
            public string content;
        }

        [System.Serializable]
        public class ChatResponse
        {
            public List<ChatChoice> choices;
        }

        [System.Serializable]
        public class ChatChoice
        {
            public ChatMessage message;
        }
    }
}
