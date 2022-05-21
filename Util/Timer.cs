using Sandbox;

namespace Source1;

class Timer
{
	float ElapseTime = -1;
    float Duration = 0;
    bool FinishCalled = false;

	public Timer( float time = 0 )
	{
		if ( time > 0 )
			Start( time );
	}

	public void Start( float time )
	{
		Invalidate();
		ElapseTime = Time.Now + time;
		Duration = time;
	}

	public void Reset()
	{
		ElapseTime = Time.Now + Duration;
	}

	public void Invalidate()
	{
		ElapseTime = -1;
		FinishCalled = false;
	}

	public bool HasStarted() => ElapseTime > 0;
	public bool IsElapsed() => Time.Now > ElapseTime;
	public float ElapsedTime() => Time.Now - ElapseTime + Duration;
	public float RemainingTime() => ElapseTime - Time.Now;
	public bool HasFinished()
	{
		if ( FinishCalled )
			return false;

		if ( HasStarted() && IsElapsed() )
		{
			FinishCalled = true;
			return true;
		}

		return false;
	}
}
