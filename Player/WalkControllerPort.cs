using Sandbox;
using System;

namespace Source1
{
	public partial class S1GameMovementPort : PawnController
	{
		S1Player Player { get; set; }
		MoveHelper MoveHelper { get; set; }

		protected Sandbox.Internal.WaterLevel m_nOldWaterLevel;
		protected float m_flWaterEntryTime;
		protected int m_nOnLadder;

		protected Vector3 m_vecForward;
		protected Vector3 m_vecRight;
		protected Vector3 m_vecUp;

		protected float m_flForwardMove;
		protected float m_flSideMove;
		protected float m_flUpMove;

		protected Vector3 m_vecVelocity;

		// Cache used to remove redundant calls to GetPointContents().
		// int m_CachedGetPointContents[MAX_PLAYERS][MAX_PC_CACHE_SLOTS];
		// Vector m_CachedGetPointContentsPoint[MAX_PLAYERS][MAX_PC_CACHE_SLOTS];	

		protected Vector3 m_vecProximityMins;      // Used to be globals in sv_user.cpp.
		protected Vector3 m_vecProximityMaxs;

		public SPEED_CROPPED m_iSpeedCropped;

		[Flags]
		public enum SPEED_CROPPED
		{
			RESET,
			DUCK,
			WEAPON
		}

		// float m_flStuckCheckTime[MAX_PLAYERS + 1][2]; // Last time we did a full test

		// Output
		float m_outStepHeight;  // how much you climbed this move
		Vector3 m_outWishVel;        // This is where you tried 
		Vector3 m_outJumpVel;        // This is your jump velocity

		public enum IntervalType
		{
			GROUND = 0,
			STUCK,
			LADDER
		}

		// FROM PLAYER

		int m_StuckLast;
		int m_flWaterJumpTime;

		Surface m_surfaceProps;
		float m_surfaceFriction;

		float m_flFallVelocity;
		float m_flDuckJumpTime;

		public override void Simulate()
		{
			// ResetGetPointContentsCache();

			// Cropping movement speed scales mv->m_fForwardSpeed etc. globally
			// Once we crop, we don't want to recursively crop again, so we set the crop
			//  flag globally here once per usercmd cycle.
			m_iSpeedCropped = SPEED_CROPPED.RESET;

			// StartTrackPredictionErrors should have set this
			Player = Pawn as S1Player;

			// Run the command.
			PlayerMove();
		}

		//-----------------------------------------------------------------------------
		// Purpose: 
		//-----------------------------------------------------------------------------
		public void PlayerMove(  )
		{
			// VPROF( "CGameMovement::PlayerMove" );

			// clear output applied velocity
			m_outWishVel = default;
			m_outJumpVel = default;

			// MoveHelper()->ResetTouchList();                    // Assume we don't touch anything

			// ReduceTimers();

			m_vecForward = Input.Rotation.Forward;
			m_vecRight = Input.Rotation.Right;
			m_vecUp = Input.Rotation.Up;

			// Always try and unstick us unless we are using a couple of the movement modes
			if ( Player.MoveType != MoveType.MOVETYPE_NOCLIP &&
				 Player.MoveType != MoveType.None &&
				 Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				 Player.MoveType != MoveType.MOVETYPE_OBSERVER &&
				 Player.IsAlive )
			{
				if ( CheckInterval( IntervalType.STUCK ) )
				{
					/*
					if ( CheckStuck() )
					{
						// Can't move, we're stuck
						return;
					}*/
				}
			}

			// Now that we are "unstuck", see where we are (player->GetWaterLevel() and type, player->GetGroundEntity()).
			if ( Player.MoveType != MoveType.MOVETYPE_WALK )
			{
				CategorizePosition();
			}
			else
			{
				if ( m_vecVelocity.z > 250.0f )
				{
					SetGroundEntity( default );
				}
			}

			// Store off the starting water level
			m_nOldWaterLevel = Player.WaterLevel;

			// If we are not on ground, store off how fast we are moving down
			if ( Player.GroundEntity == null ) 
			{
				m_flFallVelocity = -m_vecVelocity.z;
			}

			m_nOnLadder = 0;

			// Player.UpdateStepSound( player->m_pSurfaceData, mv->GetAbsOrigin(), mv->m_vecVelocity );

			UpdateDuckJumpEyeOffset();
			// Duck();

			// Handle movement modes.
			switch ( Player.MoveType )
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
			}
		}


