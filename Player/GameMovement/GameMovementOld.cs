using Sandbox;
using System;

namespace Amper.Source1;

#if false
public partial class GameMovementOld
{
	MoveData Move;
	Source1Player Player;

	public void ProcessMovement( Source1Player player, ref MoveData move )
	{
		// Check if we've supplied proper player.
		if ( !player.IsValid() )
			return;

		// Store variables.
		Player = player;
		Move = move;

		// Do movement.
		PlayerMove();
		ShowDebugOverlay();

		move = Move;
	}

	public virtual void PlayerMove()
	{
		CheckParameters();
		// ReduceTimers();

		/*

		if ( Player.MoveType != MoveType.MOVETYPE_NOCLIP &&
			Player.MoveType != MoveType.None &&
			Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
			Player.MoveType != MoveType.MOVETYPE_OBSERVER &&
			Player.IsAlive )
		{
			if ( CheckInterval( IntervalType_t.Stuck ) )
			{
				//if ( CheckStuck() )
				{
					//return;
				}
			}
		}

		if ( mv.Velocity.z > 250 )
		{
			ClearGroundEntity();
		}

		// remember last level type
		m_nOldWaterLevel = Player.WaterLevelType;

		// If we are not on ground, store how fast we are moving down
		if ( Player.GroundEntity == null )
		{
			Player.m_flFallVelocity = -mv.Velocity[2];
		}

		SimulateModifiers();
		Player.SimulateFootsteps( mv.Position, mv.Velocity );*/

		/*
		if ( IsAlive ) 
		{
			if ( !LadderMove() && Player.MoveType == MoveType.MOVETYPE_LADDER )
			{
				// Clear ladder stuff unless player is dead or riding a train
				// It will be reset immediately again next frame if necessary
				Player.MoveType = MoveType.MOVETYPE_WALK;
			}
		}*/
		UpdateViewOffset();

		switch ( Player.MoveType )
		{
			case MoveType.None:
				break;

			case MoveType.MOVETYPE_ISOMETRIC:
			case MoveType.MOVETYPE_WALK:
				FullWalkMove();
				break;

			case MoveType.MOVETYPE_NOCLIP:
				FullNoClipMove( sv_noclip_speed, sv_noclip_accelerate );
				break;

			case MoveType.MOVETYPE_LADDER:
				// FullLadderMove();
				break;

			case MoveType.MOVETYPE_OBSERVER:
				FullObserverMove();
				break;
		}
	}
	public virtual void CheckParameters()
	{
		if ( Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				Player.MoveType != MoveType.MOVETYPE_NOCLIP &&
				Player.MoveType != MoveType.MOVETYPE_OBSERVER )
		{
			float spd;

			spd = (Move.ForwardMove * Move.ForwardMove) +
					(Move.SideMove * Move.SideMove) +
					(Move.UpMove * Move.UpMove);

			if ( (spd != 0) && (spd > Move.MaxSpeed * Move.MaxSpeed) )
			{
				var ratio = Move.MaxSpeed / MathF.Sqrt( spd );
				Move.ForwardMove *= ratio;
				Move.SideMove *= ratio;
				Move.UpMove *= ratio;
			}
		}

		if ( Player.Flags.HasFlag( PlayerFlags.FL_FROZEN ) ||
			Player.Flags.HasFlag( PlayerFlags.FL_ONTRAIN ) ||
			IsDead() )
		{
			Move.ForwardMove = 0;
			Move.SideMove = 0;
			Move.UpMove = 0;
		}

		DecayPunchAngle();

		if ( !IsDead() )
		{
			var v_angle = Move.ViewAngles;
			v_angle = v_angle + Player.ViewPunchAngle;

			// Now adjust roll angle
			if ( Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				 Player.MoveType != MoveType.MOVETYPE_NOCLIP )
			{
				// Move.ViewAngles.Roll = CalcRoll( v_angle, Move.Velocity, sv_rollangle, sv_rollspeed );
			}
			else
			{
				Move.ViewAngles.Roll = 0; // v_angle[ ROLL ];
			}

			Move.ViewAngles.Pitch = v_angle.Pitch;
			Move.ViewAngles.Yaw = v_angle.Yaw;
		}
		else
		{
			// mv->m_vecAngles = mv->m_vecOldAngles;
		}

		if ( IsDead() )
		{
			Player.ViewOffset = Player.GetDeadViewHeightScaled();
		}

		if ( Move.ViewAngles.Yaw > 180 )
		{
			Move.ViewAngles.Yaw -= 360;
		}
	}

	public virtual void ReduceTimers()
	{
		var frame_msec = 1000 * Time.Delta;

		if ( Player.m_flDucktime > 0 )
		{
			Player.m_flDucktime -= frame_msec;
			if ( Player.m_flDucktime < 0 )
			{
				Player.m_flDucktime = 0;
			}
		}

		if ( Player.m_flDuckJumpTime > 0 )
		{
			Player.m_flDuckJumpTime -= frame_msec;
			if ( Player.m_flDuckJumpTime < 0 )
			{
				Player.m_flDuckJumpTime = 0;
			}
		}

		if ( Player.m_flJumpTime > 0 )
		{
			Player.m_flJumpTime -= frame_msec;
			if ( Player.m_flJumpTime < 0 )
			{
				Player.m_flJumpTime = 0;
			}
		}

		if ( Player.m_flSwimSoundTime > 0 )
		{
			Player.m_flSwimSoundTime -= frame_msec;
			if ( Player.m_flSwimSoundTime < 0 )
			{
				Player.m_flSwimSoundTime = 0;
			}
		}
	}

