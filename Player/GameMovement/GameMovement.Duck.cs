using Sandbox;
using System;

namespace Amper.Source1;

partial class Source1GameMovement
{
	public virtual bool IsDucking => DuckTime > 0;
	public virtual float TimeToDuck => .2f;
	public virtual float DuckProgress => GetDuckProgress();

	public float DuckTime { get; set; }
	public float DuckTransitionEndTime { get; set; }
	public DuckStateTypes DuckState { get; set; }

	public enum DuckStateTypes
	{
		Unducked,
		Ducking,
		Ducked,
		Unducking
	}

	public virtual bool WishDuck() => Input.Down( InputButton.Duck );

	public virtual float GetDuckProgress()
	{
		var duckEnd = DuckTransitionEndTime;
		var duckStart = duckEnd - TimeToDuck;

		var duckElapsed = Time.Now - duckStart;
		var fraction = Math.Clamp( duckElapsed / TimeToDuck, 0, 1 );

		// if unducking, revert
		if ( DuckState == DuckStateTypes.Unducking || DuckState == DuckStateTypes.Unducked )
			fraction = 1 - fraction;

		return fraction;
	}

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

		if ( Player.IsDucked )
			SetTag( "ducked" );
	}

	public virtual void OnDucking()
	{
		if ( DuckState == DuckStateTypes.Ducked )
			return;

		if ( DuckState != DuckStateTypes.Ducking )
			StartDucking();

		if ( Time.Now > DuckTransitionEndTime || IsInAir )
			FinishDucking();
	}

	public virtual void OnUnducking()
	{
		if ( DuckState == DuckStateTypes.Unducked )
			return;

		if ( DuckState != DuckStateTypes.Unducking )
			StartUnducking();

		if ( Time.Now > DuckTransitionEndTime || IsInAir )
			FinishUnducking();
	}

	public virtual void StartDucking()
	{
		Log.Info( "Start Ducking" );

		var duckTime = TimeToDuck;
		if ( DuckState == DuckStateTypes.Unducking )
		{
			var duckStart = DuckTransitionEndTime - TimeToDuck;
			duckTime = Time.Now - duckStart;
		}

		DuckTransitionEndTime = Time.Now + duckTime;
		DuckState = DuckStateTypes.Ducking;
	}

	public virtual void StartUnducking()
	{
		Log.Info( "Start Unducking" );

		var duckTime = TimeToDuck;
		if ( DuckState == DuckStateTypes.Ducking )
		{
			var duckStart = DuckTransitionEndTime - TimeToDuck;
			duckTime = Time.Now - duckStart;
		}

		DuckTransitionEndTime = Time.Now + duckTime;
		DuckState = DuckStateTypes.Unducking;
	}

	public virtual void FinishDucking()
	{
		if ( DuckState == DuckStateTypes.Ducked )
			return;

		Log.Info( "Finished Ducking" );
		DuckState = DuckStateTypes.Ducked;
		Player.IsDucked = true;
		DuckTransitionEndTime = -1;

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

	public virtual void FinishUnducking()
	{
		Log.Info( "Finish Unducking" );
		DuckState = DuckStateTypes.Unducked;
		DuckTransitionEndTime = -1;
		Player.IsDucked = false;

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

	public virtual void OnDuckingOld()
	{
		if ( !CanDuck() )
			return;

		DuckTime = DuckTime.Approach( TimeToDuck, Time.Delta );

		if ( Player.IsDucked )
			return;

		if ( DuckTime >= TimeToDuck || IsInAir )
			FinishDuck();
	}

	public virtual void OnUnduckingOld()
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
		if ( DuckState == DuckStateTypes.Ducked ) 
			return;

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