		//-----------------------------------------------------------------------------
		// Purpose: 
		// Input  : ducked - 
		// Output : const Vector
		//-----------------------------------------------------------------------------
		public Vector3 GetPlayerMins( bool ducked )
		{
			return (ducked
				? GameRules.Instance.ViewVectors.DuckHullMin
				: GameRules.Instance.ViewVectors.HullMin
				) * Player.Scale;
		}

		//-----------------------------------------------------------------------------
		// Purpose: 
		// Input  : ducked - 
		// Output : const Vector
		//-----------------------------------------------------------------------------
		public Vector3 GetPlayerMaxs( bool ducked )
		{
			return (ducked
				? GameRules.Instance.ViewVectors.DuckHullMax
				: GameRules.Instance.ViewVectors.HullMax
				) * Player.Scale;
		}

		public Vector3 GetPlayerMins(  )
		{
			return GetPlayerMins( false );
		}

		public Vector3 GetPlayerMaxs(  )
		{
			return GetPlayerMaxs( false );
		}


		//-----------------------------------------------------------------------------
		// Purpose: 
		// Input  : ducked - 
		// Output : const Vector
		//-----------------------------------------------------------------------------
		public Vector3 GetPlayerViewOffset( bool ducked )
		{
			return (ducked
				? GameRules.Instance.ViewVectors.DuckViewPosition
				: GameRules.Instance.ViewVectors.ViewPosition
				) * Player.Scale;
		}


		//-----------------------------------------------------------------------------
		// Purpose: 
		// Input  : ducked - 
		// Output : const Vector
		//-----------------------------------------------------------------------------
		public Vector3 GetPlayerExtents( bool ducked )
		{
			var mins = GetPlayerMins( ducked );
			var maxs = GetPlayerMaxs( ducked );

			return new(
				MathF.Abs( mins.x ) + MathF.Abs( maxs.x ),
				MathF.Abs( mins.y ) + MathF.Abs( maxs.y ),
				MathF.Abs( mins.z ) + MathF.Abs( maxs.z )
			);

		}

		public Entity TestPlayerPosition( Vector3 pos, CollisionGroup collisionGroup, out TraceResult tr )
		{
			bool isDucked = false;

			tr = Trace.Box( GetPlayerExtents( isDucked ), pos, pos )
				.Ignore( Pawn )
				.Run();

			if ( tr.Hit && tr.Entity != null ) return tr.Entity;
			else return null;
		}

		public bool IsDead()
		{
			return Player.LifeState != LifeState.Alive && Player.Health <= 0;
		}

		public bool CheckInterval( IntervalType type )
		{
			int tickInterval = GetCheckInterval( type );

			//  return (player->CurrentCommandNumber() + player->entindex()) % tickInterval == 0;
			return (Time.Tick + Player.NetworkIdent) % tickInterval == 0;
		}

		const float TICK_INTERVAL = 0.015f;
		const float CATEGORIZE_GROUND_SURFACE_INTERVAL = 0.3f;
		const int CATEGORIZE_GROUND_SURFACE_TICK_INTERVAL = (int)(CATEGORIZE_GROUND_SURFACE_INTERVAL / TICK_INTERVAL);

		const float CHECK_STUCK_INTERVAL = 1.0f;
		const int CHECK_STUCK_TICK_INTERVAL = (int)(CHECK_STUCK_INTERVAL / TICK_INTERVAL);

		const float CHECK_STUCK_INTERVAL_SP = 0.2f;
		const int CHECK_STUCK_TICK_INTERVAL_SP = (int)(CHECK_STUCK_INTERVAL_SP / TICK_INTERVAL);