	int GetCheckInterval( IntervalType_t type )
	{
		int tickInterval = 1;
		switch ( type )
		{
			default:
				tickInterval = 1;
				break;

			case IntervalType_t.Ground:
				tickInterval = CATEGORIZE_GROUND_SURFACE_TICK_INTERVAL;
				break;

			case IntervalType_t.Stuck:

				// If we are in the process of being "stuck", then try a new position every command tick until m_StuckLast gets reset back down to zero
				if ( Player.m_StuckLast != 0 )
				{
					tickInterval = 1;
				}
				else
				{
					tickInterval = CHECK_STUCK_TICK_INTERVAL;
				}

				break;

			case IntervalType_t.Ladder:
				tickInterval = CHECK_LADDER_TICK_INTERVAL;
				break;
		}
		return tickInterval;
	}

	bool CheckInterval( IntervalType_t type )
	{
		int tickInterval = GetCheckInterval( type );
		return (Time.Tick + Player.NetworkIdent) % tickInterval == 0;
	}

	public virtual void CategorizeGroundSurface( TraceResult pm )
	{
		Player.m_pSurfaceData = pm.Surface;
		Player.m_surfaceFriction = pm.Surface.Friction;

		// HACKHACK: Scale this to fudge the relationship between vphysics friction values and player friction values.
		// A value of 0.8f feels pretty normal for vphysics, whereas 1.0f is normal for players.
		// This scaling trivially makes them equivalent.  REVISIT if this affects low friction surfaces too much.
		Player.m_surfaceFriction *= 1.25f;
		if ( Player.m_surfaceFriction > 1.0f )
			Player.m_surfaceFriction = 1.0f;
	}

	bool IsDead() => Player.Health <= 0 && !Player.IsAlive;

	// CGameMovement::ComputeConstraintSpeedFactor



	public void FinishMove()
	{

	}

	public const float PUNCH_DAMPING = 9;
	public const float PUNCH_SPRING_CONSTANT = 65;

	public void DecayPunchAngle()
	{
		if ( Player.ViewPunchAngle.LengthSquared > 0.001f || Player.ViewPunchAngleVelocity.LengthSquared > 0.001f )
		{
			Player.ViewPunchAngle += Player.ViewPunchAngleVelocity * Time.Delta;
			float damping = 1 - (PUNCH_DAMPING * Time.Delta);

			if ( damping < 0 )
			{
				damping = 0;
			}

			Player.ViewPunchAngleVelocity *= damping;

			float springForceMagnitude = PUNCH_SPRING_CONSTANT * Time.Delta;
			springForceMagnitude = Math.Clamp( springForceMagnitude, 0, 2 );
			Player.ViewPunchAngleVelocity -= Player.ViewPunchAngle * springForceMagnitude;

			// don't wrap around
			Player.ViewPunchAngle = new Vector3(
				Math.Clamp( Player.ViewPunchAngle.x, -89, 89 ),
				Math.Clamp( Player.ViewPunchAngle.y, -179, 179 ),
				Math.Clamp( Player.ViewPunchAngle.z, -89, 89 ) );
		}
		else
		{
			Player.ViewPunchAngle = 0;
			Player.ViewPunchAngleVelocity = 0;
		}
	}

	public virtual void StartGravity()
	{
		float ent_gravity = Player.PhysicsBody.GravityScale;
		if ( ent_gravity <= 0 )
			ent_gravity = 1;

		Move.Velocity.z -= (ent_gravity * GetCurrentGravity() * 0.5f * Time.Delta);
		Move.Velocity.z += Player.BaseVelocity.z * Time.Delta;

		var temp = Player.BaseVelocity;
		temp.z = 0;
		Player.BaseVelocity = temp;

		CheckVelocity();
	}

	protected void CheckWaterJump()
	{
		Move.ViewAngles.AngleVectors( out var forward, out _, out _ );

		// Already water jumping.
		if ( Player.m_flWaterJumpTime > 0 ) 
			return;

		// Don't hop out if we just jumped in
		// only hop out if we are moving up
		if ( Move.Velocity.z < -180 )
			return;

		// See if we are backing up
		var flatvelocity = Move.Velocity.WithZ( 0 );

		// Must be moving
		var curspeed = flatvelocity.Length;
		flatvelocity = flatvelocity.Normal;

		// see if near an edge
		var flatforward = forward.WithZ( 0 ).Normal;

		// Are we backing into water from steps or something?  If so, don't pop forward
		if ( curspeed != 0 && Vector3.Dot( flatvelocity, flatforward ) < 0 )
			return;

		var vecStart = Move.Position + (GetPlayerMins() + GetPlayerMaxs()) * .5f;
		var vecEnd = vecStart + flatforward * 24;

		var tr = TraceBBox( vecStart, vecEnd );
		if ( tr.Fraction < 1 )
		{
			vecStart.z = Move.Position.z + GetPlayerViewOffset().z + WATERJUMP_HEIGHT;
			vecEnd = vecStart + flatforward * 24;
			Player.m_vecWaterJumpVel = tr.Normal * -50;

			tr = TraceBBox( vecStart, vecEnd );
			if ( tr.Fraction == 1 )
			{

				// Now trace down to see if we would actually land on a standable surface.
				vecStart = vecEnd;
				vecEnd.z -= 1024;

				tr = TraceBBox( vecStart, vecEnd );
				if ( tr.Fraction < 1 && tr.Normal.z >= 0.7f )
				{
					Move.Velocity.z = 256;
					Player.AddFlags( PlayerFlags.FL_WATERJUMP );
					Player.m_flWaterJumpTime = 2000;
				}
			}
		}
	}

	public virtual void WaterJump()
	{
		if ( Player.m_flWaterJumpTime > 10000 )
			Player.m_flWaterJumpTime = 10000;

		if ( Player.m_flWaterJumpTime == 0 )
			return;

		Player.m_flWaterJumpTime -= Time.Delta * 1000;

		if ( Player.m_flWaterJumpTime <= 0 || Player.WaterLevelType == WaterLevelType.NotInWater )
		{
			Player.m_flWaterJumpTime = 0;
			Player.RemoveFlag( PlayerFlags.FL_WATERJUMP );
		}

		Move.Velocity[0] = Player.m_vecWaterJumpVel[0];
		Move.Velocity[1] = Player.m_vecWaterJumpVel[1];
	}

