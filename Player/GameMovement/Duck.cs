using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.Source1;

partial class GameMovement
{
	void HandleDuckingSpeedCrop()
	{
		//if ( !(m_iSpeedCropped & SPEED_CROPPED_DUCK) && (player->GetFlags() & FL_DUCKING) && (player->GetGroundEntity() != NULL) )
		if ( Player.Flags.HasFlag( PlayerFlags.FL_DUCKING ) && Player.GroundEntity.IsValid() ) 
		{
			float frac = 0.33333333f;
			Move.ForwardMove *= frac;
			Move.SideMove *= frac;
			Move.UpMove *= frac;
			// m_iSpeedCropped |= SPEED_CROPPED_DUCK;
		}
	}

	public virtual void SimulateDucking()
	{
		// Check to see if we are in the air.
		bool bInAir = !Player.GroundEntity.IsValid();
		bool bInDuck = Player.Flags.HasFlag( PlayerFlags.FL_DUCKING );
		bool bDuckJump = Player.m_nJumpTimeMsecs > 0;
		bool bDuckJumpTime = Player.m_nDuckJumpTimeMsecs > 0;

		// Handle death.
		if ( IsDead )
			return;

		HandleDuckingSpeedCrop();

		// If the player is holding down the duck button, the player is in duck transition, ducking, or duck-jumping.
		if ( Input.Down( InputButton.Duck ) || Player.m_bDucking || bInDuck || bDuckJump ) 
		{
			// DUCK
			if ( Input.Down( InputButton.Duck ) || bDuckJump )
			{
				// Have the duck button pressed, but the player currently isn't in the duck position.
				if ( Input.Pressed( InputButton.Duck ) && !bInDuck && !bDuckJump && !bDuckJumpTime ) 
				{
					Player.m_nDuckTimeMsecs = GAMEMOVEMENT_DUCK_TIME;
					Player.m_bDucking = true;
				}

				// The player is in duck transition and not duck-jumping.
				if ( Player.m_bDucking && !bDuckJump && !bDuckJumpTime )
				{
					int nDuckMilliseconds = Math.Max( 0, GAMEMOVEMENT_DUCK_TIME - Player.m_nDuckTimeMsecs );

					// Finish in duck transition when transition time is over, in "duck", in air.
					if ( (nDuckMilliseconds > TIME_TO_DUCK_MSECS) || bInDuck || bInAir )
					{
						FinishDuck();
					}
					else
					{
						// Calc parametric time
						float flDuckFraction = Util.SimpleSpline( FractionDucked( nDuckMilliseconds ) );
						SetDuckedEyeOffset( flDuckFraction );
					}
				}

				if ( bDuckJump )
				{
					// Make the bounding box small immediately.
					if ( !bInDuck )
					{
						StartUnDuckJump();
					}
					else
					{
						// Check for a crouch override.
						if ( !Input.Down( InputButton.Duck ) ) 
						{
							if ( CanUnDuckJump( out var trace ) )
							{
								FinishUnDuckJump( trace );
								Player.m_nDuckJumpTimeMsecs = (int)((GAMEMOVEMENT_TIME_TO_UNDUCK_MSECS * (1 - trace.Fraction)) + GAMEMOVEMENT_TIME_TO_UNDUCK_MSECS_INV);
							}
						}
					}
				}
			}
			// UNDUCK (or attempt to...)
			else
			{
				if ( Player.m_bInDuckJump )
				{
					// Check for a crouch override.
					if ( !Input.Down( InputButton.Duck ) )
					{
						if ( CanUnDuckJump( out var trace ) )
						{
							FinishUnDuckJump( trace );

							if ( trace.Fraction < 1.0f )
							{
								Player.m_nDuckJumpTimeMsecs = (int)(((float)GAMEMOVEMENT_TIME_TO_UNDUCK_MSECS * (1.0f - trace.Fraction)) + (float)GAMEMOVEMENT_TIME_TO_UNDUCK_MSECS_INV);
							}
						}
					}
					else
					{
						Player.m_bInDuckJump = false;
					}
				}

				if ( bDuckJumpTime )
					return;

				// We released the duck button, we aren't in "duck" and we are not in the air - start unduck transition.
				if ( Input.Released( InputButton.Duck ) ) 
				{
					if ( bInDuck && !bDuckJump )
					{
						Player.m_nDuckTimeMsecs = GAMEMOVEMENT_DUCK_TIME;
					}
					else if ( Player.m_bDucking && !Player.m_bDucked )
					{
						// Invert time if release before fully ducked!!!
						int elapsedMilliseconds = GAMEMOVEMENT_DUCK_TIME - Player.m_nDuckTimeMsecs;

						float fracDucked = FractionDucked( elapsedMilliseconds );
						int remainingUnduckMilliseconds = (int)(fracDucked * TIME_TO_UNDUCK_MSECS);

						Player.m_nDuckTimeMsecs = GAMEMOVEMENT_DUCK_TIME - TIME_TO_UNDUCK_MSECS + remainingUnduckMilliseconds;
					}
				}


				// Check to see if we are capable of unducking.
				if ( CanUnduck() )
				{
					// or unducking
					if ( (Player.m_bDucking || Player.m_bDucked) )
					{
						int nDuckMilliseconds = Math.Max( 0, GAMEMOVEMENT_DUCK_TIME - Player.m_nDuckTimeMsecs );

						// Finish ducking immediately if duck time is over or not on ground
						if ( nDuckMilliseconds > TIME_TO_UNDUCK_MSECS || (bInAir && !bDuckJump) )
						{
							FinishUnDuck();
						}
						else
						{
							// Calc parametric time
							float flDuckFraction = Util.SimpleSpline( 1.0f - FractionUnDucked( nDuckMilliseconds ) );
							SetDuckedEyeOffset( flDuckFraction );
							Player.m_bDucking = true;
						}
					}
				}
				else
				{
					// Still under something where we can't unduck, so make sure we reset this timer so
					//  that we'll unduck once we exit the tunnel, etc.
					if ( Player.m_nDuckTimeMsecs != GAMEMOVEMENT_DUCK_TIME )
					{
						SetDuckedEyeOffset( 1.0f );
						Player.m_nDuckTimeMsecs = GAMEMOVEMENT_DUCK_TIME;
						Player.m_bDucked = true;
						Player.m_bDucking = false;
						Player.AddFlags( PlayerFlags.FL_DUCKING );
					}
				}
			}
		}
		// HACK: (jimd 5/25/2006) we have a reoccuring bug (#50063 in Tracker) where the player's
		// view height gets left at the ducked height while the player is standing, but we haven't
		// been  able to repro it to find the cause.  It may be fixed now due to a change I'm
		// also making in UpdateDuckJumpEyeOffset but just in case, this code will sense the 
		// problem and restore the eye to the proper position.  It doesn't smooth the transition,
		// but it is preferable to leaving the player's view too low.
		//
		// If the player is still alive and not an observer, check to make sure that
		// his view height is at the standing height.
		else if ( !IsDead && !Player.IsObserver /*&& !player->IsInAVehicle()*/ )
		{
			if ( Player.m_nDuckJumpTimeMsecs == 0 && MathF.Abs( Player.EyeLocalPosition.z - GetPlayerViewOffset( false ).z ) > 0.1 ) 
			{
				// we should rarely ever get here, so assert so a coder knows when it happens
				Log.Info( "Restoring player view height" );

				// set the eye height to the non-ducked height
				SetDuckedEyeOffset( 0.0f );
			}
		}
	}