		const float CHECK_LADDER_INTERVAL = 0.2f;
		const int CHECK_LADDER_TICK_INTERVAL = (int)(CHECK_LADDER_INTERVAL / TICK_INTERVAL);

		int GetCheckInterval( IntervalType type )
		{
			int tickInterval = 1;
			switch ( type )
			{
				default:
					tickInterval = 1;
					break;

				case IntervalType.GROUND:
					tickInterval = CATEGORIZE_GROUND_SURFACE_TICK_INTERVAL;
					break;

				case IntervalType.STUCK:
					// If we are in the process of being "stuck", then try a new position every command tick until m_StuckLast gets reset back down to zero
					if ( m_StuckLast != 0 )
					{
						tickInterval = 1;
					}
					else
					{
						// garry pls expose maxplayers
						int maxplayers = 2;

						if ( maxplayers == 1 ) 
						{
							tickInterval = CHECK_STUCK_TICK_INTERVAL_SP;
						}
						else
						{
							tickInterval = CHECK_STUCK_TICK_INTERVAL;
						}
					}
					break;

				case IntervalType.LADDER:
					tickInterval = CHECK_LADDER_TICK_INTERVAL;
					break;
			}
			return tickInterval;
		}

		//-----------------------------------------------------------------------------
		// Purpose: 
		// Input  : &input - 
		//-----------------------------------------------------------------------------
		void CategorizePosition()
		{
			Vector3 point = Player.Position;
			TraceResult pm = default;

			// TODO VC:
			// Reset this each time we-recategorize, otherwise we have bogus friction when we jump into water and plunge downward really quickly
			// player->m_surfaceFriction = 1.0f;

			// if the player hull point one unit down is solid, the player
			// is on ground

			// see if standing on something solid	

			// Doing this before we move may introduce a potential latency in water detection, but
			// doing it after can get us stuck on the bottom in water if the amount we move up
			// is less than the 1 pixel 'threshold' we're about to snap to.	Also, we'll call
			// this several times per frame, so we really need to avoid sticking to the bottom of
			// water on each call, and the converse case will correct itself if called twice.
			// CheckWater();

			// TODO VC:
			// observers don't have a ground entity
			// if ( player->IsObserver() )
			//	return;

			float flOffset = 2.0f;

			point = point.WithZ( point.z - flOffset );

			Vector3 bumpOrigin;
			bumpOrigin = Player.Position;

			// Shooting up really fast.  Definitely not on ground.
			// On ladder moving up, so not on ground either
			// NOTE: 145 is a jump.
			const float NON_JUMP_VELOCITY = 140.0f;

			float zvel = Player.Velocity.z;
			bool bMovingUp = zvel > 0.0f;
			bool bMovingUpRapidly = zvel > NON_JUMP_VELOCITY;
			float flGroundEntityVelZ = 0.0f;
			if ( bMovingUpRapidly )
			{
				// Tracker 73219, 75878:  ywb 8/2/07
				// After save/restore (and maybe at other times), we can get a case where we were saved on a lift and 
				//  after restore we'll have a high local velocity due to the lift making our abs velocity appear high.  
				// We need to account for standing on a moving ground object in that case in order to determine if we really 
				//  are moving away from the object we are standing on at too rapid a speed.  Note that CheckJump already sets
				//  ground entity to NULL, so this wouldn't have any effect unless we are moving up rapidly not from the jump button.
				var ground = Player.GroundEntity;
				if ( ground != null )
				{
					flGroundEntityVelZ = ground.Velocity.z;
					bMovingUpRapidly = (zvel - flGroundEntityVelZ) > NON_JUMP_VELOCITY;
				}
			}

			// Was on ground, but now suddenly am not
			if ( bMovingUpRapidly ||
				(bMovingUp && Player.MoveType == MoveType.MOVETYPE_LADDER) ) 
			{
				SetGroundEntity( default );
			}
			else
			{
				// TODO:
				var isDucked = false;

				// Try and move down.
				TryTouchGround( bumpOrigin, point, GetPlayerMins( isDucked ), GetPlayerMaxs( isDucked ), ref pm );

				// Was on ground, but now suddenly am not.  If we hit a steep plane, we are not on ground
				if ( pm.Entity == null || pm.Normal.z < 0.7f )
				{
					// Test four sub-boxes, to see if any of them would have found shallower slope we could actually stand on
					TryTouchGroundInQuadrants( bumpOrigin, point, ref pm );

					if ( pm.Entity == null || pm.Normal.z < 0.7f )
					{
						SetGroundEntity( default );

						// probably want to add a check for a +z velocity too!
						if ( (m_vecVelocity.z > 0.0f) &&
							(Player.MoveType != MoveType.MOVETYPE_NOCLIP) ) 
						{
							m_surfaceFriction = 0.25f;
						}
					}
					else
					{
						SetGroundEntity( pm );
					}
				}
				else
				{
					SetGroundEntity( pm );  // Otherwise, point to index of ent under us.
				}
			}
		}

