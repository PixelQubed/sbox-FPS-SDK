using Sandbox;
using System;

namespace Source1
{
	partial class S1GameMovement
	{
		/// <summary>
		/// We are currently in process of ducking.
		/// </summary>
		bool IsDucking { get; set; }
		/// <summary>
		/// We are currently fully ducked.
		/// </summary>
		bool IsDucked { get; set; }
		TimeSince TimeSinceDucked { get; set; }
		TimeSince TimeSinceUnducked { get; set; }
		int AirDucksCount { get; set; }

		public virtual float GetDuckSpeed()
		{
			return 90;
		}

		public virtual void DuckSimulate()
		{
			if ( IsDead() )
				return;

			// If we are pressing duck button
			if ( Input.Down( InputButton.Duck ) )
			{
				// Try to duck.
				TryDuck();
			}
			else if ( IsDucked || IsDucking ) 
			{
				TryUnduck();
			}

			if ( IsDucked ) SetTag( "ducked" );
		}

		const float TIME_TO_DUCK = 0.15f;
		const float GAMEMOVEMENT_DUCK_TIME = 1;

		public virtual void TryDuck()
		{
			// If we just pressed the button, and we're not already fully ducked.
			if ( Input.Pressed( InputButton.Duck ) )
			{
				// Start ducking.
				TimeSinceDucked = 0;
				IsDucking = true;
			}

			// We are currently in the ducking transition.
			if ( IsDucking )
			{
				// If we've waited enough time, we are in air, then finish ducking immediately.
				if ( TimeSinceDucked > TIME_TO_DUCK || InAir() )
				{
					FinishDuck();
				} else
				{
					// Otherwise update our eye position.
					float flDuckFraction = Easing.EaseInOut( TimeSinceDucked / TIME_TO_DUCK );
					SetDuckedEyeOffset( flDuckFraction );
				}
			}
		}

		public virtual void TryUnduck()
		{
			Log.Info( "TryUnduck()" );
			if ( Input.Released( InputButton.Duck ) )
			{
				TimeSinceDucked = 0;
				IsDucking = true;
			}

			// We are currently in the ducking transition.
			if ( IsDucking )
			{
				if ( CanUnduck() ) 
				{
					// If we've waited enough time, we are in air, then finish ducking immediately.
					if ( TimeSinceDucked > TIME_TO_DUCK || InAir() )
					{
						FinishUnduck();
					}
					else
					{
						// Otherwise update our eye position.
						float flDuckFraction = 1 - Easing.EaseInOut( TimeSinceDucked / TIME_TO_DUCK );
						SetDuckedEyeOffset( flDuckFraction );
					}
				} else
				{
					SetDuckedEyeOffset( 1.0f );
					TimeSinceDucked = 0;
				}
			}
		}

		public virtual bool CanUnduck()
		{
			var newOrigin = Position;

			if ( !InAir() ) 
			{
				newOrigin += GetPlayerMins( true ) - GetPlayerMins( false );
			}
			else
			{
				// If in air an letting go of crouch, make sure we can offset origin to make
				//  up for uncrouching
				var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
				var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( false );
				var viewDelta = hullSizeNormal - hullSizeCrouch;
				newOrigin -= viewDelta;
			}

			bool saveducked = IsDucked;
			IsDucked = false;
			var tr = TraceBBox( Position, newOrigin );
			IsDucked = saveducked;

			if ( tr.StartedSolid || tr.Fraction != 1.0f ) 
				return false;

			return true;
		}

		public virtual void FinishDuck()
		{
			Log.Info( "FinishDuck()" );
			IsDucking = false;

			// If we are already ducked, dont do anything.
			if ( IsDucked ) return;
			IsDucked = true;

			EyePosLocal = GetPlayerViewOffset( true );
			var newOrigin = Position;

			if ( !InAir() )
			{
				// Wtf does this do
				newOrigin -= GetPlayerMins( true ) - GetPlayerMins( false );
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

		public virtual void FinishUnduck()
		{
			Log.Info( "FinishUnduck()" );
			var newOrigin = Position;

			if ( !InAir() )
			{
				newOrigin += GetPlayerMins( true ) - GetPlayerMins( false );
			}
			else
			{
				var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
				var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( false );
				var viewDelta = hullSizeNormal - hullSizeCrouch;
				newOrigin -= viewDelta;
			}

			IsDucking = false;
			IsDucked = false;
			TimeSinceDucked = 0;

			EyePosLocal = GetPlayerViewOffset( false );

			Position = newOrigin;

			// See if we are stuck?
			FixPlayerCrouchStuck( true );
			CategorizePosition();
		}

		//-----------------------------------------------------------------------------
		// Purpose: Determine if crouch/uncrouch caused player to get stuck in world
		// Input  : direction - 
		//-----------------------------------------------------------------------------
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
	}
}