	protected virtual float FractionDucked( int msecs )
	{
		return Math.Clamp( msecs / (float)TIME_TO_DUCK_MSECS, 0, 1 );
	}

	protected virtual float FractionUnDucked( int msecs )
	{
		return Math.Clamp( msecs / (float)TIME_TO_UNDUCK_MSECS, 0, 1 );
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
		if ( Player.Flags.HasFlag( PlayerFlags.FL_DUCKING ) ) 
			return;

		Player.AddFlags( PlayerFlags.FL_DUCKING );
		Player.m_bDucked = true;
		Player.m_bDucking = false;

		Player.EyeLocalPosition = GetPlayerViewOffset( true );

		// HACKHACK - Fudge for collision bug - no time to fix this properly
		if ( Player.GroundEntity != null ) 
		{
			Move.Position -= GetPlayerMins( true ) - GetPlayerMins( false );
		}
		else
		{
			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;
			Move.Position += viewDelta;
		}

		// See if we are stuck?
		FixPlayerCrouchStuck( true );

		// Recategorize position since ducking can change origin
		CategorizePosition();

		Log.Info( "::FinishDuck()" );
	}

	void FinishUnDuck( )
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

		Player.m_bDucked = false;
		Player.RemoveFlag( PlayerFlags.FL_DUCKING );
		Player.m_bDucking = false;
		Player.m_bInDuckJump = false;
		Player.EyeLocalPosition = GetPlayerViewOffset( false );
		Player.m_nDuckTimeMsecs = 0;

