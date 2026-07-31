using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Cognitive3D.Identify
{
    /// <summary>
    /// Result of resolving a QR token into participant information.
    /// </summary>
    public class TokenResult
    {
        public bool Success;
        public string ParticipantId;
        public string ParticipantName;
        public string ParticipantEmail;
        public string ErrorMessage;
    }

    public class TokenResolver : MonoBehaviour
    {
        [Header("API Settings")]
        [Tooltip("The endpoint URL to send the code to.")]
        [SerializeField] private string endpointUrl = "https://data.cognitive3d.com/v0/identify/exchange";
        [Tooltip("Request timeout in seconds.")]
        [SerializeField] private int timeoutSeconds = 10;

        public void ResolveToken(string token, Action<TokenResult> onComplete)
        {
            StartCoroutine(SendRequest(token, onComplete));
        }

        /// <summary>
        /// Copies the API configuration (endpoint + timeout) from another resolver, so a spawned
        /// panel can resolve against the same backend as the panel that spawned it.
        /// </summary>
        public void CopyConfigFrom(TokenResolver source)
        {
            if (source == null) return;
            endpointUrl = source.endpointUrl;
            timeoutSeconds = source.timeoutSeconds;
        }

        private IEnumerator SendRequest(string code, Action<TokenResult> onComplete)
        {
            string json = JsonUtility.ToJson(new AuthRequest { code = code });

            using (UnityWebRequest request = new UnityWebRequest(endpointUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", "APIKEY:DATA " + Cognitive3D_Preferences.Instance.ApplicationKey);
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = timeoutSeconds;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("[COGNITIVE3D] TokenResolver: Request failed: " + request.error);
                    onComplete?.Invoke(new TokenResult
                    {
                        Success = false,
                        ErrorMessage = request.error
                    });
                    yield break;
                }

                string responseBody = request.downloadHandler.text;

                try
                {
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(responseBody);

                    onComplete?.Invoke(new TokenResult
                    {
                        Success = true,
                        ParticipantId = response.participantId,
                        ParticipantName = response.name,
                        ParticipantEmail = response.email,
                        ErrorMessage = null
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError("[COGNITIVE3D] TokenResolver: Failed to parse response: " + e.Message);
                    onComplete?.Invoke(new TokenResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse server response"
                    });
                }
            }
        }

        [Serializable]
        private class AuthRequest
        {
            public string code;
        }

        [Serializable]
        private class AuthResponse
        {
            public string participantId;
            public string name;
            public string email;
        }
    }
}