	protected void WaterMove()
	{
		Move.ViewAngles.AngleVectors( out var forward, out var right, out var up );

		Vector3 wishvel = 0;
		for ( int i = 0; i < 3; i++ )
		{
			wishvel[i] = forward[i] * Move.ForwardMove + right[i] * Move.SideMove;
		}

		// if we have the jump key down, move us up as well
		if ( Input.Down( InputButton.Jump ) )
		{
			wishvel.z += Move.MaxSpeed;
		}
		// Sinking after no other movement occurs
		else if ( Move.ForwardMove == 0 && Move.SideMove == 0 && Move.UpMove == 0 )
		{
			wishvel.z -= 60;
		}
		else  // Go straight up by upmove amount.
		{
			// exaggerate upward movement along forward as well
			float upwardMovememnt = Move.ForwardMove * forward.z * 2;
			upwardMovememnt = Math.Clamp( upwardMovememnt, 0, Move.MaxSpeed );
			wishvel[2] += Move.UpMove += upwardMovememnt;
		}

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		// Cap speed.
		if ( wishspeed > Move.MaxSpeed )
		{
			wishvel *= Move.MaxSpeed / wishspeed;
			wishspeed = Move.MaxSpeed;
		}

		// Slow us down a bit.
		wishspeed *= 0.8f;

		// Water friction
		var temp = Move.Velocity;
		var speed = temp.Length;

		var newspeed = 0f;
		if ( speed != 0 )
		{
			newspeed = speed - Time.Delta * speed * sv_friction * Player.m_surfaceFriction;
			if ( newspeed < 0.1f ) 
			{ 
				newspeed = 0;
			}

			Move.Velocity *= newspeed / speed;
		}
		else
		{
			newspeed = 0;
		}

		// water acceleration
		if ( wishspeed >= .1f )
		{
			var addspeed = wishspeed - newspeed;
			if ( addspeed > 0 )
			{
				wishvel = wishvel.Normal;

				var accelspeed = sv_accelerate * wishspeed * Time.Delta * Player.m_surfaceFriction;
				if ( accelspeed > addspeed ) 
				{
					accelspeed = addspeed;
				}

				for ( int i = 0; i < 3; i++ )
				{
					float deltaSpeed = accelspeed * wishvel[i];
				}
			}
		}

		Move.Velocity += Player.BaseVelocity;

		// Now move
		// assume it is a stair or a slope, so press down from stepheight above
		var dest = Move.Position + Move.Velocity * Time.Delta;

		var pm = TraceBBox( Move.Position, dest );
		if ( pm.Fraction == 1 )
		{
			var start = dest;
			start[2] += Player.m_flStepSize + 1;

			pm = TraceBBox( start, dest );
			if ( !pm.StartedSolid )
			{
				// walked up the step, so just keep result and exit
				Move.Position = pm.EndPosition;
				Move.Velocity -= Player.BaseVelocity;
				return;
			}

			// Try moving straight along out normal path.
			TryPlayerMove();
		}
		else
		{
			if ( Player.GroundEntity == null ) 
			{
				TryPlayerMove();
				Move.Velocity -= Player.BaseVelocity;
				return;
			}

			StepMove( dest, pm );
		}

		Move.Velocity -= Player.BaseVelocity;
	}

	public virtual void StepMove( Vector3 dest, TraceResult trace )
	{
		var vecEndPos = dest;

		// Try sliding forward both on ground and up 16 pixels
		//  take the move that goes farthest
		var vecPos = Move.Position;
		var vecVel = Move.Velocity;

		// Slide move down.
		TryPlayerMove( vecEndPos, trace );

		// Down results.
		var vecDownPos = Move.Position;
		var vecDownVel = Move.Velocity;

		// Reset original values.
		Move.Position = vecPos;
		Move.Velocity = vecVel;

		// Move up a stair height.
		vecEndPos = Move.Position;
		vecEndPos.z += Player.m_flStepSize + DIST_EPSILON;

		trace = TraceBBox( Move.Position, vecEndPos );
		Move.Position = trace.EndPosition;

		// Slide move up.
		TryPlayerMove();

		// Move down a stair (attempt to).
		vecEndPos = Move.Position;
		vecEndPos.z -= Player.m_flStepSize + DIST_EPSILON;

		trace = TraceBBox( Move.Position, vecEndPos );

		// If we are not on the ground any more then use the original movement attempt.
		if ( trace.Normal.z < 0.7f )
		{
			Move.Position = vecDownPos;
			Move.Velocity = vecDownVel;
			return;
		}

		// If the trace ended up in empty space, copy the end over to the origin.
		if ( !trace.StartedSolid /* && !trace.allsolid */)
		{
			Move.Position = trace.EndPosition;
		}

		// Copy this origin to up.
		var vecUpPos = Move.Position;

		// decide which one went farther
		float flDownDist = (vecDownPos.x - vecPos.x) * (vecDownPos.x - vecPos.x) + (vecDownPos.y - vecPos.y) * (vecDownPos.y - vecPos.y);
		float flUpDist = (vecUpPos.x - vecPos.x) * (vecUpPos.x - vecPos.x) + (vecUpPos.y - vecPos.y) * (vecUpPos.y - vecPos.y);
		if ( flDownDist > flUpDist )
		{
			Move.Position = vecDownPos;
			Move.Velocity = vecDownVel;
		}
		else
		{
			// copy z value from slide move
			Move.Velocity.z = vecDownVel.z;
		}
	}

	/// <summary>
	/// Remove ground friction from velocity
	/// </summary>
	public virtual void Friction()
	{
		// If we are in water jump cycle, don't apply friction
		if ( Player.m_flWaterJumpTime != 0 )
			return;

		// Calculate speed
		var speed = Move.Velocity.Length;
		if ( speed < 0.1f )
			return;

		var drop = 0f;

		if ( IsOnGround() )
		{
			var friction = sv_friction * Player.m_surfaceFriction;
			var control = (speed < sv_stopspeed) ? sv_stopspeed : speed;

			// Add the amount to the drop amount.
			drop += control * friction * Time.Delta;
		}

		// scale the velocity
		float newspeed = speed - drop;
		if ( newspeed < 0 )
			newspeed = 0;

		if ( newspeed != speed )
		{
			newspeed /= speed;
			Move.Velocity *= newspeed;
		}
	}

