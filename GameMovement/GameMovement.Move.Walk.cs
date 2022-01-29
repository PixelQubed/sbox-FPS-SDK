using Sandbox;
using System;

namespace Source1
{
	public partial class Source1GameMovement
	{
		public virtual void FullWalkMove()
		{
			// if ( !CheckWater() ) 
			{
				StartGravity();
			}

			// If we are leaping out of the water, just update the counters.
			// if ( player->m_flWaterJumpTime )
			// {
			//		WaterJump();
			//		TryPlayerMove();
			//		See if we are still in water?
			//		CheckWater();
			//		return;
			// }


			// If we are swimming in the water, see if we are nudging against a place we can jump up out
			//  of, and, if so, start out jump.  Otherwise, if we are not moving up, then reset jump timer to 0
			/*if ( Pawn.WaterLevel.Fraction >= WL_Waist )
			{
				if ( player->GetWaterLevel() == WL_Waist )
				{
					CheckWaterJump();
				}

				// If we are falling again, then we must not trying to jump out of water any more.
				if ( mv->m_vecVelocity[2] < 0 &&
					 player->m_flWaterJumpTime )
				{
					player->m_flWaterJumpTime = 0;
				}

				// Was jump button pressed?
				if ( mv->m_nButtons & IN_JUMP )
				{
					CheckJumpButton();
				}
				else
				{
					mv->m_nOldButtons &= ~IN_JUMP;
				}

				// Perform regular water movement
				WaterMove();

				// Redetermine position vars
				CategorizePosition();

				// If we are on ground, no downward velocity.
				if ( player->GetGroundEntity() != NULL )
				{
					mv->m_vecVelocity[2] = 0;
				}
			}
			else
			// Not fully underwater*/
			{
				// Was jump button pressed?
				if ( WishJump() ) Jump();

				// Fricion is handled before we add in any base velocity. That way, if we are on a conveyor, 
				// we don't slow when standing still, relative to the conveyor.
				if ( IsGrounded() )
				{
					Velocity = Velocity.WithZ( 0 );
					Friction();
				}

				// Make sure velocity is valid.
				CheckVelocity();

				if ( IsGrounded() ) WalkMove();
				else AirMove();

				// Set final flags.
				CategorizePosition();

				// Make sure velocity is valid.
				CheckVelocity();

				// Add any remaining gravitational component.
				// if ( !CheckWater() )
				{
					FinishGravity();
				}

				// If we are on ground, no downward velocity.
				if ( IsGrounded() )
				{
					Velocity = Velocity.WithZ( 0 );
				}

				CheckFalling();
			}

		}

		public virtual float PlayerFatalFallSpeed => 1024;
		public virtual float PlayerMaxSafeFallSpeed => 580;
		public virtual float PLAYER_LAND_ON_FLOATING_OBJECT => 200;
		public virtual float PLAYER_MIN_BOUNCE_SPEED => 200;
		public virtual float PLAYER_FALL_PUNCH_THRESHOLD => 350;
		public virtual float DAMAGE_FOR_FALL_SPEED => 100.0f / (PlayerFatalFallSpeed - PlayerMaxSafeFallSpeed);

