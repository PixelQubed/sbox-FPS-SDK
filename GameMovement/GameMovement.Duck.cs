using Sandbox;
using System;

namespace Source1;

partial class Source1GameMovement
{
	[ConVar.Replicated] public static bool sv_debug_duck { get; set; }

	/// <summary>
	/// Is the player currently fully ducked? This is what defines whether we apply duck slow down or not.
	/// </summary>
	public bool IsDucked => Player.Tags.Has( PlayerTags.Ducked );

	public float TimeToDuck => .2f;
	public float DuckTime { get; set; }

	public float DuckProgress => DuckTime / TimeToDuck;

	public virtual float GetDuckSpeed()
	{
		return 90;
	}

	public virtual bool WishDuck()
	{
		return Input.Down( InputButton.Duck );
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

		if ( Pawn.Tags.Has( PlayerTags.Ducked ) )
			SetTag( PlayerTags.Ducked );
	}

	public virtual void OnDucking()
	{
		if ( !CanDuck() )
			return;

		if ( !IsDucked && DuckTime >= TimeToDuck || IsInAir ) 
		{
			OnFinishedDucking();
		}

		if ( DuckTime < TimeToDuck )
		{
			DuckTime += Time.Delta;

			if ( DuckTime > TimeToDuck ) 
				DuckTime = TimeToDuck;
		}
	}

	public virtual void OnUnducking()
	{
		if ( !CanUnduck() )
			return;

		if ( IsDucked && DuckTime == 0 || IsInAir )
		{
			OnFinishedUnducking();
		}

		if ( DuckTime > 0 )
		{
			DuckTime -= Time.Delta;

			if ( DuckTime < 0 )
				DuckTime = 0;
		}
	}

	public virtual void OnFinishedDucking()
	{
		if ( Pawn.Tags.Has( PlayerTags.Ducked ) )
			return;

		Pawn.Tags.Add( PlayerTags.Ducked );
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

	public virtual void OnFinishedUnducking()
	{
		if ( !Pawn.Tags.Has( PlayerTags.Ducked ) )
			return;

		Player.Tags.Remove( PlayerTags.Ducked );
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
}