	public virtual void FinishGravity()
	{
		if ( Player.m_flWaterJumpTime != 0 )
			return;

		var ent_gravity = Player.PhysicsBody.GravityScale;
		if ( ent_gravity <= 0 )
			ent_gravity = 1;

		Move.Velocity[2] -= (ent_gravity * GetCurrentGravity() * Time.Delta * 0.5f);
		CheckVelocity();
	}

	/// <summary>
	/// Add our wish direction and speed onto our velocity
	/// </summary>
	public virtual void AirAccelerate( Vector3 wishdir, float wishspeed, float accel )
	{
		var wishspd = wishspeed;

		if ( IsDead() )
			return;

		if ( Player.m_flWaterJumpTime != 0 )
			return;

		if ( wishspd > GetAirSpeedCap() ) 
			wishspd = GetAirSpeedCap();

		// See if we are changing direction a bit
		var currentspeed = Move.Velocity.Dot( wishdir );

		// Reduce wishspeed by the amount of veer.
		var addspeed = wishspd - currentspeed;

		// If not going to add any speed, done.
		if ( addspeed <= 0 )
			return;

		// Determine amount of acceleration.
		var accelspeed = accel * wishspeed * Time.Delta * Player.m_surfaceFriction;

		// Cap at addspeed
		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Move.Velocity += accelspeed * wishdir;
	}

	public virtual void AirMove()
	{
		Move.ViewAngles.AngleVectors( out var forward, out var right, out var up );

		var fmove = Move.ForwardMove;
		var smove = Move.SideMove;

		forward[2] = 0;
		right[2] = 0;
		forward = forward.Normal;
		right = right.Normal;

		Vector3 wishvel = 0;
		for ( var i = 0; i < 2; i++ )
			wishvel[i] = forward[i] * fmove + right[i] * smove;
		wishvel[2] = 0;

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		if ( wishspeed != 0 && wishspeed > Move.MaxSpeed )
		{
			wishvel *= Move.MaxSpeed / wishspeed;
			wishspeed = Move.MaxSpeed;
		}

		AirAccelerate( wishdir, wishspeed, sv_airaccelerate );

		Move.Velocity += Player.BaseVelocity;
		TryPlayerMove();
		Move.Velocity -= Player.BaseVelocity;
	}

	public virtual bool CanAccelerate()
	{
		if ( IsDead() ) 
			return false;

		if ( Player.m_flWaterJumpTime != 0 )
			return false;

		return true;
	}

	/// <summary>
	/// Add our wish direction and speed onto our velocity
	/// </summary>
	public virtual void Accelerate( Vector3 wishdir, float wishspeed, float acceleration )
	{
		if ( !CanAccelerate() )
			return;

		// See if we are changing direction a bit
		var currentspeed = Move.Velocity.Dot( wishdir );

		var addspeed = wishspeed - currentspeed;
		if ( addspeed <= 0 )
			return;

		// Determine amount of acceleration.
		var accelspeed = acceleration * Time.Delta * wishspeed * Player.m_surfaceFriction;

		// Cap at addspeed
		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Move.Velocity += accelspeed * wishdir;
	}

	public const int COORD_FRACTIONAL_BITS = 5;
	public const int COORD_DENOMINATOR = (1 << COORD_FRACTIONAL_BITS);
	public const int COORD_RESOLUTION = 1 / COORD_DENOMINATOR;

	/// <summary>
	/// Try to keep a walking player on the ground when running down slopes etc
	/// </summary>
	public virtual void StayOnGround()
	{
		var start = Move.Position;
		var end = Move.Position;

		start.z += 2;
		end.z -= Player.m_flStepSize;

		// See how far up we can go without getting stuck
		var trace = TraceBBox( Move.Position, start );
		start = trace.EndPosition;

		// Now trace down from a known safe position
		trace = TraceBBox( start, end );

		if ( trace.Fraction > 0 &&
			trace.Fraction < 1 &&
			!trace.StartedSolid &&
			trace.Normal[2] >= 0.7f )
		{
			var flDelta = MathF.Abs( Move.Position.z - trace.EndPosition.z );

			if ( flDelta > 0.5f * COORD_RESOLUTION )
			{
				Move.Position = trace.EndPosition;
			}
		}
	}



