using Sandbox;
using System;

namespace Source1
{
	partial class S1GameMovement
	{
		[ConVar.Replicated] public static bool sv_debug_duck { get; set; }

		public bool IsDucking { get; set; }
		public bool IsDucked { get; set; }
		public float DuckTime { get; set; }
		public float DuckJumpTime { get; set; }
		public float JumpTime { get; set; }
		public bool InDuckJump { get; set; }

		public virtual float GetDuckSpeed()
		{
			return 90;
		}

		public virtual void Duck()
		{
			// Check to see if we are in the air.
			bool bInAir = !IsGrounded();
			bool bInDuck = Pawn.Tags.Has( PlayerTags.Ducked );
			bool bDuckJump = JumpTime > 0.0f;
			bool bDuckJumpTime = DuckJumpTime > 0.0f;

			// Handle death.
			if ( IsDead() )
				return;

			// If the player is holding down the duck button, the player is in duck transition, ducking, or duck-jumping.
			if ( Input.Down( InputButton.Duck ) || IsDucking || bInDuck || bDuckJump )
			{
				// DUCK
				if ( Input.Down( InputButton.Duck ) || bDuckJump )
				{
					// Have the duck button pressed, but the player currently isn't in the duck position.
					if ( Input.Pressed( InputButton.Duck ) && !bInDuck && !bDuckJump && !bDuckJumpTime )
					{
						DuckTime = GAMEMOVEMENT_DUCK_TIME;
						IsDucking = true;
					}

					// The player is in duck transition and not duck-jumping.
					if ( IsDucking && !bDuckJump && !bDuckJumpTime )
					{
						float flDuckMilliseconds = MathF.Max( 0.0f, GAMEMOVEMENT_DUCK_TIME - DuckTime );
						float flDuckSeconds = flDuckMilliseconds * 0.001f;

						// Finish in duck transition when transition time is over, in "duck", in air.
						if ( (flDuckSeconds > TIME_TO_DUCK) || bInDuck || bInAir )
						{
							FinishDuck();
						}
						else
						{
							// Calc parametric time
							float flDuckFraction = Easing.QuadraticInOut( flDuckSeconds / TIME_TO_DUCK );
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
								TraceResult trace;
								if ( CanUnDuckJump( out trace ) )
								{
									FinishUnDuckJump( trace );
									DuckJumpTime = (GAMEMOVEMENT_TIME_TO_UNDUCK * (1.0f - trace.Fraction)) + GAMEMOVEMENT_TIME_TO_UNDUCK_INV;
								}
							}
						}
					}
				}
				// UNDUCK (or attempt to...)
				else
				{
					if ( InDuckJump )
					{
						// Check for a crouch override.
						if ( !Input.Down( InputButton.Duck ) )
						{
							TraceResult trace;
							if ( CanUnDuckJump( out trace ) )
							{
								FinishUnDuckJump( trace );

								if ( trace.Fraction < 1.0f )
								{
									DuckJumpTime = (GAMEMOVEMENT_TIME_TO_UNDUCK * (1.0f - trace.Fraction)) + GAMEMOVEMENT_TIME_TO_UNDUCK_INV;
								}
							}
						}
						else
						{
							InDuckJump = false;
						}
					}

					if ( bDuckJumpTime )
						return;

					// Try to unduck unless automovement is not allowed
					// NOTE: When not onground, you can always unduck
					if ( Player.AllowAutoMovement || bInAir || IsDucking )
					{
						// We released the duck button, we aren't in "duck" and we are not in the air - start unduck transition.
						if ( Input.Released( InputButton.Duck ) )
						{
							if ( bInDuck && !bDuckJump )
							{
								DuckTime = GAMEMOVEMENT_DUCK_TIME;
							}
							else if ( IsDucking && !IsDucked )
							{
								// Invert time if release before fully ducked!!!
								float unduckMilliseconds = 1000.0f * TIME_TO_UNDUCK;
								float duckMilliseconds = 1000.0f * TIME_TO_DUCK;
								float elapsedMilliseconds = GAMEMOVEMENT_DUCK_TIME - DuckTime;

								float fracDucked = elapsedMilliseconds / duckMilliseconds;
								float remainingUnduckMilliseconds = fracDucked * unduckMilliseconds;

								DuckTime = GAMEMOVEMENT_DUCK_TIME - unduckMilliseconds + remainingUnduckMilliseconds;
							}
						}


						// Check to see if we are capable of unducking.
						if ( CanUnduck() )
						{
							// or unducking
							if ( (IsDucking || IsDucked) )
							{
								float flDuckMilliseconds = Math.Max( 0.0f, GAMEMOVEMENT_DUCK_TIME - DuckTime );
								float flDuckSeconds = flDuckMilliseconds * 0.001f;

								// Finish ducking immediately if duck time is over or not on ground
								if ( flDuckSeconds > TIME_TO_UNDUCK || (bInAir && !bDuckJump) )
								{
									FinishUnDuck();
								}
								else
								{
									// Calc parametric time
									float flDuckFraction = Easing.QuadraticInOut( 1.0f - (flDuckSeconds / TIME_TO_UNDUCK) );
									SetDuckedEyeOffset( flDuckFraction );
									IsDucking = true;
								}
							}
						}
						else
						{
							// Still under something where we can't unduck, so make sure we reset this timer so
							//  that we'll unduck once we exit the tunnel, etc.
							if ( DuckTime != GAMEMOVEMENT_DUCK_TIME )
							{
								SetDuckedEyeOffset( 1.0f );
								DuckTime = GAMEMOVEMENT_DUCK_TIME;
								IsDucked = true;
								IsDucking = false;
								Pawn.Tags.Add( PlayerTags.Ducked );
							}
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
			else if ( !IsDead() /*&& !player->IsObserver() */ )
			{
				if ( (DuckJumpTime == 0.0f) && (MathF.Abs( EyePosLocal.z - GetPlayerViewOffset( false ).z ) > 0.1f) ) 
				{
					// set the eye height to the non-ducked height
					SetDuckedEyeOffset( 0.0f );
				}
			}

			if ( Pawn.Tags.Has( PlayerTags.Ducked ) ) SetTag( PlayerTags.Ducked );

			if ( sv_debug_duck && Host.IsServer ) 
			{
				DebugOverlay.ScreenText( 0, $"PlayerFlags.Ducked  {Pawn.Tags.Has( PlayerTags.Ducked )}" );
				DebugOverlay.ScreenText( 1, $"m_bDucking          {IsDucking}" );
				DebugOverlay.ScreenText( 2, $"m_bDucked           {IsDucked}" );
				DebugOverlay.ScreenText( 3, $"m_flDucktime        {DuckTime}" );
				DebugOverlay.ScreenText( 4, $"m_flDuckJumpTime    {DuckJumpTime}" );
				DebugOverlay.ScreenText( 5, $"m_flJumpTime        {JumpTime}" );
				DebugOverlay.ScreenText( 6, $"m_bInDuckJump       {InDuckJump}" );
				DebugOverlay.ScreenText( 7, $"m_bAllowAutoMovement {Player.AllowAutoMovement}" );
				DebugOverlay.ScreenText( 8, $"Speed:              {Pawn.Velocity.Length}HU" );
			}
		}

		public virtual float TIME_TO_DUCK => 0.2f;
		public virtual float TIME_TO_UNDUCK => 0.2f;
		public virtual float GAMEMOVEMENT_DUCK_TIME => 1000;
		public virtual float GAMEMOVEMENT_TIME_TO_UNDUCK => TIME_TO_UNDUCK * 1000;
		public virtual float GAMEMOVEMENT_TIME_TO_UNDUCK_INV => GAMEMOVEMENT_DUCK_TIME - GAMEMOVEMENT_TIME_TO_UNDUCK;


		//-----------------------------------------------------------------------------
		// Purpose:
		//-----------------------------------------------------------------------------
		void StartUnDuckJump()
		{
			Pawn.Tags.Add( PlayerTags.Ducked );
			IsDucked = true;
			IsDucking = false;

			EyePosLocal = GetPlayerViewOffset( true );

			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;
			Position += viewDelta;

			// See if we are stuck?
			FixPlayerCrouchStuck( true );

			// Recategorize position since ducking can change origin
			CategorizePosition();
		}

		void FixPlayerCrouchStuck( bool upward )
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

		bool CanUnDuckJump( out TraceResult trace )
		{
			var vecEnd = Position;

			// Trace down to the stand position and see if we can stand.
			vecEnd.z -= 36.0f;

			trace = TraceBBox( Position, vecEnd );
			if ( trace.Fraction < 1.0f )
			{
				// Find the endpoint.
				vecEnd.z = Position.z + (-36.0f * trace.Fraction);

				// Test a normal hull.
				bool bWasDucked = IsDucked;
				IsDucked = false;

				var traceUp = TraceBBox( vecEnd, vecEnd );
				IsDucked = bWasDucked;
				if ( !traceUp.StartedSolid )
					return true;
			}

			return false;
		}

		void FinishDuck()
		{
			if ( Pawn.Tags.Has( PlayerTags.Ducked ) ) 
				return;

			Pawn.Tags.Add( PlayerTags.Ducked );
			IsDucked = true;
			IsDucking = false;

			EyePosLocal = GetPlayerViewOffset( true );

			// HACKHACK - Fudge for collision bug - no time to fix this properly
			if ( IsGrounded() )
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

			// Recategorize position since ducking can change origin
			CategorizePosition();
		}

		void FinishUnDuckJump( TraceResult trace )
		{
			//  Up for uncrouching.
			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;

			float flDeltaZ = viewDelta.z;
			viewDelta.z *= trace.Fraction;
			flDeltaZ -= viewDelta.z;

			Pawn.Tags.Remove( PlayerTags.Ducked );
			IsDucked = false;
			IsDucking = false;
			InDuckJump = false;
			DuckTime = 0.0f;
			DuckJumpTime = 0.0f;
			JumpTime = 0.0f;

			var vecViewOffset = GetPlayerViewOffset( false );
			vecViewOffset.z -= flDeltaZ;
			EyePosLocal = vecViewOffset;

			Position -= viewDelta;

			// Recategorize position since ducking can change origin
			CategorizePosition();
		}

		bool CanUnduck()
		{
			var newOrigin = Position;

			if ( IsGrounded() ) 
			{
				newOrigin += (GetPlayerMins( true ) - GetPlayerMins( false ));
			}
			else
			{
				// If in air an letting go of crouch, make sure we can offset origin to make
				//  up for uncrouching
				var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
				var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
				var viewDelta = hullSizeNormal - hullSizeCrouch;
				newOrigin -= viewDelta;
			}

			bool saveducked = IsDucked;
			IsDucked = false;

			var trace = TraceBBox( Position, newOrigin );
			IsDucked = saveducked;
			if ( trace.StartedSolid || (trace.Fraction != 1.0f) ) 
				return false;

			return true;
		}

		void FinishUnDuck()
		{
			var newOrigin = Position;

			if ( IsGrounded() )
			{
				newOrigin += (GetPlayerMins( true ) - GetPlayerMins( false ));
			}
			else
			{
				// If in air an letting go of crouch, make sure we can offset origin to make
				//  up for uncrouching
				var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
				var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
				var viewDelta = hullSizeNormal - hullSizeCrouch;
				newOrigin -= viewDelta;
			}

			Pawn.Tags.Remove( PlayerTags.Ducked );
			IsDucked = false;
			IsDucking = false;
			InDuckJump = false;
			EyePosLocal = GetPlayerViewOffset( false );
			DuckTime = 0;

			Position = newOrigin;

			// Recategorize position since ducking can change origin
			CategorizePosition();
		}
	}
}
