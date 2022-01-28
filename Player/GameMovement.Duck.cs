using Sandbox;
using System;

namespace Source1
{
	partial class S1GameMovement
	{
		[ConVar.Replicated] public static bool sv_debug_duck { get; set; }

		/// <summary>
		/// Is the player currently in the process of duck transition (ducking or unducking)
		/// </summary>
		public bool IsDucking { get; set; }
		/// <summary>
		/// Is the player currently fully ducked? This is what defines whether we apply duck slow down or not.
		/// </summary>
		public bool IsDucked { get; set; }
		public float DuckTime { get; set; }
		public float DuckJumpTime { get; set; }
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
						DuckTime = GameMovementDuckTime;
						IsDucking = true;
					}

					// The player is in duck transition and not duck-jumping.
					if ( IsDucking && !bDuckJump && !bDuckJumpTime )
					{
						float flDuckMilliseconds = MathF.Max( 0.0f, GameMovementDuckTime - DuckTime );
						float flDuckSeconds = flDuckMilliseconds * 0.001f;

						// Finish in duck transition when transition time is over, in "duck", in air.
						if ( (flDuckSeconds > TimeToDuck) || bInDuck || bInAir )
						{
							FinishDuck();
						}
						else
						{
							// Calc parametric time
							float flDuckFraction = Easing.QuadraticInOut( flDuckSeconds / TimeToDuck );
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
									DuckJumpTime = (GameMovementTimeToUnduck * (1.0f - trace.Fraction)) + GameMovementTickToUnduckInverse;
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
									DuckJumpTime = (GameMovementTimeToUnduck * (1.0f - trace.Fraction)) + GameMovementTickToUnduckInverse;
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
								DuckTime = GameMovementDuckTime;
							}
							else if ( IsDucking && !IsDucked )
							{
								// Invert time if release before fully ducked!!!
								float unduckMilliseconds = 1000.0f * TimeToUnduck;
								float duckMilliseconds = 1000.0f * TimeToDuck;
								float elapsedMilliseconds = GameMovementDuckTime - DuckTime;

								float fracDucked = elapsedMilliseconds / duckMilliseconds;
								float remainingUnduckMilliseconds = fracDucked * unduckMilliseconds;

								DuckTime = GameMovementDuckTime - unduckMilliseconds + remainingUnduckMilliseconds;
							}
						}

						// Check to see if we are capable of unducking.
						if ( CanUnduck() )
						{
							// or unducking
							if ( (IsDucking || IsDucked) )
							{
								float flDuckMilliseconds = Math.Max( 0.0f, GameMovementDuckTime - DuckTime );
								float flDuckSeconds = flDuckMilliseconds * 0.001f;

								// Finish ducking immediately if duck time is over or not on ground
								if ( flDuckSeconds > TimeToUnduck || (bInAir && !bDuckJump) )
								{
									FinishUnDuck();
								}
								else
								{
									// Calc parametric time
									float flDuckFraction = Easing.QuadraticInOut( 1.0f - (flDuckSeconds / TimeToUnduck) );
									SetDuckedEyeOffset( flDuckFraction );
									IsDucking = true;
								}
							}
						}
						else
						{
							// Still under something where we can't unduck, so make sure we reset this timer so
							// that we'll unduck once we exit the tunnel, etc.
							if ( DuckTime != GameMovementDuckTime )
							{
								SetDuckedEyeOffset( 1.0f );
								DuckTime = GameMovementDuckTime;
								IsDucked = true;
								IsDucking = false;
								Pawn.Tags.Add( PlayerTags.Ducked );
							}
						}
					}
				}
			}

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

			ShowDuckDebug();
		}

		protected void ShowDuckDebug()
		{
			if ( sv_debug_duck && Host.IsServer )
			{
				DebugOverlay.ScreenText( 0, $"PlayerFlags.Ducked  {Pawn.Tags.Has( PlayerTags.Ducked )}" );
				DebugOverlay.ScreenText( 1, $"IsDucking           {IsDucking}" );
				DebugOverlay.ScreenText( 2, $"IsDucked            {IsDucked}" );
				DebugOverlay.ScreenText( 3, $"DuckTime            {DuckTime}" );
				DebugOverlay.ScreenText( 4, $"DuckJumpTime        {DuckJumpTime}" );
				DebugOverlay.ScreenText( 5, $"JumpTime            {JumpTime}" );
				DebugOverlay.ScreenText( 6, $"InDuckJump          {InDuckJump}" );
				DebugOverlay.ScreenText( 7, $"AllowAutoMovement:  {Player.AllowAutoMovement}" );
				DebugOverlay.ScreenText( 8, $"Speed:              {Pawn.Velocity.Length}HU" );
			}
		}

		public virtual float TimeToDuck => 0.2f;
		public virtual float TimeToUnduck => 0.2f;
		public virtual float GameMovementDuckTime => 1000;
		public virtual float GameMovementTimeToUnduck => TimeToUnduck * 1000;
		public virtual float GameMovementTickToUnduckInverse => GameMovementDuckTime - GameMovementTimeToUnduck;


		//-----------------------------------------------------------------------------
		// Purpose:
		//-----------------------------------------------------------------------------
		public void StartUnDuckJump()
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

		public void FixPlayerCrouchStuck( bool upward )
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

		public bool CanUnDuckJump( out TraceResult trace )
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

		public void FinishUnDuckJump( TraceResult trace )
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

		public bool CanUnduck()
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

		public void FinishDuck()
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

		public void FinishUnDuck()
		{
			Pawn.Tags.Remove( PlayerTags.Ducked );

			IsDucked = false;
			IsDucking = false;
			InDuckJump = false;
			DuckTime = 0;

			EyePosLocal = GetPlayerViewOffset( false );

			if ( IsGrounded() )
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

		void UpdateDuckJumpEyeOffset()
		{
			if ( DuckJumpTime != 0.0f )
			{
				float flDuckMilliseconds = MathF.Max( 0.0f, DuckTime - DuckJumpTime );
				float flDuckSeconds = flDuckMilliseconds / GameMovementDuckTime;
				if ( flDuckSeconds > TimeToUnduck )
				{
					DuckJumpTime = 0.0f;
					SetDuckedEyeOffset( 0.0f );
				}
				else
				{
					float flDuckFraction = Easing.QuadraticInOut( 1.0f - (flDuckSeconds / TimeToUnduck) );
					SetDuckedEyeOffset( flDuckFraction );
				}
			}
		}
	}
}