	public virtual void WalkMove()
	{
		Move.ViewAngles.AngleVectors( out var forward, out var right, out var up );
		var oldGround = Player.GroundEntity;

		var fmove = Move.ForwardMove;
		var smove = Move.SideMove;

		if ( forward[2] != 0 )
		{
			forward[2] = 0;
			forward = forward.Normal;
		}

		if ( right[2] != 0 )
		{
			right[2] = 0;
			right = right.Normal;
		}

		Vector3 wishvel = 0;
		for ( int i = 0; i < 2; i++ )
			wishvel[i] = forward[i] * fmove + right[i] * smove;

		wishvel[2] = 0;

		var wishspeed = wishvel.Length;
		var wishdir = wishvel.Normal;

		//
		// Clamp to server defined max speed
		//
		if ( wishspeed != 0 && wishspeed > Move.MaxSpeed )
		{
			wishvel *= Move.MaxSpeed / wishspeed;
			wishspeed = Move.MaxSpeed;
		}

		var acceleration = sv_accelerate;

		// if our wish speed is too low, we must increase acceleration or we'll never overcome friction
		// Reverse the basic friction calculation to find our required acceleration
		var wishspeedThreshold = 100 * sv_friction / sv_accelerate;
		if ( wishspeed > 0 && wishspeed < wishspeedThreshold )
		{
			float speed = Move.Velocity.Length;
			float flControl = (speed < sv_stopspeed) ? sv_stopspeed : speed;
			acceleration = (flControl * sv_friction) / wishspeed + 1;
		}

		// Set pmove velocity
		Move.Velocity[2] = 0;
		Accelerate( wishdir, wishspeed, acceleration );
		Move.Velocity[2] = 0;

		// Clamp the players speed in x,y.
		float newSpeed = Move.Velocity.Length;
		if ( newSpeed > Move.MaxSpeed )
		{
			float flScale = Move.MaxSpeed / newSpeed;
			Move.Velocity[0] *= flScale;
			Move.Velocity[1] *= flScale;
		}

		Move.Velocity += Player.BaseVelocity;
		var spd = Move.Velocity.Length;

		if ( spd < 1 )
		{
			Move.Velocity = 0;
			Move.Velocity -= Player.BaseVelocity;
			return;
		}

		// first try just moving to the destination	
		var dest = Vector3.Zero;
		dest[0] = Move.Position[0] + Move.Velocity[0] * Time.Delta;
		dest[1] = Move.Position[1] + Move.Velocity[1] * Time.Delta;
		dest[2] = Move.Position[2];

		var trace = TraceBBox( Move.Position, dest );
		// didn't hit anything.
		if ( trace.Fraction == 1 )
		{
			Move.Position = trace.EndPosition;
			Move.Velocity -= Player.BaseVelocity;

			StayOnGround();
			return;
		}

		if ( oldGround == null && Player.WaterLevel == 0 )
		{
			Move.Velocity -= Player.BaseVelocity;
			return;
		}

		// If we are jumping out of water, don't do anything more.
		if ( Player.m_flWaterJumpTime != 0 ) 
		{
			Move.Velocity -= Player.BaseVelocity;
			return;
		}

		StepMove( dest, trace );
		Move.Velocity -= Player.BaseVelocity;

		StayOnGround();
	}


	public virtual void FullObserverMove()
	{
		var mode = Player.ObserverMode;

		if ( mode == ObserverMode.InEye || mode == ObserverMode.Chase )
		{
			var target = Player.ObserverTarget;
			if ( target != null )
			{
				Move.Position = target.Position;
				Move.ViewAngles = target.Rotation;
				Move.Velocity = target.Velocity;
			}

			return;
		}

		if ( mode != ObserverMode.Roaming )
			// don't move in fixed or death cam mode
			return;

		if ( sv_spectator_noclip )
		{
			// roam in noclip mode
			FullNoClipMove( sv_spectator_speed, sv_spectator_accelerate );
			return;
		}

		// do a full clipped free roam move:

		Move.ViewAngles.AngleVectors( out var forward, out var right, out var up );

		// Copy movement amounts
		float factor = sv_spectator_speed;
		if ( Input.Down( InputButton.Run ) )
			factor /= 2.0f;

		float fmove = Move.ForwardMove * factor;
		float smove = Move.SideMove * factor;

		forward = forward.Normal;
		right = right.Normal;

		var wishvel = Vector3.Zero;
		for ( int i = 0; i < 3; i++ )
			wishvel[i] = forward[i] * fmove + right[i] + smove;
		wishvel[2] += Move.UpMove;

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		//
		// Clamp to server defined max speed
		//

		float maxspeed = sv_maxvelocity;
		if ( wishspeed > maxspeed )
		{
			wishvel *= Move.MaxSpeed / wishspeed;
			wishspeed = maxspeed;
		}

		// Set pmove velocity, give observer 50% acceration bonus
		Accelerate( wishdir, wishspeed, sv_spectator_accelerate );

		float spd = Move.Velocity.Length;
		if ( spd < 1 )
		{
			Move.Velocity = 0;
			return;
		}

		float friction = sv_friction;

		// Add the amount to the drop amount.
		float drop = spd * friction * Time.Delta;

		// scale the velocity
		float newspeed = spd - drop;

		if ( newspeed < 0 )
			newspeed = 0;

		// Determine proportion of old speed we are using.
		newspeed /= spd;

		Move.Velocity *= newspeed;
		CheckVelocity();

		TryPlayerMove();
	}

	public virtual void FullNoClipMove( float factor, float maxacceleration )
	{
		float maxspeed = sv_maxspeed * factor;
		Move.ViewAngles.AngleVectors( out var forward, out var right, out var up );

		if ( Input.Down( InputButton.Run ) )
			factor /= 2.0f;

		float fmove = Move.ForwardMove * factor;
		float smove = Move.SideMove * factor;

		forward = forward.Normal;
		right = right.Normal;

		var wishvel = Vector3.Zero;
		for ( int i = 0; i < 3; i++ )
			wishvel[i] = forward[i] * fmove + right[i] + smove;
		wishvel[2] += Move.UpMove;

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		//
		// Clamp to server defined max speed
		//
		if ( wishspeed > maxspeed )
		{
			wishvel *= maxspeed / wishspeed;
			wishspeed = maxspeed;
		}

		if ( maxacceleration > 0 )
		{
			// Set pmove velocity
			Accelerate( wishdir, wishspeed, maxacceleration );

			float spd = Move.Velocity.Length;
			if ( spd < 1 )
			{
				Move.Velocity = 0;
				return;
			}

			// Bleed off some speed, but if we have less than the bleed
			//  threshhold, bleed the theshold amount.
			float control = (spd < maxspeed / 4) ? (maxspeed / 4) : spd;

			float friction = sv_friction * Player.m_surfaceFriction;

			// Add the amount to the drop amount.
			float drop = control * friction * Time.Delta;

			// scale the velocity
			float newspeed = spd - drop;
			if ( newspeed < 0 )
				newspeed = 0;

			// Determine proportion of old speed we are using.
			newspeed /= spd;
			Move.Velocity *= newspeed;
		}
		else
		{
			Move.Velocity = wishvel;
		}

		// Just move ( don't clip or anything )
		Move.Position += Time.Delta * Move.Velocity;

		// Zero out velocity if in noaccel mode
		if ( maxacceleration < 0f )
		{
			Move.Velocity = 0;
		}
	}

