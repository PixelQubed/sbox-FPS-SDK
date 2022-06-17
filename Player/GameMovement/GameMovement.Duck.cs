using Sandbox;

namespace Amper.Source1;

partial class Source1GameMovement
{
	public virtual bool IsDucking => DuckTime > 0;
	public virtual float TimeToDuck => .2f;
	public virtual float DuckProgress => DuckTime / TimeToDuck;
	[Net, Predicted] public float DuckTime { get; set; }

	public virtual bool WishDuck() => Input.Down( InputButton.Duck );

	public virtual void SimulateDucking()
	{
		if ( WishDuck() )
		{
			OnDucking();
		}
		else
		{
			OnUnducking();
		}

		HandleDuckingSpeedCrop();
	}

	public virtual void OnDucking()
	{
		if ( !CanDuck() )
			return;

		DuckTime = DuckTime.Approach( TimeToDuck, Time.Delta );

		if ( Player.IsDucked )
			return;

		if ( DuckTime >= TimeToDuck || IsInAir )
			FinishDuck();
	}

	public virtual void OnUnducking()
	{
		if ( !CanUnduck() )
			return;

		DuckTime = DuckTime.Approach( 0, Time.Delta );

		if ( !Player.IsDucked )
			return;

		if ( DuckTime <= 0 || IsInAir )
			FinishUnDuck();
	}

	private void DuckLog( string msg )
	{
		Log.Info( $"[{(Host.IsServer ? "SV" : "CL")}] {msg}" );
	}

	public virtual void FinishDuck()
	{
		if ( Player.IsDucked ) 
			return;

		DuckLog( Player + "::FinishDuck()" );

		Player.IsDucked = true;
		DuckTime = TimeToDuck;

		if ( IsGrounded )
		{
			Position -= GetPlayerMins( true ) - GetPlayerMins( false );
		}
		else
		{
			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;
			Position += viewDelta;
		}

		// See if we are stuck?
		FixPlayerCrouchStuck( true );
		CategorizePosition();
	}

	public virtual void FinishUnDuck()
	{
		if ( !Player.IsDucked )
			return;

		DuckLog( Player + "::FinishUnDuck()" );

		Player.IsDucked = false;
		DuckTime = 0;

		if ( IsGrounded )
		{
			Position += GetPlayerMins( true ) - GetPlayerMins( false );
		}
		else
		{
			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;
			Position -= viewDelta;
		}

		// Recategorize position since ducking can change origin
		CategorizePosition();
	}

	public virtual bool CanDuck()
	{
		return true;
	}

	public virtual bool CanUnduck()
	{
		var origin = Position;

		if ( IsGrounded )
		{
			origin += GetPlayerMins( true ) - GetPlayerMins( false );
		}
		else
		{
			var normalHull = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var duckedHull = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = normalHull - duckedHull;
			origin -= viewDelta;
		}

		bool wasDucked = Player.Tags.Has( PlayerTags.Ducked );

		if ( wasDucked ) Player.Tags.Remove( PlayerTags.Ducked );
		var trace = TraceBBox( Position, origin );
		if ( wasDucked ) Player.Tags.Add( PlayerTags.Ducked );

		if ( trace.StartedSolid || trace.Fraction != 1 )
			return false;

		return true;
	}

	public virtual void FixPlayerCrouchStuck( bool upward )
	{
		int direction = upward ? 1 : 0;

		var trace = TraceBBox( Position, Position );
		if ( trace.Entity == null )
			return;

		var test = Position;
		for ( int i = 0; i < 36; i++ )
		{
			var org = Position;
			org.z += direction;

			Position = org;
			trace = TraceBBox( Position, Position );
			if ( trace.Entity == null )
				return;
		}

		Position = test;
	}

	public virtual void HandleDuckingSpeedCrop()
	{
		if ( Player.IsDucked && IsGrounded )
		{
			ForwardMove *= Player.DuckingSpeedMultiplier;
			RightMove *= Player.DuckingSpeedMultiplier;
			UpMove *= Player.DuckingSpeedMultiplier;
		}
	}
}