		void SetGroundEntity( TraceResult pm )
		{
			Entity newGround = pm.Entity;

			Entity oldGround = Player.GroundEntity;
			Vector3 vecBaseVelocity = Player.BaseVelocity;

			if ( oldGround == null && newGround != null )
			{
				// Subtract ground velocity at instant we hit ground jumping
				vecBaseVelocity -= newGround.Velocity;
				vecBaseVelocity.z = newGround.Velocity.z;
			}
			else if ( oldGround != null && newGround == null ) 
			{
				// Add in ground velocity at instant we started jumping
				vecBaseVelocity += oldGround.Velocity;
				vecBaseVelocity.z = oldGround.Velocity.z;
			}

			Player.BaseVelocity = vecBaseVelocity;
			Player.GroundEntity = newGround;

			// If we are on something...

			if ( newGround != null ) 
			{
				CategorizeGroundSurface( pm );

				// Then we are not in water jump sequence
				m_flWaterJumpTime = 0;

				// Standing on an entity other than the world, so signal that we are touching something.
				if ( !pm.Entity.IsWorld ) 
				{
					// TODO VC
					//MoveHelper()->AddToTouched( *pm, mv->m_vecVelocity );
				}

				m_vecVelocity = m_vecVelocity.WithZ( 0.0f );
			}
		}

		//-----------------------------------------------------------------------------
		// Purpose: 
		//-----------------------------------------------------------------------------
		void CategorizeGroundSurface( TraceResult pm )
		{
			m_surfaceProps = pm.Surface;

			// HACKHACK: Scale this to fudge the relationship between vphysics friction values and player friction values.
			// A value of 0.8f feels pretty normal for vphysics, whereas 1.0f is normal for players.
			// This scaling trivially makes them equivalent.  REVISIT if this affects low friction surfaces too much.

			m_surfaceFriction *= 1.25f;
			if ( m_surfaceFriction > 1.0f )
				m_surfaceFriction = 1.0f;
		}

