// when enabled, this forces a 10-second delay at the start of the app to allow time to connect a remote debugging session
//#define DEBUG_DELAY
using SysDiag = System.Diagnostics;
using UnityEngine;
using System.Collections.Generic;


public class BubblePop : MonoBehaviour 
{
	enum Mode 
	{
		Free,
		Pay
	}
	
	private const int NUM_BUBBLES = 4;
	
	private Mode mMode;
	private string mGreeting;
	private int mGold;
	private int mGameCost;
	
	private int mMisses = 0;
	private bool mPlaying = false;
	private bool mReady = false;
	private bool mInited = false;
	private bool mPurchasing = false;
	private bool mWonLast = false;

	private string mPurchaseId;

	#if DEBUG_DELAY
	bool mDelayedInitOccurred = false;
	void Update()
	{
		// in this mode we want to run the init code 1 time AFTER a 10 second delay
		if(Time.time < 10.0 || mDelayedInitOccurred == true)
			return;
		mDelayedInitOccurred = true;
	#else
	void Awake()
	{
	#endif
		CognitiveVR.InitParams initParams = CognitiveVR.InitParams.create(
		"mmginc27715-cardboarddemo-test"								// (required) Customer ID from the CognitiveVR team.  If you don't have one, contact them.
		//,userInfo: CognitiveVR.EntityInfo.createUserInfo("joe")		// (optional) Only necessary if user info is known at startup, otherwise use registerUser later
		//,deviceInfo: CognitiveVR.EntityInfo.createDeviceInfo().setProperty("screenwidth", 1024)	// (optional) Only generally needed if device properties are sent at startup
		//,requestTimeout: 1500									// (optional) Only necessary if the default is inadequate
		//,logEnabled: true										// (optional) Typically only set to true during development
        //,host: "http://localhost"								// (don't use) This is for CognitiveVR developers only
		);
		initParams.OnNotification = delegate(string message, bool wasLaunched)
		{
			Debug.Log("initParams.OnNotification: " + (wasLaunched ? "wasLaunched" : "!wasLaunched") + ": " + message);
		};
		
		Debug.Log ("BubblePop.Awake()");
		CognitiveVR.Core.init(initParams, delegate(CognitiveVR.Error initError) 
		{
			if(CognitiveVR.Error.Success == initError)
				Debug.Log("onCognitiveVRInitComplete: " + initError.ToString());
			else
				Debug.LogError("onCognitiveVRInitComplete: " + initError.ToString());
			
			// in this contrived case, we learn about the user just after init - generally this would be triggered by a user action (login?)
			CognitiveVR.EntityInfo user = CognitiveVR.EntityInfo.createUserInfo(
				"joe",
				properties: new Dictionary<string, object> { { "funguy", true }, { "favorite team", "Sweepers" } }
			);
			
			CognitiveVR.Core.registerUser(user, delegate(CognitiveVR.Error registerError) {
				if(CognitiveVR.Error.Success == registerError)
					Debug.Log("onCognitiveVRRegisterUserComplete: " + registerError.ToString());
				else
					Debug.LogError("onCognitiveVRRegisterUserComplete: " + registerError.ToString());
				
				OnGameReady();
			});
		});
	// Monodevelop won't play nice if it counts the wrong number of braces :(
	#if DEBUG_DELAY
	}
	#else
	}
	#endif