		Move.Position = newOrigin;

		// Recategorize position since ducking can change origin
		CategorizePosition();

		Log.Info( "::FinishUnDuck()" );
	}

	public virtual void StartUnDuckJump()
	{
		Player.AddFlags( PlayerFlags.FL_DUCKING );
		Player.m_bDucked = true;
		Player.m_bDucking = false;

		Player.EyeLocalPosition = GetPlayerViewOffset( true );

		var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
		var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
		var viewDelta = hullSizeNormal - hullSizeCrouch;
		Move.Position += viewDelta;

		// See if we are stuck?
		FixPlayerCrouchStuck( true );

		// Recategorize position since ducking can change origin
		CategorizePosition();
	}

	bool CanUnDuckJump( out TraceResult trace )
	{
		// Trace down to the stand position and see if we can stand.
		var vecEnd = Move.Position;
		vecEnd.z -= 36.0f; // This will have to change if bounding hull change!

		trace = TraceBBox( Move.Position, vecEnd );
		if ( trace.Fraction < 1.0f )
		{
			// Find the endpoint.
			vecEnd.z = Move.Position.z + (-36.0f * trace.Fraction);

			// Test a normal hull.
			bool bWasDucked = Player.m_bDucked;
			Player.m_bDucked = false;
			var traceUp = TraceBBox( vecEnd, vecEnd );
			Player.m_bDucked = bWasDucked;
			if ( !traceUp.StartedSolid )
				return true;
		}

		return false;
	}

	void FinishUnDuckJump( TraceResult trace )
	{
		var vecNewOrigin = Move.Position;

		//  Up for uncrouching.
		var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
		var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
		var viewDelta = hullSizeNormal - hullSizeCrouch;

		float flDeltaZ = viewDelta.z;
		viewDelta.z *= trace.Fraction;
		flDeltaZ -= viewDelta.z;

		Player.RemoveFlag( PlayerFlags.FL_DUCKING );
		Player.m_bDucked = false;
		Player.m_bDucking = false;
		Player.m_bInDuckJump = false;
		Player.m_nDuckTimeMsecs = 0;
		Player.m_nDuckJumpTimeMsecs = 0;
		Player.m_nJumpTimeMsecs = 0;

		var vecViewOffset = GetPlayerViewOffset( false );
		vecViewOffset.z -= flDeltaZ;
		Player.EyeLocalPosition = vecViewOffset;

		Move.Position = vecNewOrigin - viewDelta;

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

	bool CanUnduck()
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

		bool saveducked = Player.m_bDucked;
		Player.m_bDucked = false;

		var trace = TraceBBox( Move.Position, newOrigin );
		Player.m_bDucked = saveducked;

		if ( trace.StartedSolid || (trace.Fraction != 1.0f) ) 
			return false;

		return true;
	}
}