	public void PlaySwimSound()
	{
		// Player.PlaySwimSound();
	}

	/// <summary>
	/// Returns true if we succesfully made a jump.
	/// </summary>
	public virtual bool CheckJumpButton()
	{
		if ( IsDead() )
			return false;

		if ( Player.m_flWaterJumpTime != 0 )
		{
			Player.m_flWaterJumpTime -= Time.Delta;
			if ( Player.m_flWaterJumpTime < 0 )
				Player.m_flWaterJumpTime = 0;

			return false;
		}

		if ( InWater() )
		{
			ClearGroundEntity();
			Move.Velocity[2] = 100;

			if ( Player.m_flSwimSoundTime <= 0 )
			{
				Player.m_flSwimSoundTime = 1000;
				PlaySwimSound();
			}

			return false;
		}

		if ( Player.GroundEntity == null )
			return false;

		if ( Player.m_bDucking && Player.Flags.HasFlag( PlayerFlags.FL_DUCKING ) )
			return false;

		if ( Player.m_flDuckJumpTime > 0 )
			return false;

		ClearGroundEntity();

		Player.DoJumpSound( Move.Position, Player.m_pSurfaceData, 1 );
		Player.SetAnimParameter( "b_jump", true );

		var startz = Move.Velocity[2];
		if ( Player.m_bDucking || Player.Flags.HasFlag( PlayerFlags.FL_DUCKING ) ) 
		{
			Move.Velocity[2] = JumpImpulse;
		}
		else
		{
			Move.Velocity[2] += JumpImpulse;
		}

		FinishGravity();
		OnJump( Move.Velocity.z - startz );

		return true;
	}

	public virtual void OnJump( float impulse ) { }

	// FullLadderMove() @ L2536

	public int TryPlayerMove( Vector3? firstDest = null, TraceResult? firstTrace = null )
	{
		TraceResult pm = default;
		var numbumps = 4;
		var blocked = 0;
		var numplanes = 0;

		var planes = new Vector3[MAX_CLIP_PLANES];

		var original_velocity = Move.Velocity;
		var primal_velocity = Move.Velocity;

		var allFraction = 0f;
		var timeLeft = Time.Delta;

		var new_velocity = Vector3.Zero;

		for ( var bumpcount = 0; bumpcount < numbumps; bumpcount++ )
		{
			if ( Move.Velocity.Length == 0 )
				break;

			// Assume we can move all the way from the current origin to the
			// end point.
			var end = Move.Position + Move.Velocity * timeLeft;

			if ( firstDest.HasValue && end == firstDest.Value )
				pm = firstTrace.Value;
			else
				pm = TraceBBox( Move.Position, end );

			allFraction += pm.Fraction;

			/*
		    // If we started in a solid object, or we were in solid space
		    //  the whole way, zero out our velocity and return that we
		    //  are blocked by floor and wall.
		    if (pm.allsolid)
		    {	
			    // entity is trapped in another solid
			    VectorCopy (vec3_origin, mv->m_vecVelocity);
			    return 4;
		    }
            */

			// If we moved some portion of the total distance, then
			//  copy the end position into the pmove.origin and 
			//  zero the plane counter.
			if ( pm.Fraction > 0 )
			{
				if ( numbumps > 0 && pm.Fraction == 1 )
				{
					// There's a precision issue with terrain tracing that can cause a swept box to successfully trace
					// when the end position is stuck in the triangle.  Re-run the test with an uswept box to catch that
					// case until the bug is fixed.
					// If we detect getting stuck, don't allow the movement
					var stuck = TraceBBox( pm.EndPosition, pm.EndPosition );
					if ( stuck.StartedSolid || stuck.Fraction != 1.0f )
					{
						Log.Info( "Player will become stuck!!!\n" );
						Move.Velocity = 0;
						break;
					}

					// actually covered some distance
					Move.Position = pm.EndPosition;
					original_velocity = Move.Velocity;
					numplanes = 0;
				}
			}

			// If we covered the entire distance, we are done
			//  and can return.
			if ( pm.Fraction == 1 )
				break; // moved the entire distance

			if ( pm.Normal.z > 0.7f )
				blocked |= 1;   // floor

			if ( pm.Normal.z == 0 )
				blocked |= 2;   // step / wall

			timeLeft -= timeLeft * pm.Fraction;

			// Did we run out of planes to clip against?
			if ( numplanes >= MAX_CLIP_PLANES )
			{
				// this shouldn't really happen
				//  Stop our movement if so.
				Move.Velocity = 0;
				break;
			}

			planes[numplanes] = pm.Normal;
			numplanes++;

			// reflect player velocity 
			// Only give this a try for first impact plane because you can get yourself stuck in an acute corner by jumping in place
			//  and pressing forward and nobody was really using this bounce/reflection feature anyway...
			if ( numplanes == 1 && Player.MoveType == MoveType.MOVETYPE_WALK && Player.GroundEntity == null )
			{
				for ( int i = 0; i < numplanes; i++ )
				{
					if ( planes[i][2] > 0.7f )
					{
						ClipVelocity( original_velocity, planes[i], out new_velocity, 1 );
						original_velocity = new_velocity;
					}
					else
					{
						ClipVelocity( original_velocity, planes[i], out new_velocity, 1 + sv_bounce * (1 - Player.m_surfaceFriction) );
					}
				}

				Move.Velocity = new_velocity;
				original_velocity = new_velocity;
			}
			else
			{
				var i = 0;
				for ( i = 0; i < numplanes; i++ )
				{
					ClipVelocity(
						original_velocity,
						planes[i],
						out Move.Velocity,
						1 );

					var j = 0;
					for ( j = 0; j < numplanes; j++ )
					{
						if ( j != i )
						{
							// Are we now moving against this plane?
							if ( Vector3.Dot( Move.Velocity, planes[j] ) < 0 )
								break;  // not ok
						}
					}

					if ( j == numplanes ) // Didn't have to clip, so we're ok
						break;
				}

				var d = 0f;
				if ( i == numplanes )
				{
					// go along the crease
					if ( numplanes != 2 )
					{
						Move.Velocity = 0;
						break;
					}

					var dir = Vector3.Cross( planes[0], planes[1] );
					dir = dir.Normal;
					d = Vector3.Dot( dir, Move.Velocity );
					Move.Velocity = d * dir;
				}

				//
				// if original velocity is against the original velocity, stop dead
				// to avoid tiny occilations in sloping corners
				//
				d = Vector3.Dot( Move.Velocity, primal_velocity );
				if ( d <= 0 )
				{
					//Con_DPrintf("Back\n");
					Move.Velocity = 0;
					break;
				}
			}
		}

		if ( allFraction == 0 )
			Move.Velocity = 0;

		/*
		// Check if they slammed into a wall
		float fSlamVol = 0.0f;

		float fLateralStoppingAmount = primal_velocity.Length2D() - mv->m_vecVelocity.Length2D();
		if ( fLateralStoppingAmount > PLAYER_MAX_SAFE_FALL_SPEED * 2.0f )
		{
			fSlamVol = 1.0f;
		}
		else if ( fLateralStoppingAmount > PLAYER_MAX_SAFE_FALL_SPEED )
		{
			fSlamVol = 0.85f;
		}

		PlayerRoughLandingEffects( fSlamVol );
		*/

		return blocked;
	}

