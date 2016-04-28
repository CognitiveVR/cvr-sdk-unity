using System;
using System.Collections;
using UnityEngine;

public class SplytHeartbeatSession : MonoBehaviour
{				
	public SplytHeartbeatSession() : this(INITIAL_INTERVAL_SECONDS, GetNextIntervalSec)
	{
		System.Console.WriteLine("got here");
	}
	
	public SplytHeartbeatSession(float initialIntervalSec = INITIAL_INTERVAL_SECONDS) : this(initialIntervalSec, GetNextIntervalSec)
	{ }
	
	public SplytHeartbeatSession(float initialIntervalSec, IntervalCalculationDelegate intervalCalculator)
	{			
		_initialInterval = initialIntervalSec;
		_intervalCalculator = intervalCalculator;		
	}
			
	public void Begin()
	{
		lock (_sessionLock)
		{			
			if (_sessionStarted)
			{
				throw new InvalidOperationException("A session is already in progress.");
			}
			
			_sessionStarted = true;								
			SplytAnalytics.Error error = SplytAnalytics.Session.Begin();
			
			if (error == SplytAnalytics.Error.Success)
			{				
				_heartbeatState = new HeartbeatState(0, _initialInterval);	
				StartCoroutine("UpdateSession", _heartbeatState);				
			}			
		}			
	}
	
	public void End()		
	{
		lock (_sessionLock)
		{
			if (!_sessionStarted)
			{
				throw new InvalidOperationException("A session is not in progress and cannot be ended.");
			}
						
			StopCoroutine("UpdateSession");
				
			_sessionStarted = false;
			
			 SplytAnalytics.Session.End();
		}
	}
	
	private void OnDisable()
	{
		StopCoroutine("UpdateSession");
	}	
			
	IEnumerator UpdateSession(HeartbeatState hs)
	{		
		while (true) 
		{
			yield return new WaitForSeconds(hs.IntervalSec);
			
			// Update the interval for the next time around.
			hs.IntervalSec = _intervalCalculator(_initialInterval, hs.IntervalSec, hs.ProgressPct, (float) hs.Elapsed.TotalSeconds);
				
			// Progress beyond 100% is not allowed.  100% will be reported by End().
			if (hs.ProgressPct < 100)
			{
				hs.ProgressPct++;
				SplytAnalytics.Session.UpdateProgress(hs.ProgressPct); 
			}	
		}
	}
	
	// Default interval calculator; an alternate can be supplied when constructing
	// a SplytHeartbeatSession instance.
	private static float GetNextIntervalSec(float initialIntervalSec, float priorIntervalSec, int priorProgressPct, float timeElapsedSec)
	{
		// Use simple exponential growth to determine the next interval, with 4% rate.
		//
		// With 100 progress intervals, and assuming an initial interval of every 30s, this supports 
		// session times of approximately 11 hours max.
		return (float) ( initialIntervalSec * Math.Pow( 1.04, priorProgressPct ) );
	}
	
	private bool _disposed;
	private HeartbeatState _heartbeatState;
	private float _initialInterval;
	private IntervalCalculationDelegate _intervalCalculator;
	private object _sessionLock = new object();
	private bool _sessionStarted;

	public const float INITIAL_INTERVAL_SECONDS = 30.0f;

	class HeartbeatState
	{				
		public HeartbeatState() 
		{ }

		public HeartbeatState(int progress, float interval) : this(progress, interval, DateTime.UtcNow)
		{ }

		public HeartbeatState(int progress, float interval, DateTime startTime)
		{
			_progress = progress;
			_interval = interval;
			_startTime = startTime;
		}
		
		public TimeSpan Elapsed
		{
			get { return DateTime.UtcNow - _startTime; }
		}
		
		public float IntervalSec
		{
			get { return _interval; }
			set { _interval = value; }
		}
		
		public int ProgressPct 
		{ 
			get { return _progress; }
			set { _progress = value; }
		} 
		
		private float _interval = SplytHeartbeatSession.INITIAL_INTERVAL_SECONDS;
		private int _progress;
		private readonly DateTime _startTime;
	}
}

public delegate float IntervalCalculationDelegate(float initialIntervalSec, float priorIntervalSec, int priorProgressPct, float timeElapsed); 