		public void CheckFalling()
		{
			// this function really deals with landing, not falling, so early out otherwise
			if ( !IsGrounded() || FallVelocity <= 0 )
				return;

			if ( !IsDead() && FallVelocity >= PLAYER_FALL_PUNCH_THRESHOLD )
			{
				float fvol = 0.5f;

				if ( Pawn.WaterLevel.Fraction > 0 ) 
				{
					// They landed in water.
				}
				else
				{
					// Scale it down if we landed on something that's floating...
					/*if ( player->GetGroundEntity()->IsFloating() )
					{
						player->m_Local.m_flFallVelocity -= PLAYER_LAND_ON_FLOATING_OBJECT;
					}*/

					//
					// They hit the ground.
					//
					if ( GroundEntity.Velocity.z < 0.0f )
					{
						// Player landed on a descending object. Subtract the velocity of the ground entity.
						FallVelocity += GroundEntity.Velocity.z;
						FallVelocity = MathF.Max( 0.1f, FallVelocity );
					}

					if ( FallVelocity > PlayerMaxSafeFallSpeed )
					{
						//
						// If they hit the ground going this fast they may take damage (and die).
						//
						// bAlive = MoveHelper()->PlayerFallingDamage();
						fvol = 1f;
					}
					else if ( FallVelocity > PlayerMaxSafeFallSpeed / 2 )
					{
						fvol = 0.85f;
					}
					else if ( FallVelocity < PLAYER_MIN_BOUNCE_SPEED )
					{
						fvol = 0;
					}
				}

				PlayerRoughLandingEffects( fvol );
			}

			// let any subclasses know that the player has landed and how hard
			OnLand( FallVelocity );

			//
			// Clear the fall velocity so the impact doesn't happen again.
			//
			FallVelocity = 0;
		}

		public virtual void PlayerRoughLandingEffects( float fvol )
		{
			Log.Info( $"landing effects vol: {fvol}" );
			if ( fvol > 0.0 )
			{
				//
				// Play landing sound right away.
				// player->m_flStepSoundTime = 400;

				// Play step sound for current texture.
				// player->PlayStepSound( (Vector &)mv->GetAbsOrigin(), player->m_pSurfaceData, fvol, true );

				//
				// Knock the screen around a little bit, temporary effect.
				//
				/*player->m_Local.m_vecPunchAngle.Set( ROLL, player->m_Local.m_flFallVelocity * 0.013 );

				if ( player->m_Local.m_vecPunchAngle[PITCH] > 8 )
				{
					player->m_Local.m_vecPunchAngle.Set( PITCH, 8 );
				}

#if !defined( CLIENT_DLL )
				player->RumbleEffect( (fvol > 0.85f) ? (RUMBLE_FALL_LONG) : (RUMBLE_FALL_SHORT), 0, RUMBLE_FLAGS_NONE );
#endif*/
			}
		}

		public virtual void OnLand( float velocity )
		{

		}

		public virtual void WalkMove()
		{
			var oldGround = GroundEntity;

			WishVelocity = new Vector3( Input.Forward, Input.Left, 0 );
			var inSpeed = WishVelocity.Length.Clamp( 0, 1 );
			WishVelocity *= Input.Rotation.Angles().WithPitch( 0 ).ToRotation();

			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.Normal * inSpeed;
			WishVelocity *= GetWishSpeed();

			var wishspeed = WishVelocity.Length;
			var wishdir = WishVelocity.Normal;

			if ( wishspeed != 0 && wishspeed > MaxSpeed )
			{
				WishVelocity *= MaxSpeed / wishspeed;
				wishspeed = MaxSpeed;
			}

			Velocity = Velocity.WithZ( 0 );
			Accelerate( wishdir, wishspeed, sv_accelerate );
			Velocity = Velocity.WithZ( 0 );

			Velocity += BaseVelocity;

			if ( Velocity.Length < 1 )
			{
				Velocity = 0;
				Velocity -= BaseVelocity;
				return;
			}

			var dest = (Position + Velocity * Time.Delta).WithZ( Position.z );
			var pm = TraceBBox( Position, dest );

			if ( pm.Fraction == 1 )
			{
				Position = pm.EndPos;
				Velocity -= BaseVelocity;
				StayOnGround();
				return;
			}

			if ( oldGround == null && Pawn.WaterLevel.Fraction == 0 )
			{
				Velocity -= BaseVelocity;
				return;
			}

			/*// If we are jumping out of water, don't do anything more.
			if ( player->m_flWaterJumpTime )
			{
				Velocity -= BaseVelocity;
				return;
			}*/

			StepMove( dest );
			Velocity -= BaseVelocity;

			StayOnGround();
		}
	}
}