	// OnLadder()

	// LadderMove()

	protected string DescribeAxis( int axis )
	{
		switch ( axis )
		{
			case 0: return "X";
			case 1: return "Y";
			case 2: default: return "Z";
		}
	}

	public void CheckVelocity()
	{
		for ( int i = 0; i < 3; i++ )
		{
			if ( float.IsNaN( Move.Velocity[i] ) )
			{
				Log.Info( $"Got a NaN velocity {DescribeAxis( i )}" );
				Move.Velocity[i] = 0;
			}

			if ( float.IsNaN( Move.Position[i] ) )
			{
				Log.Info( $"Got a NaN position {DescribeAxis( i )}" );
				Move.Position[i] = 0;
			}

			if ( Move.Velocity[i] > sv_maxvelocity )
			{
				Log.Info( $"Got a velocity too high on {DescribeAxis( i )}" );
				Move.Velocity[i] = sv_maxvelocity;
			}

			if ( Move.Velocity[i] < -sv_maxvelocity )
			{
				Log.Info( $"Got a velocity too low on {DescribeAxis( i )}" );
				Move.Velocity[i] = -sv_maxvelocity;
			}
		}
	}

	// AddGravity()
	// PushEntity()

	public int ClipVelocity( Vector3 vIn, Vector3 normal, out Vector3 vOut, float overbounce )
	{
		var angle = normal.z;

		var blocked = 0x00;
		if ( angle > 0 )
			blocked |= 0x01;
		if ( angle == 0 )
			blocked |= 0x02;

		var backoff = Vector3.Dot( vIn, normal ) * overbounce;

		vOut = 0;
		for ( var i = 0; i < 3; i++ )
		{
			var change = normal[i] * backoff;
			vOut[i] = vIn[i] - change;
		}

		var adjust = Vector3.Dot( vOut, normal );
		if ( adjust < 0 )
		{
			vOut -= normal * adjust;
		}

		return blocked;
	}

	float CalcRoll( QAngle angles, Vector3 velocity, float rollangle, float rollspeed )
	{
		angles.AngleVectors( out var forward, out var right, out var up );

		var side = velocity.Dot( right );
		var sign = side < 0 ? -1 : 1;
		side = MathF.Abs( side );
		var value = rollangle;

		if ( side < rollspeed )
		{
			side = side * side / rollspeed;
		}
		else
		{
			side = value;
		}

		return side * sign;
	}

	// CreateStuckTable()
	// GetRandomStuckOffsets()
	// ResetStuckOffsets()
	// CheckStuck()

	public bool InWater()
	{
		return Player.WaterLevelType > WaterLevelType.Feet;
	}

	public virtual bool CheckWater()
	{
		var vPlayerExtents = GetPlayerExtents();
		var vPlayerView = GetPlayerViewOffset();

		// Assume that we are not in water at all.
		Player.WaterLevelType = WaterLevelType.NotInWater;

		var fraction = Player.WaterLevel;
		var playerHeight = vPlayerExtents.z;
		var viewHeight = vPlayerView.z;

		var viewFraction = viewHeight / playerHeight;
		if ( fraction > viewFraction )
		{
			Player.WaterLevelType = WaterLevelType.Eyes;
		}
		else if ( fraction >= 0.5f )
		{
			Player.WaterLevelType = WaterLevelType.Waist;
		}
		else if ( fraction > 0 )
		{
			Player.WaterLevelType = WaterLevelType.Feet;
		}

		if ( m_nOldWaterLevel == WaterLevelType.NotInWater && Player.WaterLevelType != WaterLevelType.NotInWater )
		{
			m_flWaterEntryTime = Time.Now;
		}

		return Player.WaterLevelType > WaterLevelType.Feet;
	}

	/// <summary>
	/// We have a new ground entity
	/// </summary>
	public virtual void SetGroundEntity( TraceResult tr )
	{
		var newGround = tr.Entity;

		var oldGround = Player.GroundEntity;
		var vecBaseVelocity = Player.BaseVelocity;

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

		Player.BaseVelocity = vecBaseVelocity;
		Player.GroundEntity = newGround;

		// If we are on something...
		if ( newGround != null )
		{
			CategorizeGroundSurface( tr );

			// Then we are not in water jump sequence
			Player.m_flWaterJumpTime = 0;
			Move.Velocity.z = 0;
		}
	}