	//
	// For this sample, we just use Unity's immediate-mode GUI
	//
	void OnGUI()
	{
		GUI.Label(new Rect(200, 20, 200, 20), mGreeting);
		GUI.Label(new Rect(400, 40, 200, 20), "Gold: " + mGold);
		if(mPlaying)
		{
			/*
			if(GUI.Button(new Rect(40,40,200,20), "Finish Game"))
			{
				_FinishGame();
			}
			*/
		}
		else
		{
			if(mReady)
			{
				if(GUI.Button(new Rect(40,40,200,20), "Start Game for " + ((mMode == Mode.Pay) ? mGameCost.ToString() + " gold" : "Free")))
				{
					_TryStartGame();
				}
				
				if(mPurchasing)
				{
					if(GUI.Button(new Rect(40,80,200,20), "Confirm Purchase?"))
					{
						_CompleteGoldPurchase();
					}
					
					if(GUI.Button(new Rect(40,120,200,20), "Cancel Purchase"))
					{
						_CancelGoldPurchase();
					}
				}
				else
				{
					if(GUI.Button(new Rect(40,80,200,20), "Buy 200 Gold!"))
					{
						_BeginGoldPurchase();
					}
				}
			}
			else
			{
				if(mInited && GUI.Button(new Rect(40,40,200,20), "You " + (mWonLast?"won":"lost") + "! Ok?"))
				{
					_DismissResults();
				}
			}
		}
		
	}
	//
	// Sent to all game objects when the player pauses/resumes. We need to pause/resume cognitivevr's core as well
	//
	void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
		{
			CognitiveVR.Core.pause();
		}
		else
		{
			CognitiveVR.Core.resume();
		}
	}

	//
	// Error event handler from Startup module
	//
	void OnStartupError(string location)
	{
		Debug.LogError("Error rec'd starting up " + location);
	}
	
	//
	// Ready event handler from Startup module - expected at startup, and again between each game
	//
	void OnGameReady()
	{
		_InitGame();
		
		mReady = true;
	}

	//
	// Pop event handler from bubbles, mainly responsible for game-end logic
	//
	void OnPop(bool hasPrize)
	{
		if(hasPrize)
		{
			_FinishGame(true);
		}
		else
		{
			if(++mMisses == NUM_BUBBLES - 1)
			{
				_FinishGame(false);
			}
		}
	}
	
	//
	// Starts up a game if the user has the cash to do it
	// 
	private void _TryStartGame()
	{
		if(!_BuyGame())
		{
			return;
		}
		
		_StartGame();
	}
	
	//
	// Starts up the game - assumes validation happens elsewhere
	//
	private void _StartGame()
	{
		CognitiveVR.Instrumentation.Transaction("game").begin();

		mMisses = 0;
		
		var bubbleProto = Resources.Load("Bubble");
		float x = -(NUM_BUBBLES - 1f);
		
		// create 4 bubbles, one of which is given the 'prize'
		int prizeGuy = ((int)(Random.value * NUM_BUBBLES)) % NUM_BUBBLES;
		for(int idx = 0; idx < NUM_BUBBLES; ++idx)
		{
			GameObject bubble = (GameObject) Instantiate(bubbleProto);
			bubble.name = "bubble" + idx;
			bubble.transform.position = new Vector3(x, 0, 0); 
			bubble.transform.parent = transform;
			
			if(idx == prizeGuy)
			{
				bubble.GetComponent<Bubble>().HasPrize = true;
			}

			x += 2f;
		}
		
		mPlaying = true;
		mReady = false;
	}
	
	//
	// End of game processing
	//
	private void _FinishGame(bool won)
	{	
		CognitiveVR.Instrumentation.Transaction("game")
			.setProperties(new Dictionary<string, object> {
				{ "numberOfMisses", this.mMisses },
				{ "didWin", won },
				{ "winQuality", _GetScore() }
			})
			.end();

		if(won)
		{
			mGold += 110;
		}
		
		// clean up any remaining bubbles
		BroadcastMessage("OnPopAll");
		
		mWonLast = won;
		mPlaying = false;
	}
	
	//
	// Does necessary work between games
	//
	private void _DismissResults()
	{
		CognitiveVR.Tuning.refresh(delegate(CognitiveVR.Error refreshError) {
			if(CognitiveVR.Error.Success == refreshError)
				Debug.Log("onCognitiveVRRefreshComplete: " + refreshError.ToString());
			else
				Debug.LogError("onCognitiveVRRefreshComplete: " + refreshError.ToString());

			// even if we received an error, we want to let the app continue
			OnGameReady();
		});
	}
	
	//
	// Purchase a game session, and send necessary telemetry
	//
	private bool _BuyGame()
	{
		if(mMode == Mode.Pay)
		{
			if(mGold < mGameCost)
			{
				return false;
			}
			mGold -= mGameCost;

			// we really don't need a transaction id in this case
			//string purchaseId = System.Guid.NewGuid().ToString();

			CognitiveVR.Plugins.Purchase.Transaction()
				.setProperty( "type", "virtual" )
				.setPrice(100.0, "gold")
				.setOfferId("game")
				.setItemName("New Game")
				.setPointOfSale("Main Menu")
				.beginAndEnd();
		}
		return true;
	}
	
	//
	// Purchase virtual currency with real money (but not really), and send necessary telemetry
	//
	private void _BeginGoldPurchase()
	{
		// you don't NEED a purchase id unless multiple purchases may be occuring simulatenously, but it's included here to show the API
		mPurchaseId = System.Guid.NewGuid().ToString();

		CognitiveVR.Plugins.Purchase.Transaction(mPurchaseId)
			.setPointOfSale("Main Menu")
			.begin();

		// let's assume we don't learn more about the item until after this purchase process has begun, for this case...  we can add
		//  more properties through an update
		CognitiveVR.Plugins.Purchase.Transaction(mPurchaseId)
			.setPrice(1.99, "usd")
			.setOfferId("200gold_199")
			.setItemName("200 Gold")
			.update(1);

		mPurchasing = true;
	}
	
	//
	// Acknowledge 'ok' response for purchase
	//
	private void _CompleteGoldPurchase()
	{
		mGold += 200;

		CognitiveVR.Plugins.Purchase.Transaction(mPurchaseId).end();

		mPurchasing = false;
	}
	
	//
	// Acknowledge 'cancel' response from purchase
	//
	private void _CancelGoldPurchase()
	{
		CognitiveVR.Plugins.Purchase.Transaction(mPurchaseId).end(CognitiveVR.Constants.TXN_ERROR);

		mPurchasing = false;
	}
	
	// Gets a game score.
	//
	// Score is on a 0 to 1 scale, based on the number of pops.
	//
	// Score starts at 1 and decreases with each pop. If all bubbles are popped 
	// before the player wins, final score is 0.
	private float _GetScore()
	{
		SysDiag.Debug.Assert(NUM_BUBBLES > 1);
		
		return (NUM_BUBBLES - (mMisses + 1)) / (float)(NUM_BUBBLES - 1);
	}

	//
	// Startup actions for the game, including initial startup and between game preparation for the next game
	//
	private void _InitGame()
	{
		if(false == mInited)
		{
			CognitiveVR.Plugins.Session.Transaction().begin();

			// only initialize starting gold the first time around
			mGold = CognitiveVR.Tuning.getVar("initialGoldAmount", 200);
			
			mInited = true;
		}	
		
		// Retrieve a value for a string variable, which can be dynamically set on the server
		mGreeting = CognitiveVR.Tuning.getVar("greeting", "Hello Popper!");

		// Integers can also be pulled down quite easily...  note the default value, which is used if no value is sent from the server
		mGameCost = CognitiveVR.Tuning.getVar("newGameCost", 100);
			
		// Enums work out-of-the-box, as will any type which can be natively parsed from a string value
		mMode = CognitiveVR.Tuning.getVar("gameMode", Mode.Pay);
		
		// Unity's Color struct is not an enum, so we take a different approach for tuning that
		string colorValue = CognitiveVR.Tuning.getVar("bubbleColor", "cyan");
		
		// we cheat and update the color for the light component, since the bubbles are the only 3d thing we're rendering...
		Light light = GetComponent<Light>();
		light.color = _GetColor(colorValue, light.color);
	}
	
	//
	// Parsing a string into a Unity Color struct, is possible
	//
	private static Color _GetColor(string colorName, Color current)
	{
		Color color = current;
		
		System.Reflection.PropertyInfo property = typeof(Color).GetProperty(colorName);
		if(null != property)
		{
			color = (Color) property.GetValue(color, null);
		}
		
		return color;
	}
}
		