using Sandbox;
using System;

namespace Source1
{
	public partial class S1GameMovement : PawnController
	{
		S1Player Player { get; set; }
		protected float MaxSpeed { get; set; }
		protected float SurfaceFriction { get; set; }
		bool IsSwimming { get; set; }


		public override void FrameSimulate()
		{
			base.FrameSimulate();

			EyeRot = Input.Rotation;
		}

		public virtual void PawnChanged( S1Player player, S1Player prev )
		{

		}

		public override void Simulate()
		{
			if ( Player != Pawn )
			{
				var newPlayer = Pawn as S1Player;
				PawnChanged( newPlayer, Player );
				Player = newPlayer;
			}

			if ( Player == null ) return;

			PlayerMove();
		}


		public bool GameCodeMovedPlayer;

		protected float FallVelocity;

		public virtual void PlayerMove()
		{
			CheckParameters();

			// clear output applied velocity
			// mv->m_outWishVel.Init();
			// mv->m_outJumpVel.Init();

			// MoveHelper()->ResetTouchList();                    // Assume we don't touch anything

			ReduceTimers();

			EyeRot = Input.Rotation;

			if ( Pawn.MoveType != MoveType.MOVETYPE_NOCLIP &&
				Pawn.MoveType != MoveType.None &&
				Pawn.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				Pawn.MoveType != MoveType.MOVETYPE_OBSERVER &&
				!IsDead() ) 
			{
				/*
				if ( CheckInterval( STUCK ) )
				{
					if ( CheckStuck() )
					{
						// Can't move, we're stuck
						return;
					}
				}
				*/
			}

			// Now that we are "unstuck", see where we are (player->GetWaterLevel() and type, player->GetGroundEntity()).
			if ( Velocity.z > 250.0f )
			{
				ClearGroundEntity();
			}

			// Store off the starting water level
			// m_nOldWaterLevel = player->GetWaterLevel();

			// If we are not on ground, store off how fast we are moving down
			if ( !IsGrounded() )
			{
				FallVelocity = -Velocity.z;
			}

			// m_nOnLadder = 0;

			UpdateDuckJumpEyeOffset();
			Duck();

			/*// Don't run ladder code if dead or on a train
			if ( !IsDead() && !(player->GetFlags() & FL_ONTRAIN) )
			{
				// If was not on a ladder now, but was on one before, 
				//  get off of the ladder

				// TODO: this causes lots of weirdness.
				//bool bCheckLadder = CheckInterval( LADDER );
				//if ( bCheckLadder || player->GetMoveType() == MOVETYPE_LADDER )
				{
					if ( !LadderMove() &&
						(player->GetMoveType() == MOVETYPE_LADDER) )
					{
						// Clear ladder stuff unless player is dead or riding a train
						// It will be reset immediately again next frame if necessary
						player->SetMoveType( MOVETYPE_WALK );
						player->SetMoveCollide( MOVECOLLIDE_DEFAULT );
					}
				}
			}*/

			switch (Pawn.MoveType)
			{
				case MoveType.None:
					break;

				case MoveType.MOVETYPE_NOCLIP:
					// FullNoClipMove( sv_noclipspeed.GetFloat(), sv_noclipaccelerate.GetFloat() );
					break;

				case MoveType.MOVETYPE_FLY:
				case MoveType.MOVETYPE_FLYGRAVITY:
					// FullTossMove();
					break;

				case MoveType.MOVETYPE_LADDER:
					// FullLadderMove();
					break;

				case MoveType.MOVETYPE_WALK:
					FullWalkMove();
					break;

				case MoveType.MOVETYPE_ISOMETRIC:
					//IsometricMove();
					// Could also try:  FullTossMove();
					FullWalkMove();
					break;

				case MoveType.MOVETYPE_OBSERVER:
					// FullObserverMove(); // clips against world&players
					break;
			}
		}

		float SwimSoundTime;

