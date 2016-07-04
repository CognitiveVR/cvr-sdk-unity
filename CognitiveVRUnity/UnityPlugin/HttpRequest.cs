using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    internal class HttpRequest : MonoBehaviour
    {
        private enum State
        {
            FetchPolicy,
            Connect,
            ReadResponse,
            Complete,
            Finished
        }

        private enum ResponseState
        {
            StatusCode,
            Headers,
            Content
        }

        //// A URI that identifies the Internet resource (i.e., the URL)
        private string mUrl;

        //// Timeout, in milliseconds, for the request
        private int mTimeout;

        //// The time of the start of this request
        private System.DateTime mStartTime;

        //// Data to send (optional)
        private string mSendData;

        //// The listener to callback when the request is complete
        private Listener mListener;

        //// The Request state
        private State mState;

        //// The Request result
        private Result mResult;

        //// The WWW request
        private WWW mWWW;

        internal HttpRequest()
        {
            mResult = new Result();

            // Start off by fetching the policy (crossdomain.xml)
            // This security restriction applies only to the webplayer, and to the editor when the active build target is WebPlayer.
            if (sIsWebPlayer)
            {
                mState = State.FetchPolicy;
            }
            else
            {
                // Otherwise, just open the connection
                mState = State.Connect;
            }

            // Set the start time to now
            mStartTime = System.DateTime.UtcNow;
        }

        // Overriden from MonoBehaviour
        // This method processes the main request state machine
        void Update()
        {
            // If we're not already done, check for timeout
            if (State.Complete != mState && State.Finished != mState)
            {
                System.TimeSpan requestTime = System.DateTime.UtcNow - mStartTime;
                if ((int)requestTime.TotalMilliseconds > mTimeout)
                {
                    // We're out of time
                    mResult.ErrorCode = Error.RequestTimedOut;
                    mState = State.Complete;
                }
            }

            switch (mState)
            {
                case State.FetchPolicy:
                    // See http://docs.unity3d.com/Documentation/Manual/SecuritySandbox.html
                    System.Uri uri = new System.Uri(mUrl);
                    System.Net.IPAddress[] addr = System.Net.Dns.GetHostAddresses(uri.Host);
                    if (addr.Length > 0)
                    {
                        // 1024 is the port our servers listen on.  Do NOT change this unless you know what you're doing.
                        if (Security.PrefetchSocketPolicy(addr[0].ToString(), 1024))
                        {
                            // Success! Now, let's connect to the server
                            mState = State.Connect;
                        }
                        else
                        {
                            // Couldn't fetch the policy file.
                            Util.logError("Failed to fetch the policy file.");

                            // This is a problem, we're done
                            mResult.ErrorCode = Error.Generic;
                            mState = State.Complete;
                        }
                    }
                    else
                    {
                        // Couldn't resolve the Host? 
                        Util.logError("Failed to resolve the host.");

                        // This is a problem, we're done...
                        mResult.ErrorCode = Error.InvalidArgs;
                        mState = State.Complete;
                    }
                    break;
                case State.Connect:
                    mState = State.ReadResponse;

                    /*
                    System.Collections.Hashtable headers = new System.Collections.Hashtable(2) {
                        { "ssf-use-positional-post-params", "true" },
                        { "ssf-contents-not-url-encoded", "true" }
                    };
                    */
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    headers.Add("ssf-use-positional-post-params", "true");
                    headers.Add("ssf-contents-not-url-encoded", "true");

                    mWWW = new WWW(mUrl, System.Text.Encoding.UTF8.GetBytes(mSendData), headers);
                    break;
                case State.ReadResponse:
                    if (mWWW.isDone)
                    {
                        mState = State.Complete;
                        if (mWWW.error != null)
                            mResult.ErrorCode = Error.Generic;
                        else
                            mResult.Response = mWWW.text;
                    }
                    break;
                case State.Complete:
                    // Perform any cleanup
                    if(null != mWWW) mWWW.Dispose();

                    // Call back the listener with the result
                    if (null != mListener)
                    {
                        mListener.onComplete(mResult);
                    }

                    // Remove ourselves...
                    GameObject.Destroy(gameObject);

                    // Move into the finished state while we wait to be destroyed
                    mState = State.Finished;
                    break;
                case State.Finished:
                    // Waiting to be destroyed
                    break;
            }
        }

        private static GameObject sHubObj;
        private static bool sIsWebPlayer;

        #region "public" methods
        internal static void init(string hubObjName, bool isWebPlayer)
        {
            sHubObj = GameObject.Find(hubObjName);
            sIsWebPlayer = isWebPlayer;
        }

        internal static void executeAsync(System.Uri uri, int requestTimeout, string sendData, Listener listener)
        {
            // Create the request and add it to the Hub Object so that it gets updated
            HttpRequest req = sHubObj.AddComponent<HttpRequest>();
            req.mUrl = uri.ToString();
            req.mTimeout = requestTimeout;
            req.mSendData = sendData;
            req.mListener = listener;
        }
        #endregion

        #region helper classes/interfaces
        internal class Result
        {
            internal Error ErrorCode { get; set; } // Result error code
            internal string Response { get; set; } // Server response (null if no response)

            internal Result()
            {
                // Assume Success
                ErrorCode = Error.Success;
            }
        }

        internal interface Listener
        {
            void onComplete(Result result);
        }
        #endregion
    }
}
