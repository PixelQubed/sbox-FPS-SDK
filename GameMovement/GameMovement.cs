using Sandbox;
using System;

namespace Source1
{
	public partial class Source1GameMovement : PawnController
	{
		Source1Player Player { get; set; }
		protected float MaxSpeed { get; set; }
		protected float SurfaceFriction { get; set; }


		protected float ForwardMove { get; set; }
		protected float SideMove { get; set; }
		protected float UpMove { get; set; }


		public override void FrameSimulate()
		{
			base.FrameSimulate();
			EyeRotation = Input.Rotation;
		}

		public virtual void PawnChanged( Source1Player player, Source1Player prev )
		{

		}

		public override void Simulate()
		{
			if ( Player != Pawn )
			{
				var newPlayer = Pawn as Source1Player;
				PawnChanged( newPlayer, Player );
				Player = newPlayer;
			}

			ProcessMovement();
			ShowDebugOverlay();
		}

		public virtual void ProcessMovement()
		{
			if ( Player == null ) return;
			MaxSpeed = Player.MaxSpeed;

			var speed = GetWishSpeed();
			ForwardMove = speed * Input.Forward;
			SideMove = speed * -Input.Left;
			UpMove = speed * Input.Up;

			if ( !Player.CanPlayerMove() )
			{
				Input.Forward = 0;
				Input.Left = 0;
				Input.Up = 0;
			}

			PlayerMove();
		}

		protected float FallVelocity { get; set; }

		public virtual void PlayerMove()
		{
			ReduceTimers();

			EyeRotation = Input.Rotation;

			if ( Pawn.MoveType != MoveType.MOVETYPE_NOCLIP &&
				Pawn.MoveType != MoveType.None &&
				Pawn.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				Pawn.MoveType != MoveType.MOVETYPE_OBSERVER &&
				!IsDead() ) 
			{
				if ( CheckInterval( IntervalType.Stuck ) )
				{
					if ( CheckStuck() )
					{
						// Can't move, we're stuck
						return;
					}
				}
			}

			if ( Velocity.z > 250.0f ) ClearGroundEntity();

			LastWaterLevelType = Player.WaterLevelType;

			// If we are not on ground, store off how fast we are moving down
			if ( !IsGrounded() ) FallVelocity = -Velocity.z;

			Player.SimulateFootsteps();

			UpdateDuckJumpEyeOffset();
			Duck();
			
			if ( !IsDead() ) 
			{
				if ( !LadderMove() && Player.MoveType == MoveType.MOVETYPE_LADDER )
				{
					// Clear ladder stuff unless player is dead or riding a train
					// It will be reset immediately again next frame if necessary
					Player.MoveType = MoveType.MOVETYPE_WALK;
				}
			}

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
					FullLadderMove();
					break;

				case MoveType.MOVETYPE_ISOMETRIC:
				case MoveType.MOVETYPE_WALK:
					FullWalkMove();
					break;

				case MoveType.MOVETYPE_OBSERVER:
					// FullObserverMove(); // clips against world&players
					break;
			}
		}

		public virtual void ReduceTimers()
		{
			float frame_msec = 1000.0f * Time.Delta;

			if ( DuckTime > 0 )
			{
				DuckTime -= frame_msec;
				if ( DuckTime < 0 ) DuckTime = 0;
			}

			if ( DuckJumpTime > 0 )
			{
				DuckJumpTime -= frame_msec;
				if ( DuckJumpTime < 0 ) DuckJumpTime = 0;
			}

			if ( JumpTime > 0 )
			{
				JumpTime -= frame_msec;
				if ( JumpTime < 0 ) JumpTime = 0;
			}
		}


		public virtual void StepMove( Vector3 dest )
		{
			MoveHelper mover = new MoveHelper( Position, Velocity );
			mover.Trace = SetupBBoxTrace( 0, 0 );
			mover.MaxStandableAngle = sv_maxstandableangle;

			mover.TryMoveWithStep( Time.Delta, sv_stepsize );

			Position = mover.Position;
			Velocity = mover.Velocity;
		}

		public virtual void TryPlayerMove()
		{
			MoveHelper mover = new MoveHelper( Position, Velocity );
			mover.Trace = SetupBBoxTrace( 0, 0 );
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

			// If not going to add any speed, done.
			if ( addspeed <= 0 )
				return;

			// Determine amount of acceleration.
			var accelspeed = acceleration * wishspeed * Time.Delta;

			// Cap at addspeed
			if ( accelspeed > addspeed )
				accelspeed = addspeed;

			Velocity += wishdir * accelspeed;
		}