		//-----------------------------------------------------------------------------
		// Traces the player's collision bounds in quadrants, looking for a plane that
		// can be stood upon (normal's z >= 0.7f).  Regardless of success or failure,
		// replace the fraction and endpos with the original ones, so we don't try to
		// move the player down to the new floor and get stuck on a leaning wall that
		// the original trace hit first.
		//-----------------------------------------------------------------------------
		void TryTouchGroundInQuadrants( Vector3 start, Vector3 end, ref TraceResult pm )
		{
			// VPROF( "CGameMovement::TryTouchGroundInQuadrants" );

			// TODO: 
			bool isDucked = false;

			Vector3 mins, maxs;
			Vector3 minsSrc = GetPlayerMins( isDucked );
			Vector3 maxsSrc = GetPlayerMaxs( isDucked );

			float fraction = pm.Fraction;
			Vector3 endpos = pm.EndPos;


			// Check the -x, -y quadrant
			mins = minsSrc;
			maxs = new( MathF.Min( 0, maxsSrc.x ), MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

			TryTouchGround( start, end, mins, maxs, ref pm );
			if ( pm.Entity != null && pm.Normal.z >= 0.7f ) 
			{
				pm.Fraction = fraction;
				pm.EndPos = endpos;
				return;
			}

			// Check the +x, +y quadrant
			maxs = maxsSrc;
			mins = new(MathF.Max( 0, minsSrc.x ), MathF.Max( 0, minsSrc.y ), minsSrc.z );

			TryTouchGround( start, end, mins, maxs, ref pm );
			if ( pm.Entity != null && pm.Normal.z >= 0.7f )
			{
				pm.Fraction = fraction;
				pm.EndPos = endpos;
				return;
			}

			// Check the -x, +y quadrant
			mins = new( minsSrc.x, MathF.Max( 0, minsSrc.y ), minsSrc.z );
			maxs = new( MathF.Min( 0, maxsSrc.x ), maxsSrc.y, maxsSrc.z );

			TryTouchGround( start, end, mins, maxs, ref pm );
			if ( pm.Entity != null && pm.Normal.z >= 0.7f )
			{
				pm.Fraction = fraction;
				pm.EndPos = endpos;
				return;
			}

			// Check the +x, -y quadrant
			mins = new( MathF.Max( 0, minsSrc.x ), minsSrc.y, minsSrc.z );
			maxs = new( maxsSrc.x, MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

			TryTouchGround( start, end, mins, maxs,ref pm );
			if ( pm.Entity != null && pm.Normal.z >= 0.7f )
			{
				pm.Fraction = fraction;
				pm.EndPos = endpos;
				return;
			}

			pm.Fraction = fraction;
			pm.EndPos = endpos;
		}

		//-----------------------------------------------------------------------------
		// Purpose: overridded by game classes to limit results (to standable objects for example)
		//-----------------------------------------------------------------------------
		void TryTouchGround( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, ref TraceResult pm )
		{
			// VPROF( "CGameMovement::TryTouchGround" );

			pm = Trace.Ray( start, end )
				.Size( mins, maxs )
				.Ignore( Pawn )
				.Run();
		}

		const float TIME_TO_DUCK = 0.2f;
		const float TIME_TO_DUCK_MS = 200.0f;

		const float TIME_TO_UNDUCK = 0.2f;
		const float TIME_TO_UNDUCK_MS = 200.0f;

		const float GAMEMOVEMENT_DUCK_TIME = 1000.0f; // ms
		const float GAMEMOVEMENT_JUMP_TIME = 510.0f; // ms approx - based on the 21 unit height jump
		const float GAMEMOVEMENT_JUMP_HEIGHT = 21.0f; // units

		const float GAMEMOVEMENT_TIME_TO_UNDUCK = (TIME_TO_UNDUCK * 1000.0f); // ms
		const float GAMEMOVEMENT_TIME_TO_UNDUCK_INV = (GAMEMOVEMENT_DUCK_TIME - GAMEMOVEMENT_TIME_TO_UNDUCK);


		void UpdateDuckJumpEyeOffset()
		{
			if ( m_flDuckJumpTime != 0.0f )
			{
				float flDuckMilliseconds = MathF.Max( 0.0f, GAMEMOVEMENT_DUCK_TIME - m_flDuckJumpTime );
				float flDuckSeconds = flDuckMilliseconds / GAMEMOVEMENT_DUCK_TIME;
				if ( flDuckSeconds > TIME_TO_UNDUCK )
				{
					m_flDuckJumpTime = 0.0f;
					SetDuckedEyeOffset( 0.0f );
				}
				else
				{
					float flDuckFraction = Easing.EaseInOut( 1.0f - (flDuckSeconds / TIME_TO_UNDUCK) );
					SetDuckedEyeOffset( flDuckFraction );
				}
			}
		}

		//
		//-----------------------------------------------------------------------------
		// Purpose: 
		// Input  : duckFraction - 
		//-----------------------------------------------------------------------------
		void SetDuckedEyeOffset( float duckFraction )
		{
			Vector3 vDuckHullMin = GetPlayerMins( true );
			Vector3 vStandHullMin = GetPlayerMins( false );

			float fMore = (vDuckHullMin.z - vStandHullMin.z);

			Vector3 vecDuckViewOffset = GetPlayerViewOffset( true );
			Vector3 vecStandViewOffset = GetPlayerViewOffset( false );

			Vector3 temp = Player.EyePos;
			temp.z = ((vecDuckViewOffset.z - fMore) * duckFraction) +
						(vecStandViewOffset.z * (1 - duckFraction));

			Player.EyePos = temp;
		}

		//-----------------------------------------------------------------------------
		// Purpose: 
		//-----------------------------------------------------------------------------
		void FullWalkMove()
		{
			// TODO VC
			// if ( !CheckWater() )
			{
				StartGravity();
			}

			if ( Player.GroundEntity != null ) 
			{
				WalkMove();
			}
			else
			{
			}
		}



		//-----------------------------------------------------------------------------
		// Purpose: 
		//-----------------------------------------------------------------------------
		void WalkMove()
		{
			int i;

			Vector3 wishvel = default;
			float spd;
			float fmove, smove;
			Vector3 wishdir;
			float wishspeed;

			Vector3 dest = default;
			TraceResult pm;


			var forward = Input.Rotation.Forward;
			var right = Input.Rotation.Right;
			var up = Input.Rotation.Up;

			var oldground = Player.GroundEntity;

			// Copy movement amounts
			fmove = m_flForwardMove;
			smove = m_flSideMove;

			// Zero out z components of movement vectors
			if ( forward.z != 0 )
			{
				forward.z = 0;
				forward = forward.Normal;
			}

			if ( right[2] != 0 )
			{
				right[2] = 0;
				right = right.Normal;
			}

			wishvel.x = forward.x * fmove + right.x * smove;
			wishvel.y = forward.y * fmove + right.y * smove;

			wishvel.z = 0;             // Zero out z part of velocity

			wishdir = wishvel;
			wishspeed = wishdir.Length;

			//
			// Clamp to server defined max speed
			//
			/*
			if ( (wishspeed != 0.0f) && (wishspeed > mv->m_flMaxSpeed) )
			{
				VectorScale( wishvel, mv->m_flMaxSpeed / wishspeed, wishvel );
				wishspeed = mv->m_flMaxSpeed;
			}*/

			// Set pmove velocity
			m_vecVelocity.z = 0;
			Accelerate( wishdir, wishspeed, 5.6f );
			m_vecVelocity.z = 0;

			// Add in any base velocity to the current velocity.
			m_vecVelocity += Player.BaseVelocity;

			spd = m_vecVelocity.Length;

			if ( spd < 1.0f )
			{
				m_vecVelocity = default;
				// Now pull the base velocity back out.   Base velocity is set if you are on a moving object, like a conveyor (or maybe another monster?)
				m_vecVelocity -= Player.BaseVelocity;
				return;
			}

			// first try just moving to the destination	
			dest.x = Pawn.Position.x + m_vecVelocity.x * Time.Delta;
			dest.y = Pawn.Position.y + m_vecVelocity.y * Time.Delta;
			dest.z = Pawn.Position.z;

			// first try moving directly to the next spot
			pm = TracePlayerBBox( Pawn.Position, dest );

			// If we made it all the way, then copy trace end as new player position.
			m_outWishVel += wishdir * wishspeed;

			if ( pm.Fraction == 1 )
			{
				Pawn.Position = pm.EndPos;

				// Now pull the base velocity back out.   Base velocity is set if you are on a moving object, like a conveyor (or maybe another monster?)
				m_vecVelocity -= Pawn.BaseVelocity;

				StayOnGround();
				return;
			}

			// Don't walk up stairs if not on ground.
			if ( oldground == null && Player.WaterLevel.Fraction == 0 ) 
			{
				// Now pull the base velocity back out.   Base velocity is set if you are on a moving object, like a conveyor (or maybe another monster?)
				m_vecVelocity -= Pawn.BaseVelocity;
				return;
			}

			// If we are jumping out of water, don't do anything more.
			if ( m_flWaterJumpTime == 0) 
			{
				// Now pull the base velocity back out.   Base velocity is set if you are on a moving object, like a conveyor (or maybe another monster?)
				m_vecVelocity -= Pawn.BaseVelocity;
				return;
			}


			StayOnGround();
		}


		//-----------------------------------------------------------------------------
		// Purpose: Does the basic move attempting to climb up step heights.  It uses
		//          the mv->GetAbsOrigin() and mv->m_vecVelocity.  It returns a new
		//          new mv->GetAbsOrigin(), mv->m_vecVelocity, and mv->m_outStepHeight.
		//-----------------------------------------------------------------------------
		void StepMove( Vector3 vecDestination, TraceResult tr )
		{
		}

		//-----------------------------------------------------------------------------
		// Purpose: Try to keep a walking player on the ground when running down slopes etc
		//-----------------------------------------------------------------------------
		void StayOnGround(  )
		{
			var start = Pawn.Position;
			var end = Pawn.Position;
			start.z += 2;
			end.z -= Player.StepSize;

			// See how far up we can go without getting stuck

			var trace = TracePlayerBBox( Pawn.Position, start );
			start = trace.EndPos;

			// using trace.startsolid is unreliable here, it doesn't get set when
			// tracing bounding box vs. terrain

			// Now trace down from a known safe position
			trace = TracePlayerBBox( start, end );

			if ( trace.Fraction > 0.0f &&   // must go somewhere
				trace.Fraction < 1.0f &&    // must hit something
				!trace.StartedSolid &&		// can't be embedded in a solid
				trace.Normal.z >= 0.7 )		// can't hit a steep slope that we can't stand on anyway
			{
				float flDelta = Math.Abs( Pawn.Position.z - trace.EndPos.z );
			}
		}

		//-----------------------------------------------------------------------------
		// Purpose: Traces player movement + position
		//-----------------------------------------------------------------------------
		TraceResult TracePlayerBBox( Vector3 start, Vector3 end )
		{
			// VPROF( "CGameMovement::TracePlayerBBox" );

			return Trace.Ray( start, end )
				.Size( GetPlayerMins(), GetPlayerMaxs() )
				.Ignore( Pawn )
				.Run();
		}

		//-----------------------------------------------------------------------------
		// Purpose: 
		//-----------------------------------------------------------------------------
		void StartGravity( )
		{
			float gravityScale = Player.PhysicsBody.GravityScale;

			// Add gravity so they'll be in the correct position during movement
			// yes, this 0.5 looks wrong, but it's not.  
			m_vecVelocity.z -= (gravityScale * 800 * 0.5f * Time.Delta);
			m_vecVelocity.z += Player.BaseVelocity.z * Time.Delta;

			var temp = Player.BaseVelocity;
			temp.z = 0;
			Player.BaseVelocity = temp;
		}

		void Accelerate( Vector3 wishdir, float wishspeed, float accel )
		{
			int i;
			float addspeed, accelspeed, currentspeed;

			// This gets overridden because some games (CSPort) want to allow dead (observer) players
			// to be able to move around.
			if ( !CanAccelerate() )
				return;

			// See if we are changing direction a bit
			currentspeed = m_vecVelocity.Dot( wishdir );

			// Reduce wishspeed by the amount of veer.
			addspeed = wishspeed - currentspeed;

			// If not going to add any speed, done.
			if ( addspeed <= 0 )
				return;

			// Determine amount of accleration.
			accelspeed = accel * Time.Delta * wishspeed * m_surfaceFriction;

			// Cap at addspeed
			if ( accelspeed > addspeed )
				accelspeed = addspeed;

			// Adjust velocity.
			m_vecVelocity += wishdir * accelspeed;
		}
		bool CanAccelerate()
		{
			// Dead players don't accelerate.
			if ( IsDead() )
				return false;

			// If waterjumping, don't accelerate
			if ( m_flWaterJumpTime == 0 )
				return false;

			return true;
		}
	}
}
