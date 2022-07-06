using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.Source1;

partial class GameMovement
{
	public virtual void HandleDuckingSpeedCrop()
	{
		if ( Player.IsDucked && Player.IsGrounded ) 
		{
			float frac = Player.DuckingSpeedMultiplier;
			Move.ForwardMove *= frac;
			Move.SideMove *= frac;
			Move.UpMove *= frac;
		}
	}

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

	public virtual bool CanDuck()
	{
		if ( Player.IsInAir )
		{
			if ( Player.AirDuckCount >= MaxAirDucks )
				return false;
		}

		return true;
	}

	public virtual void OnDucking()
	{
		if ( !CanDuck() )
			return;

		Player.DuckTime = Player.DuckTime.Approach( TimeToDuck, Time.Delta );

		if ( Player.IsDucked )
			return;

		if ( Player.DuckTime >= TimeToDuck || Player.IsInAir )
			FinishDuck();
	}

	public virtual void OnUnducking()
	{
		if ( !CanUnduck() )
			return;

		Player.DuckTime = Player.DuckTime.Approach( 0, Time.Delta );

		if ( !Player.IsDucked )
			return;

		if ( Player.DuckTime <= 0 || Player.IsInAir )
			FinishUnDuck();
	}

	public virtual bool CanUnduck()
	{
		var newOrigin = Move.Position;

		if ( Player.GroundEntity.IsValid() )
		{
			newOrigin += GetPlayerMins( true ) - GetPlayerMins( false );
		}
		else
		{
			// If in air an letting go of crouch, make sure we can offset origin to make
			//  up for uncrouching
			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;
			viewDelta *= -1;
			newOrigin += viewDelta;
		}

		bool saveducked = Player.IsDucked;
		Player.IsDucked = false;

		var trace = TraceBBox( Move.Position, newOrigin );
		Player.IsDucked = saveducked;

		if ( trace.StartedSolid || (trace.Fraction != 1.0f) )
			return false;

		return true;
	}

	protected virtual void SetDuckedEyeOffset( float duckFraction )
	{
		duckFraction = Util.SimpleSpline( duckFraction );

		var vDuckHullMin = GetPlayerMins( true );
		var vStandHullMin = GetPlayerMins( false );

		float fMore = (vDuckHullMin.z - vStandHullMin.z);

		var vecDuckViewOffset = GetPlayerViewOffset( true );
		var vecStandViewOffset = GetPlayerViewOffset( false );
		var temp = Player.EyeLocalPosition;

		temp.z = ((vecDuckViewOffset.z - fMore) * duckFraction) +
					(vecStandViewOffset.z * (1 - duckFraction));

		Player.EyeLocalPosition = temp;
	}

	public virtual void FinishDuck()
	{
		if ( Player.IsDucked )
			return;

		Player.IsDucked = true;
		Player.DuckTime = TimeToDuck;

		if ( Player.IsGrounded )
		{
			Move.Position -= GetPlayerMins( true ) - GetPlayerMins( false );
		}
		else
		{
			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;
			Move.Position += viewDelta;

			Player.AirDuckCount++;
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
		Player.DuckTime = 0;

		if ( Player.IsGrounded )
		{
			Move.Position += GetPlayerMins( true ) - GetPlayerMins( false );
		}
		else
		{
			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;
			Move.Position -= viewDelta;
		}

		// Recategorize position since ducking can change origin
		CategorizePosition();
	}

	public virtual void FixPlayerCrouchStuck( bool upward )
	{
		int direction = upward ? 1 : 0;

		var trace = TraceBBox( Move.Position, Move.Position );
		if ( trace.Entity == null )
			return;

		var test = Move.Position;
		for ( int i = 0; i < 36; i++ )
		{
			var org = Move.Position;
			org.z += direction;

			Move.Position = org;
			trace = TraceBBox( Move.Position, Move.Position );
			if ( trace.Entity == null )
				return;
		}

		Move.Position = test;
	}

}