		/// <summary>
		/// Remove ground friction from velocity
		/// </summary>
		public virtual void Friction()
		{
			// If we are in water jump cycle, don't apply friction
			if ( IsJumpingFromWater ) 
				return;

			// Calculate speed
			var speed = Velocity.Length;
			if ( speed < 0.1f ) return;

			float friction, control, drop = 0;
			if ( !InAir() )
			{
				friction = sv_friction * SurfaceFriction;

				// Bleed off some speed, but if we have less than the bleed
				// threshold, bleed the threshold amount.
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

			if ( wishspeed != 0 && wishspeed > MaxSpeed )
			{
				WishVelocity *= MaxSpeed / wishspeed;
				wishspeed = MaxSpeed;
			}

			Accelerate( wishdir, wishspeed, sv_airaccelerate, sv_aircontrol );

			Velocity += BaseVelocity;
			TryPlayerMove();
			Velocity -= BaseVelocity;
		}

		public virtual void CategorizePosition()
		{
			SurfaceFriction = 1.0f;
			CheckWater();

			var point = Position - Vector3.Up * 2;
			var bumpOrigin = Position;

			float zvel = Velocity.z;
			bool bMovingUp = zvel > 0;
			bool bMovingUpRapidly = zvel > sv_maxnonjumpvelocity;
			float flGroundEntityVelZ = 0;

			if( bMovingUpRapidly )
			{
				if ( IsGrounded() )
				{
					flGroundEntityVelZ = GroundEntity.Velocity.z;
					bMovingUpRapidly = (zvel - flGroundEntityVelZ) > sv_maxnonjumpvelocity;
				}
			}


			if ( bMovingUpRapidly || (bMovingUp && Player.MoveType == MoveType.MOVETYPE_LADDER) )
			{
				ClearGroundEntity();
			}
			else
			{
				var pm = TraceBBox( bumpOrigin, point );
				if ( pm.Entity == null || Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle ) 
				{
					pm = TryTouchGroundInQuadrants( bumpOrigin, point, pm );
					if ( pm.Entity == null || Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
					{
						ClearGroundEntity();

						if ( Velocity.z > 0 && Player.MoveType != MoveType.MOVETYPE_NOCLIP ) 
						{
							SurfaceFriction = 0.25f;
						}
					} else
					{
						UpdateGroundEntity( pm );
					}
				} else
				{
					UpdateGroundEntity( pm );
				}
			}
		}

		public TraceResult TryTouchGroundInQuadrants( Vector3 start, Vector3 end, TraceResult pm )
		{
			bool isDucked = false;

			Vector3 mins, maxs;
			Vector3 minsSrc = GetPlayerMins( isDucked );
			Vector3 maxsSrc = GetPlayerMaxs( isDucked );

			float fraction = pm.Fraction;
			Vector3 endpos = pm.EndPos;

			// Check the -x, -y quadrant
			mins = minsSrc;
			maxs = new( MathF.Min( 0, maxsSrc.x ), MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

			pm = TraceBBox( start, end, mins, maxs );
			if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
			{
				pm.Fraction = fraction;
				pm.EndPos = endpos;
				return pm;
			}

			// Check the +x, +y quadrant
			maxs = maxsSrc;
			mins = new( MathF.Max( 0, minsSrc.x ), MathF.Max( 0, minsSrc.y ), minsSrc.z );

			pm = TraceBBox( start, end, mins, maxs );
			if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
			{
				pm.Fraction = fraction;
				pm.EndPos = endpos;
				return pm;
			}

			// Check the -x, +y quadrant
			mins = new( minsSrc.x, MathF.Max( 0, minsSrc.y ), minsSrc.z );
			maxs = new( MathF.Min( 0, maxsSrc.x ), maxsSrc.y, maxsSrc.z );

			pm = TraceBBox( start, end, mins, maxs );
			if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
			{
				pm.Fraction = fraction;
				pm.EndPos = endpos;
				return pm;
			}

			// Check the +x, -y quadrant
			mins = new( MathF.Max( 0, minsSrc.x ), minsSrc.y, minsSrc.z );
			maxs = new( maxsSrc.x, MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

			pm = TraceBBox( start, end, mins, maxs );
			if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
			{
				pm.Fraction = fraction;
				pm.EndPos = endpos;
				return pm;
			}

			pm.Fraction = fraction;
			pm.EndPos = endpos;
			return pm;
		}


		/// <summary>
		/// We have a new ground entity
		/// </summary>
		public virtual void UpdateGroundEntity( TraceResult tr )
		{
			var newGround = tr.Entity;
			var oldGround = GroundEntity;

			var vecBaseVelocity = BaseVelocity;

			if ( oldGround == null && newGround != null )
			{
				// Subtract ground velocity at instant we hit ground jumping
				vecBaseVelocity -= newGround.Velocity;
				vecBaseVelocity.z = newGround.Velocity.z;
			}
			else if ( oldGround != null && newGround != null ) 
			{
				// Add in ground velocity at instant we started jumping
				vecBaseVelocity += oldGround.Velocity;
				vecBaseVelocity.z = oldGround.Velocity.z;
			}

			BaseVelocity = vecBaseVelocity;
			GroundEntity = newGround;

			// If we are on something...
			if ( newGround != null ) 
			{
				CategorizeGroundSurface( tr );

				// Then we are not in water jump sequence
				WaterJumpTime = 0;

				Velocity = Velocity.WithZ( 0 );
			}
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

			Position = trace.EndPos;
		}

		public Entity TestPlayerPosition( Vector3 pos, ref TraceResult pm )
		{
			pm = TraceBBox( pos, pos );
			return pm.Entity;
		}

		public virtual void CategorizeGroundSurface( TraceResult pm )
		{
			Player.SurfaceData = pm.Surface;
			SurfaceFriction = pm.Surface.Friction;

			SurfaceFriction *= 1.25f;
			if ( SurfaceFriction > 1.0f )
				SurfaceFriction = 1.0f;
		}

		public bool IsDead()
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

		public bool IsGrounded()
		{
			return GroundEntity != null;
		}

		public enum IntervalType
		{
			Ground,
			Stuck,
			Ladder
		}

		float TickInterval => 0.015f;

		// Roughly how often we want to update the info about the ground surface we're on.
		// We don't need to do this very often.
		float CategorizeGroundSurfaceInterval => 0.3f;
		int CategrizeGroundSurfaceTickInterval => (int)(CategorizeGroundSurfaceInterval / TickInterval);

		float CheckStuckInterval => 1;
		int CheckStuckTickInterval => (int)(CheckStuckInterval / TickInterval);

		float CheckLadderInterval => 0.2f;
		int CheckLadderTickInterval => (int)(CheckLadderInterval / TickInterval);

		public int GetCheckInterval( IntervalType type )
		{
			int tickInterval = 1;
			switch ( type )
			{
				default:
					tickInterval = 1;
					break;

				case IntervalType.Ground:
					tickInterval = CategrizeGroundSurfaceTickInterval;
					break;

				case IntervalType.Stuck:
					// If we are in the process of being "stuck", then try a new position every command tick until m_StuckLast gets reset back down to zero
					/*if ( player->m_StuckLast != 0 )
					{
						tickInterval = 1;
					}
					else*/
					{
						tickInterval = CheckStuckTickInterval;
					}
					break;

				case IntervalType.Ladder:
					tickInterval = CheckLadderTickInterval;
					break;
			}

			return tickInterval;
		}

		public bool CheckInterval( IntervalType type )
		{
			int tickInterval = GetCheckInterval( type );
			return (Time.Tick + Player.NetworkIdent) % tickInterval == 0;
		}

		protected void ShowDebugOverlay()
		{
			if ( cl_debug_movement && Host.IsClient )
			{
				DebugOverlay.ScreenText( 0, $"MoveType          {Player.MoveType}" );
				DebugOverlay.ScreenText( 1, $"Water Level       {Player.WaterLevelType}" );
				DebugOverlay.ScreenText( 2, $"Water Fraction    {Player.WaterLevel.Fraction}" );
				DebugOverlay.ScreenText( 3, $"m_flWaterJumpTime {WaterJumpTime}" );
				/*
				DebugOverlay.ScreenText( 0, $"PlayerFlags.Ducked  {Pawn.Tags.Has( PlayerTags.Ducked )}" );
				DebugOverlay.ScreenText( 1, $"IsDucking           {IsDucking}" );
				DebugOverlay.ScreenText( 2, $"IsDucked            {IsDucked}" );
				DebugOverlay.ScreenText( 3, $"DuckTime            {DuckTime}" );
				DebugOverlay.ScreenText( 4, $"DuckJumpTime        {DuckJumpTime}" );
				DebugOverlay.ScreenText( 5, $"JumpTime            {JumpTime}" );
				DebugOverlay.ScreenText( 6, $"InDuckJump          {InDuckJump}" );
				DebugOverlay.ScreenText( 7, $"AllowAutoMovement:  {Player.AllowAutoMovement}" );
				DebugOverlay.ScreenText( 8, $"Speed:              {Pawn.Velocity.Length}HU" );*/
			}
		}
	}
}