		public virtual void ReduceTimers()
		{
			float frame_msec = 1000.0f * Time.Delta;

			if ( m_flDucktime > 0 )
			{
				m_flDucktime -= frame_msec;
				if ( m_flDucktime < 0 ) m_flDucktime = 0;
			}

			if ( m_flDuckJumpTime > 0 )
			{
				m_flDuckJumpTime -= frame_msec;
				if ( m_flDuckJumpTime < 0 ) m_flDuckJumpTime = 0;
			}

			if ( m_flJumpTime > 0 )
			{
				m_flJumpTime -= frame_msec;
				if ( m_flJumpTime < 0 ) m_flJumpTime = 0;
			}

			if ( SwimSoundTime > 0 )
			{
				SwimSoundTime -= frame_msec;
				if ( SwimSoundTime < 0 ) SwimSoundTime = 0;
			}
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
			var maxspeed = GetMaxSpeed();

			if ( wishspeed != 0 && wishspeed > maxspeed )
			{
				WishVelocity *= maxspeed / wishspeed;
				wishspeed = maxspeed;
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

		public virtual void StepMove( Vector3 dest )
		{
			MoveHelper mover = new MoveHelper( Position, Velocity );
			mover.Trace = mover.Trace.Size( GetPlayerMins(), GetPlayerMaxs() ).Ignore( Pawn );
			mover.MaxStandableAngle = sv_maxstandableangle;

			mover.TryMoveWithStep( Time.Delta, sv_stepsize );

			Position = mover.Position;
			Velocity = mover.Velocity;
		}

		public virtual void Move()
		{
			MoveHelper mover = new MoveHelper( Position, Velocity );
			mover.Trace = mover.Trace.Size( GetPlayerMins(), GetPlayerMaxs() ).Ignore( Pawn );
			mover.MaxStandableAngle = sv_maxstandableangle;

			mover.TryMove( Time.Delta );

			Position = mover.Position;
			Velocity = mover.Velocity;
		}

		public virtual bool CanAccelerate()
		{
			// Sure, why not.
			return true;
		}

		/// <summary>
		/// Add our wish direction and speed onto our velocity
		/// </summary>
		public virtual void Accelerate( Vector3 wishdir, float wishspeed, float acceleration, float speedLimit = 0 )
		{
			// This gets overridden because some games (CSPort) want to allow dead (observer) players
			// to be able to move around.
			 if ( !CanAccelerate() )
			     return;

			// Cap speed
			if ( speedLimit > 0 ) wishspeed = MathF.Min( wishspeed, speedLimit );

			// See if we are changing direction a bit
			var currentspeed = Velocity.Dot( wishdir );

			// Reduce wishspeed by the amount of veer.
			var addspeed = wishspeed - currentspeed;

			// DebugOverlay.ScreenText( $"{MathF.Floor( wishspeed )} - { MathF.Floor( currentspeed )} = {MathF.Floor( addspeed )}" );

			// If not going to add any speed, done.
			if ( addspeed <= 0 )
				return;

			// Determine amount of acceleration.
			var accelspeed = acceleration * wishspeed * Time.Delta;

			// Cap at addspeed
			if ( accelspeed > addspeed )
				accelspeed = addspeed;

			Velocity += wishdir * accelspeed;
			// DebugOverlay.Line( Position, Position + Velocity * 100, Color.Yellow, 0 );
		}

		/// <summary>
		/// Remove ground friction from velocity
		/// </summary>
		public virtual void Friction()
		{
			// If we are in water jump cycle, don't apply friction
			//if ( player->m_flWaterJumpTime )
			//   return;

			// Not on ground - no friction

			// Calculate speed
			var speed = Velocity.Length;
			if ( speed < 0.1f ) return;

			float friction, control, drop = 0;
			if ( !InAir() )
			{
				friction = sv_friction * SurfaceFriction;

				// Bleed off some speed, but if we have less than the bleed
				//  threshold, bleed the threshold amount.
				control = (speed < sv_stopspeed) ? sv_stopspeed : speed;

				// Add the amount to the drop amount.
				drop += control * friction * Time.Delta;
			}

			// scale the velocity
			float newspeed = speed - drop;
			if ( newspeed < 0 ) newspeed = 0;

			if ( newspeed != speed )
			{
				newspeed /= speed;
				Velocity *= newspeed;
			}

			// mv->m_outWishVel -= (1.f-newspeed) * mv->m_vecVelocity;
		}

		public virtual void AirMove()
		{
			WishVelocity = new Vector3( Input.Forward, Input.Left, 0 );
			var inSpeed = WishVelocity.Length.Clamp( 0, 1 );
			WishVelocity *= Input.Rotation.Angles().WithPitch( 0 ).ToRotation();

			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.Normal * inSpeed;
			WishVelocity *= GetWishSpeed();

			var wishspeed = WishVelocity.Length;
			var wishdir = WishVelocity.Normal;
			var maxspeed = GetMaxSpeed();

			if ( wishspeed != 0 && wishspeed > maxspeed )
			{
				WishVelocity *= maxspeed / wishspeed;
				wishspeed = maxspeed;
			}

			Accelerate( wishdir, wishspeed, sv_airaccelerate, sv_aircontrol );

			Velocity += BaseVelocity;
			Move();
			Velocity -= BaseVelocity;
		}

		bool IsTouchingLadder = false;
		Vector3 LadderNormal;

		public virtual void CheckLadder()
		{
			var wishvel = new Vector3( Input.Forward, Input.Left, 0 );
			wishvel *= Input.Rotation.Angles().WithPitch( 0 ).ToRotation();
			wishvel = wishvel.Normal;

			if ( IsTouchingLadder )
			{
				if ( Input.Pressed( InputButton.Jump ) )
				{
					Velocity = LadderNormal * 100.0f;
					IsTouchingLadder = false;

					return;

				}
				else if ( GroundEntity != null && LadderNormal.Dot( wishvel ) > 0 )
				{
					IsTouchingLadder = false;

					return;
				}
			}

			const float ladderDistance = 1.0f;
			var start = Position;
			Vector3 end = start + (IsTouchingLadder ? (LadderNormal * -1.0f) : wishvel) * ladderDistance;

			var pm = Trace.Ray( start, end )
						.Size( GetPlayerMins(), GetPlayerMaxs() )
						.HitLayer( CollisionLayer.All, false )
						.HitLayer( CollisionLayer.LADDER, true )
						.Ignore( Pawn )
						.Run();

			IsTouchingLadder = false;

			if ( pm.Hit && !(pm.Entity is ModelEntity me && me.CollisionGroup == CollisionGroup.Always) )
			{
				IsTouchingLadder = true;
				LadderNormal = pm.Normal;
			}
		}

		public virtual void LadderMove()
		{
			var velocity = WishVelocity;
			float normalDot = velocity.Dot( LadderNormal );
			var cross = LadderNormal * normalDot;
			Velocity = (velocity - cross) + (-normalDot * LadderNormal.Cross( Vector3.Up.Cross( LadderNormal ).Normal ));

			Move();
		}

		public virtual void CategorizePosition()
		{
			SurfaceFriction = 1.0f;

			// Doing this before we move may introduce a potential latency in water detection, but
			// doing it after can get us stuck on the bottom in water if the amount we move up
			// is less than the 1 pixel 'threshold' we're about to snap to.	Also, we'll call
			// this several times per frame, so we really need to avoid sticking to the bottom of
			// water on each call, and the converse case will correct itself if called twice.
			//CheckWater();

			var point = Position - Vector3.Up * 2;
			var vBumpOrigin = Position;

			//
			//  Shooting up really fast.  Definitely not on ground trimed until ladder shit
			//
			bool bMovingUpRapidly = Velocity.z > sv_maxnonjumpvelocity;
			bool bMovingUp = Velocity.z > 0;

			bool bMoveToEndPos = false;

			if ( GroundEntity != null ) // and not underwater
			{
				bMoveToEndPos = true;
				point.z -= sv_stepsize;
			}

			if ( bMovingUpRapidly || IsSwimming ) // or ladder and moving up
			{
				ClearGroundEntity();
				return;
			}

			var pm = TraceBBox( vBumpOrigin, point, 4.0f );

			if ( pm.Entity == null || Vector3.GetAngle( Vector3.Up, pm.Normal ) > sv_maxstandableangle )
			{
				ClearGroundEntity();
				bMoveToEndPos = false;

				if ( Velocity.z > 0 )
					SurfaceFriction = 0.25f;
			}
			else
			{
				UpdateGroundEntity( pm );
			}

			if ( bMoveToEndPos && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f )
			{
				Position = pm.EndPos;
			}

		}

		/// <summary>
		/// We have a new ground entity
		/// </summary>
		public virtual void UpdateGroundEntity( TraceResult tr )
		{
			GroundNormal = tr.Normal;

			// VALVE HACKHACK: Scale this to fudge the relationship between vphysics friction values and player friction values.
			// A value of 0.8f feels pretty normal for vphysics, whereas 1.0f is normal for players.
			// This scaling trivially makes them equivalent.  REVISIT if this affects low friction surfaces too much.
			SurfaceFriction = tr.Surface.Friction * 1.25f;
			if ( SurfaceFriction > 1 ) SurfaceFriction = 1;

			//if ( tr.Entity == GroundEntity ) return;

			Vector3 oldGroundVelocity = default;
			if ( GroundEntity != null ) oldGroundVelocity = GroundEntity.Velocity;

			bool wasOffGround = GroundEntity == null;

			GroundEntity = tr.Entity;

			if ( GroundEntity != null )
			{
				BaseVelocity = GroundEntity.Velocity;
			}

			/*
              	m_vecGroundUp = pm.m_vHitNormal;
	            player->m_surfaceProps = pm.m_pSurfaceProperties->GetNameHash();
	            player->m_pSurfaceData = pm.m_pSurfaceProperties;
	            const CPhysSurfaceProperties *pProp = pm.m_pSurfaceProperties;

	            const CGameSurfaceProperties *pGameProps = g_pPhysicsQuery->GetGameSurfaceproperties( pProp );
	            player->m_chTextureType = (int8)pGameProps->m_nLegacyGameMaterial;
            */
		}

		/// <summary>
		/// We're no longer on the ground, remove it
		/// </summary>
		public virtual void ClearGroundEntity()
		{
			if ( GroundEntity == null ) return;

			GroundEntity = null;
			GroundNormal = Vector3.Up;
			SurfaceFriction = 1.0f;
		}

		/// <summary>
		/// Try to keep a walking player on the ground when running down slopes etc
		/// </summary>
		public virtual void StayOnGround()
		{
			var start = Position + Vector3.Up * 2;
			var end = Position + Vector3.Down * sv_stepsize;

			// See how far up we can go without getting stuck
			var trace = TraceBBox( Position, start );
			start = trace.EndPos;

			// Now trace down from a known safe position
			trace = TraceBBox( start, end );

			if ( trace.Fraction <= 0 ) return;
			if ( trace.Fraction >= 1 ) return;
			if ( trace.StartedSolid ) return;
			if ( Vector3.GetAngle( Vector3.Up, trace.Normal ) > sv_maxstandableangle ) return;

			// This is incredibly hacky. The real problem is that trace returning that strange value we can't network over.
			// float flDelta = fabs( mv->GetAbsOrigin().z - trace.m_vEndPos.z );
			// if ( flDelta > 0.5f * DIST_EPSILON )

			Position = trace.EndPos;
		}

		public Entity TestPlayerPosition( Vector3 pos, ref TraceResult pm )
		{
			pm = TraceBBox( pos, pos );
			return pm.Entity;
		}

		Surface SurfaceData { get; set; }

		public virtual void CategorizeGroundSurface( TraceResult pm )
		{
			SurfaceData = pm.Surface;
			SurfaceFriction = pm.Surface.Friction;

			SurfaceFriction *= 1.25f;
			if ( SurfaceFriction > 1.0f )
				SurfaceFriction = 1.0f;
		}

		float ConstraintRadius { get; set; }
		float ConstraintSpeedFactor { get; set; }
		float ConstraintWidth { get; set; }
		Vector3 ConstraintCenter { get; set; }

		public virtual float ComputeConstraintSpeedFactor()
		{
			// If we have a constraint, slow down because of that too.
			if ( ConstraintRadius == 0.0f ) return 1.0f;

			float flDist = Position.Distance( ConstraintCenter );
			float flDistSq = flDist * flDist;

			float flOuterRadiusSq = ConstraintRadius * ConstraintRadius;
			float flInnerRadiusSq = ConstraintRadius - ConstraintWidth;
			flInnerRadiusSq *= flInnerRadiusSq;

			// Only slow us down if we're inside the constraint ring
			if ( (flDistSq <= flInnerRadiusSq) || (flDistSq >= flOuterRadiusSq) )
				return 1.0f;

			// Only slow us down if we're running away from the center
			Vector3 vecDesired = Input.Forward * Input.Rotation.Forward;
			vecDesired = VectorMA( vecDesired, Input.Left, Input.Rotation.Left);
			vecDesired = VectorMA( vecDesired, Input.Up, Input.Rotation.Up);

			Vector3 vecDelta = Position - ConstraintCenter;
			vecDelta = vecDelta.Normal;
			vecDesired = vecDesired.Normal;

			if ( Vector3.Dot( vecDelta, vecDesired ) < 0.0f )
				return 1.0f;

			float flFrac = (MathF.Sqrt( flDistSq ) - (ConstraintRadius - ConstraintWidth)) / ConstraintWidth;

			float flSpeedFactor = flFrac.LerpTo( 1.0f, ConstraintSpeedFactor );
			return flSpeedFactor;
		}

		bool IsDead()
		{
			return Pawn.LifeState != LifeState.Alive;
		}

		protected Vector3 VectorMA( Vector3 start, float scale, Vector3 direction )
		{
			return new Vector3(
				start.x + direction.x * scale,
				start.y + direction.y * scale,
				start.z + direction.z * scale
			);
		}

		//-----------------------------------------------------------------------------
		// Purpose: 
		//-----------------------------------------------------------------------------
		void CheckParameters()
		{
			/*
			Rotation v_angle;

			if ( Pawn.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				 Pawn.MoveType != MoveType.MOVETYPE_NOCLIP &&
				 Pawn.MoveType != MoveType.MOVETYPE_OBSERVER )
			{
				float spd;
				float maxspeed;

				spd = (Input.Forward * Input.Forward) +
					  (Input.Left * Input.Left) +
					  (Input.Up * Input.Up);

				// Slow down by the speed factor
				float flSpeedFactor = 1.0f;
				// if ( player->m_pSurfaceData )
				// {
					// flSpeedFactor = player->m_pSurfaceData->game.maxSpeedFactor;
				// }

				// If we have a constraint, slow down because of that too.
				float flConstraintSpeedFactor = ComputeConstraintSpeedFactor();

				MaxSpeed *= flSpeedFactor;

				// Same thing but only do the sqrt if we have to.
				if ( (spd != 0.0) && (spd > MaxSpeed * MaxSpeed) )
				{
					float fRatio = MaxSpeed / MathF.Sqrt( spd );
					Input.Forward *= fRatio;
					Input.Left *= fRatio;
					Input.Up *= fRatio;
				}
			}

			/* if ( player->GetFlags() & FL_FROZEN ||
				 player->GetFlags() & FL_ONTRAIN ||
				 IsDead() )
			{
				mv->m_flForwardMove = 0;
				mv->m_flSideMove = 0;
				mv->m_flUpMove = 0;
			}*/

			// DecayPunchAngle();

			/*
			// Take angles from command.
			if ( !IsDead() )
			{
				v_angle = Input.Rotation;
				v_angle = v_angle + player->m_Local.m_vecPunchAngle;

				// Now adjust roll angle
				if ( player->GetMoveType() != MOVETYPE_ISOMETRIC &&
					 player->GetMoveType() != MOVETYPE_NOCLIP )
				{
					mv->m_vecAngles[ROLL] = CalcRoll( v_angle, mv->m_vecVelocity, sv_rollangle.GetFloat(), sv_rollspeed.GetFloat() );
				}
				else
				{
					mv->m_vecAngles[ROLL] = 0.0; // v_angle[ ROLL ];
				}
				mv->m_vecAngles[PITCH] = v_angle[PITCH];
				mv->m_vecAngles[YAW] = v_angle[YAW];
			}
			else
			{
				mv->m_vecAngles = mv->m_vecOldAngles;
			}*/

			// Set dead player view_offset
			if ( IsDead() )
			{
				// SetViewOffset()
				// player->SetViewOffset( VEC_DEAD_VIEWHEIGHT_SCALED( player ) );
			}
		}

		public bool IsGrounded()
		{
			return GroundEntity != null;
		}

		//-----------------------------------------------------------------------------
		// 
		//-----------------------------------------------------------------------------
		void UpdateDuckJumpEyeOffset()
		{
			if ( m_flDuckJumpTime != 0.0f )
			{
				float flDuckMilliseconds = MathF.Max( 0.0f, m_flDucktime - m_flDuckJumpTime );
				float flDuckSeconds = flDuckMilliseconds / GAMEMOVEMENT_DUCK_TIME;
				if ( flDuckSeconds > TIME_TO_UNDUCK )
				{
					m_flDuckJumpTime = 0.0f;
					SetDuckedEyeOffset( 0.0f );
				}
				else
				{
					float flDuckFraction = Easing.QuadraticInOut( 1.0f - (flDuckSeconds / TIME_TO_UNDUCK) );
					SetDuckedEyeOffset( flDuckFraction );
				}
			}
		}
	}
}
