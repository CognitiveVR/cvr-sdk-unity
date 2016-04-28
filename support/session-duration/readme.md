
# Introduction

The code in this folder provides support for using a coroutine to update session progress.

# How to Use

1. Add the following script to your Unity project:

		samples/BubbleGame/Assets/Scripts/SplytHeartbeatSession.cs to your project

2. In your code, after `Splyt.Init` is done, add code like

		// Add this field to your class:
		private SplytHeartbeatSession _session;

		// ...

		// ...and after Splyt.Init is done and invokes its callback, add SplytHeartbeatSession
		// as a component and call Begin() to start tracking the session duration:
		if (_session == null)
		{						
			var g = new GameObject("SplytHeartbeatSession");
			_session = g.AddComponent<SplytHeartbeatSession>();
		}
		_session.Begin();

# Notes on the Implementation

`SplytHeartbeatSession.cs` starts a Splyt session, and then starts a coroutine that updates session progress in an infinite loop.  Initially, the coroutine will report the session is still alive every 30 seconds.  You can only report updates to a session 100 times (each update incrementing the "progress" by 1%), so the coroutine interval gradually increases over time.

This gives you finer-grained durations at the outset, and more coarse duration as users play longer.  We do this so that we can support longer sessions with good resolution for shorter sessions.  Duration stops being measured after about 11 hours of continuous play, since the level of "progress" will have reached 99% by that point.

We're working to automatically measure session duration in the future.  When we do this, this sort of manual reporting will no longer be necessary. But in the meantime, it might be of some use.