	public TraceResult TryTouchGroundInQuadrants( Vector3 start, Vector3 end, TraceResult pm )
	{
		Vector3 mins, maxs;
		Vector3 minsSrc = GetPlayerMins();
		Vector3 maxsSrc = GetPlayerMaxs();

		float fraction = pm.Fraction;
		Vector3 endpos = pm.EndPosition;

		// Check the -x, -y quadrant
		mins = minsSrc;
		maxs = new( MathF.Min( 0, maxsSrc.x ), MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the +x, +y quadrant
		maxs = maxsSrc;
		mins = new( MathF.Max( 0, minsSrc.x ), MathF.Max( 0, minsSrc.y ), minsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the -x, +y quadrant
		mins = new( minsSrc.x, MathF.Max( 0, minsSrc.y ), minsSrc.z );
		maxs = new( MathF.Min( 0, maxsSrc.x ), maxsSrc.y, maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the +x, -y quadrant
		mins = new( MathF.Max( 0, minsSrc.x ), minsSrc.y, minsSrc.z );
		maxs = new( maxsSrc.x, MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		pm.Fraction = fraction;
		pm.EndPosition = endpos;
		return pm;
	}

	public virtual void CategorizePosition()
	{
		Player.m_surfaceFriction = 1.0f;
		CheckWater();

		if ( Player.IsObserver )
			return;

		var offset = 2;

		var point = Move.Position + Vector3.Down * offset;
		var bumpOrigin = Move.Position;

		float zvel = Move.Velocity.z;
		bool bMovingUp = zvel > 0;
		bool bMovingUpRapidly = zvel > NON_JUMP_VELOCITY;
		float flGroundEntityVelZ = 0;
		if ( bMovingUpRapidly )
		{
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
			ClearGroundEntity();
		}
		else
		{
			// Try and move down.
			var trace = TraceBBox( bumpOrigin, point );

			// Was on ground, but now suddenly am not.  If we hit a steep plane, we are not on ground
			if ( trace.Entity == null || trace.Normal[2] < .7f )
			{
				// Test four sub-boxes, to see if any of them would have found shallower slope we could actually stand on
				trace = TryTouchGroundInQuadrants( bumpOrigin, point, trace );

				if ( trace.Entity == null || trace.Normal[2] < .7f )
				{
					ClearGroundEntity();

					if ( Move.Velocity.z > 0 && 
						Player.MoveType != MoveType.MOVETYPE_NOCLIP )
					{
						Player.m_surfaceFriction = 0.25f;
					}
				}
				else
				{
					SetGroundEntity( trace );
				}
			}
			else
			{
				SetGroundEntity( trace );
			}
		}
	}

	public void CheckFalling()
	{
		if ( Player.GroundEntity == null || Player.m_flFallVelocity <= 0 )
			return;

		// let any subclasses know that the player has landed and how hard
		OnLand( Player.FallVelocity );

		//
		// Clear the fall velocity so the impact doesn't happen again.
		//
		Player.m_flFallVelocity = 0;
	}

	public virtual void OnLand( float velocity ) { }

	public void DecayAngles( ref Vector3 angle, float exp, float lin, float time )
	{
		exp *= time;
		lin *= time;

		angle *= MathF.Exp( -exp );

		var mag = angle.Length;
		if ( mag > lin )
		{
			angle *= (1 - lin / mag);
		}
		else
		{
			angle = 0;
		}
	}

	public virtual void DecayViewPunchAngle()
	{
		var angles = Player.ViewPunchAngle;
		DecayAngles( ref angles, view_punch_decay, 0, Time.Delta );
		Player.ViewPunchAngle = angles;
	}

	[ConVar.Replicated] public static float view_punch_decay { get; set; } = 18f;


	public virtual void SimulateModifiers()
	{
		// SimulateDucking();
	}

	public void UpdateViewOffset()
	{
		if ( Player.m_flDuckJumpTime != 0 )
		{
			float flDuckMilliseconds = MathF.Max( 0.0f, GAMEMOVEMENT_DUCK_TIME - Player.m_flDuckJumpTime );
			float flDuckSeconds = flDuckMilliseconds / GAMEMOVEMENT_DUCK_TIME;
			if ( flDuckSeconds > TIME_TO_UNDUCK )
			{
				Player.m_flDuckJumpTime = 0.0f;
				SetDuckedEyeOffset( 0.0f );
			}
			else
			{
				float flDuckFraction = Util.SimpleSpline( 1.0f - (flDuckSeconds / TIME_TO_UNDUCK) );
				SetDuckedEyeOffset( flDuckFraction );
			}
		}

		SetDuckedEyeOffset( 0 );
	}

	public virtual void SetDuckedEyeOffset( float duckFraction )
	{
		Vector3 vDuckHullMin = GetPlayerMins( true );
		Vector3 vStandHullMin = GetPlayerMins( false );

		float fMore = vDuckHullMin.z - vStandHullMin.z;

		Vector3 vecDuckViewOffset = GetPlayerViewOffset( true );
		Vector3 vecStandViewOffset = GetPlayerViewOffset( false );
		Vector3 temp = Player.EyeLocalPosition;

		temp.z = ((vecDuckViewOffset.z - fMore) * duckFraction +
					vecStandViewOffset.z * (1 - duckFraction));

		Player.EyeLocalPosition = temp;
	}


	public virtual float GetAirSpeedCap() => 30;


	/// <summary>
	/// We're no longer on the ground, remove it
	/// </summary>
	public virtual void ClearGroundEntity()
	{
		if ( Player.GroundEntity == null ) 
			return;

		Player.GroundEntity = null;
		Player.m_surfaceFriction = 1.0f;
		Player.BaseVelocity = 0;
	}

	public Entity TestPlayerPosition( Vector3 pos, ref TraceResult pm )
	{
		pm = TraceBBox( pos, pos );
		return pm.Entity;
	}

	public bool IsOnGround() => Player.GroundEntity != null;
	public bool IsInAir() => !IsOnGround();


}

#endif
