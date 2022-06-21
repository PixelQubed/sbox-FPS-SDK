using Sandbox;
using System;

namespace Amper.Source1;

public struct MoveData
{
	public float MaxSpeed;

	public Vector3 Position;
	public Vector3 Velocity;
	public QAngle ViewAngles;

	public float ForwardMove;
	public float SideMove;
	public float UpMove;
}

public struct QAngle
{
	public float Pitch, Yaw, Roll;

	public float x { get => Pitch; set { Pitch = value; } }
	public float y { get => Yaw; set { Yaw = value; } }
	public float z { get => Roll; set { Roll = value; } }

	public QAngle() : this( 0, 0, 0 ) { }
	public QAngle( float x, float y, float z )
	{
		Pitch = x;
		Yaw = y;
		Roll = z;
	}

	public void AngleVectors( out Vector3 forward, out Vector3 right, out Vector3 up )
	{
		float sr, sp, sy, cr, cp, cy;

		var pitchRad = Pitch.DegreeToRadian();
		var yawRad = Yaw.DegreeToRadian();
		var rollRad = Roll.DegreeToRadian();

		sp = MathF.Sin( pitchRad );
		cp = MathF.Cos( pitchRad );

		sy = MathF.Sin( yawRad );
		cy = MathF.Cos( yawRad );

		sr = MathF.Sin( rollRad );
		cr = MathF.Cos( rollRad );

		forward = new(	cp * cy, 
						cp * sy, 
						-sp );

		right = new(	-1 * sr * sp * cy + -1 * cr * -sy,
						-1 * sr * sp * sy + -1 * cr * cy,
						-1 * sr * cp );

		up = new(	cr * sp * cy + -sr * -sy,
					cr * sp * sy + -sr * cy,
					cr * cp );
	}
	public static QAngle operator +( QAngle c1, QAngle c2 )
	{
		return new QAngle( c1.x + c2.x, c1.y + c2.y, c1.z + c2.z );
	}
	public static QAngle operator -( QAngle c1, QAngle c2 )
	{
		return new QAngle( c1.x - c2.x, c1.y - c2.y, c1.z - c2.z );
	}
	public static QAngle operator -( QAngle value )
	{
		return new QAngle( 0f - value.x, 0f - value.y, 0f - value.z );
	}

	public static implicit operator QAngle( Vector3 value )
	{
		return new( value.x, value.y, 0f );
	}
}

public partial class GameMovement
{
	MoveData mv;

	WaterLevelType m_nOldWaterLevel { get; set; }
	float m_flWaterEntryTime { get; set; }
	int m_nOnLadder { get; set; }

	public GameMovement()
	{
		m_nOldWaterLevel = WaterLevelType.NotInWater;
		m_flWaterEntryTime = 0;
		m_nOnLadder = 0;

		mv = default;
	}

	enum IntervalType_t
	{
		Ground,
		Stuck,
		Ladder
	}

	public const float WATERJUMP_HEIGHT = 8;

	public float CATEGORIZE_GROUND_SURFACE_INTERVAL => 0.3f;
	public int CATEGORIZE_GROUND_SURFACE_TICK_INTERVAL => (int)(CATEGORIZE_GROUND_SURFACE_INTERVAL / Global.TickInterval);

	public float CHECK_STUCK_INTERVAL => 1;
	public int CHECK_STUCK_TICK_INTERVAL => (int)(CHECK_STUCK_TICK_INTERVAL / Global.TickInterval);

	public float CHECK_LADDER_INTERVAL => .2f;
	public int CHECK_LADDER_TICK_INTERVAL => (int)(CHECK_LADDER_TICK_INTERVAL / Global.TickInterval);

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

	public virtual void CheckParameters()
	{
		if (	Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				Player.MoveType != MoveType.MOVETYPE_NOCLIP &&
				Player.MoveType != MoveType.MOVETYPE_OBSERVER )
		{
			float spd;

			spd =	(mv.ForwardMove * mv.ForwardMove) +
					(mv.SideMove * mv.SideMove) +
					(mv.UpMove * mv.UpMove);

			// Slow down by the speed factor
			float flSpeedFactor = 1.0f;
			if ( Player.m_pSurfaceData != null )
			{
				// flSpeedFactor = player->m_pSurfaceData->game.maxSpeedFactor;
			}

			// If we have a constraint, slow down because of that too.
			// float flConstraintSpeedFactor = ComputeConstraintSpeedFactor();
			// if ( flConstraintSpeedFactor < flSpeedFactor )
			//		flSpeedFactor = flConstraintSpeedFactor;

			mv.MaxSpeed *= flSpeedFactor;

			if ( (spd != 0) && (spd > mv.MaxSpeed * mv.MaxSpeed) )
			{
				var ratio = mv.MaxSpeed / MathF.Sqrt( spd );
				mv.ForwardMove *= ratio;
				mv.SideMove *= ratio;
				mv.UpMove *= ratio;
			}
		}

		if ( Player.Flags.HasFlag( PlayerFlags.Frozen ) ||
			Player.Flags.HasFlag( PlayerFlags.OnTrain ) ||
			IsDead() )
		{
			mv.ForwardMove = 0;
			mv.SideMove = 0;
			mv.UpMove = 0;
		}

		DecayPunchAngle();

		if ( !IsDead() )
		{
			var v_angle = mv.ViewAngles;
			v_angle = v_angle + Player.ViewPunchAngle;

			// Now adjust roll angle
			if ( Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				 Player.MoveType != MoveType.MOVETYPE_NOCLIP )
			{
				mv.ViewAngles.Roll = CalcRoll( v_angle, mv.Velocity, sv_rollangle, sv_rollspeed );
			}
			else
			{
				mv.ViewAngles.Roll = 0; // v_angle[ ROLL ];
			}

			mv.ViewAngles.Pitch = v_angle.Pitch;
			mv.ViewAngles.Yaw = v_angle.Yaw;
		}
		else
		{
			// mv->m_vecAngles = mv->m_vecOldAngles;
		}

		if ( IsDead() )
		{
			Player.ViewOffset = Player.GetDeadViewHeightScaled();
		}

		if ( mv.ViewAngles.Yaw > 180 )
		{
			mv.ViewAngles.Yaw -= 360;
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

	public void ProcessMovement( Source1Player player, MoveData move )
	{
		if ( !player.IsValid() )
			return;

		Player = player;
		mv = move;

		PlayerMove();
		FinishMove();
	}

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

		mv.Velocity.z -= (ent_gravity * GetCurrentGravity() * 0.5f * Time.Delta);
		mv.Velocity.z += Player.BaseVelocity.z * Time.Delta;

		var temp = Player.BaseVelocity;
		temp.z = 0;
		Player.BaseVelocity = temp;

		CheckVelocity();
	}

	protected void CheckWaterJump()
	{
		mv.ViewAngles.AngleVectors( out var forward, out _, out _ );

		// Already water jumping.
		if ( Player.m_flWaterJumpTime > 0 ) 
			return;

		// Don't hop out if we just jumped in
		// only hop out if we are moving up
		if ( mv.Velocity.z < -180 )
			return;

		// See if we are backing up
		var flatvelocity = mv.Velocity.WithZ( 0 );

		// Must be moving
		var curspeed = flatvelocity.Length;
		flatvelocity = flatvelocity.Normal;

		// see if near an edge
		var flatforward = forward.WithZ( 0 ).Normal;

		// Are we backing into water from steps or something?  If so, don't pop forward
		if ( curspeed != 0 && Vector3.Dot( flatvelocity, flatforward ) < 0 )
			return;

		var vecStart = mv.Position + (GetPlayerMins() + GetPlayerMaxs()) * .5f;
		var vecEnd = vecStart + flatforward * 24;

		var tr = TraceBBox( vecStart, vecEnd );
		if ( tr.Fraction < 1 )
		{
			vecStart.z = mv.Position.z + GetPlayerViewOffset().z + WATERJUMP_HEIGHT;
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
					mv.Velocity.z = 256;
					Player.Flags |= PlayerFlags.FL_WATERJUMP;
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

		mv.Velocity[0] = Player.m_vecWaterJumpVel[0];
		mv.Velocity[1] = Player.m_vecWaterJumpVel[1];
	}

	protected void WaterMove()
	{
		mv.ViewAngles.AngleVectors( out var forward, out var right, out var up );

		Vector3 wishvel = 0;
		for ( int i = 0; i < 3; i++ )
		{
			wishvel[i] = forward[i] * mv.ForwardMove + right[i] * mv.SideMove;
		}

		// if we have the jump key down, move us up as well
		if ( Input.Down( InputButton.Jump ) )
		{
			wishvel[2] += mv.MaxSpeed;
		}
		// Sinking after no other movement occurs
		else if ( mv.ForwardMove == 0 && mv.SideMove == 0 && mv.UpMove == 0 )
		{
			wishvel[2] -= 60;
		}
		else  // Go straight up by upmove amount.
		{
			// exaggerate upward movement along forward as well
			float upwardMovememnt = mv.ForwardMove * forward.z * 2;
			upwardMovememnt = Math.Clamp( upwardMovememnt, 0, Player.MaxSpeed );
			wishvel[2] += mv.UpMove += upwardMovememnt;
		}

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		// Cap speed.
		if ( wishspeed > Player.MaxSpeed )
		{
			wishvel *= Player.MaxSpeed / wishspeed;
			wishspeed = Player.MaxSpeed;
		}

		// Slow us down a bit.
		wishspeed *= 0.8f;

		// Water friction
		var temp = mv.Velocity;
		var speed = temp.Length;

		var newspeed = 0f;
		if ( speed != 0 )
		{
			newspeed = speed - Time.Delta * speed * sv_friction * Player.m_surfaceFriction;
			if ( newspeed < 0.1f ) 
			{ 
				newspeed = 0;
			}

			mv.Velocity *= newspeed / speed;
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

		mv.Velocity += Player.BaseVelocity;

		// Now move
		// assume it is a stair or a slope, so press down from stepheight above
		var dest = mv.Position + mv.Velocity * Time.Delta;

		var pm = TraceBBox( mv.Position, dest );
		if ( pm.Fraction == 1 )
		{
			var start = dest;
			start[2] += Player.m_flStepSize + 1;

			pm = TraceBBox( start, dest );
			if ( !pm.StartedSolid )
			{
				// walked up the step, so just keep result and exit
				mv.Position = pm.EndPosition;
				mv.Velocity -= Player.BaseVelocity;
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
				mv.Velocity -= Player.BaseVelocity;
				return;
			}

			StepMove( dest );
		}

		mv.Velocity -= Player.BaseVelocity;
	}

	public virtual void StepMove( Vector3 dest )
	{
		var mover = new MoveHelper( mv.Position, mv.Velocity );
		mover.Trace = SetupBBoxTrace( 0, 0 );
		mover.MaxStandableAngle = sv_maxstandableangle;

		mover.TryMoveWithStep( Time.Delta, sv_stepsize );

		mv.Position = mover.Position;
		mv.Velocity = mover.Velocity;
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
		var speed = mv.Velocity.Length;
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
			mv.Velocity *= newspeed;
		}
	}

	public virtual void FinishGravity()
	{
		if ( Player.m_flWaterJumpTime != 0 )
			return;

		var ent_gravity = Player.PhysicsBody.GravityScale;
		if ( ent_gravity <= 0 )
			ent_gravity = 1;

		mv.Velocity[2] -= (ent_gravity * GetCurrentGravity() * Time.Delta * 0.5f);
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
		var currentspeed = mv.Velocity.Dot( wishdir );

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

		mv.Velocity += accelspeed * wishdir;
	}

	public virtual void AirMove()
	{
		mv.ViewAngles.AngleVectors( out var forward, out var right, out var up );

		var fmove = mv.ForwardMove;
		var smove = mv.SideMove;

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

		if ( wishspeed != 0 && wishspeed > mv.MaxSpeed )
		{
			wishvel *= mv.MaxSpeed / wishspeed;
			wishspeed = mv.MaxSpeed;
		}

		AirAccelerate( wishdir, wishspeed, sv_airaccelerate );

		mv.Velocity += Player.BaseVelocity;
		TryPlayerMove();
		mv.Velocity -= Player.BaseVelocity;
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
		var currentspeed = mv.Velocity.Dot( wishdir );

		var addspeed = wishspeed - currentspeed;
		if ( addspeed <= 0 )
			return;

		// Determine amount of acceleration.
		var accelspeed = acceleration * Time.Delta * wishspeed * Player.m_surfaceFriction;

		// Cap at addspeed
		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		mv.Velocity += accelspeed * wishdir;
	}

	public const int COORD_FRACTIONAL_BITS = 5;
	public const int COORD_DENOMINATOR = (1 << COORD_FRACTIONAL_BITS);
	public const int COORD_RESOLUTION = 1 / COORD_DENOMINATOR;

	/// <summary>
	/// Try to keep a walking player on the ground when running down slopes etc
	/// </summary>
	public virtual void StayOnGround()
	{
		var start = mv.Position;
		var end = mv.Position;

		start.z += 2;
		end.z -= Player.m_flStepSize;

		// See how far up we can go without getting stuck
		var trace = TraceBBox( mv.Position, start );
		start = trace.EndPosition;

		// Now trace down from a known safe position
		trace = TraceBBox( start, end );

		if ( trace.Fraction > 0 &&
			trace.Fraction < 1 &&
			!trace.StartedSolid &&
			trace.Normal[2] >= 0.7f )
		{
			var flDelta = MathF.Abs( mv.Position.z - trace.EndPosition.z );

			if ( flDelta > 0.5f * COORD_RESOLUTION )
			{
				mv.Position = trace.EndPosition;
			}
		}
	}



	public virtual void WalkMove()
	{
		mv.ViewAngles.AngleVectors( out var forward, out var right, out var up );
		var oldGround = Player.GroundEntity;

		var fmove = mv.ForwardMove;
		var smove = mv.SideMove;

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

		if ( wishspeed != 0 && wishspeed > Player.MaxSpeed )
		{
			wishvel *= mv.MaxSpeed / wishspeed;
			wishspeed = mv.MaxSpeed;
		}

		var acceleration = sv_accelerate;

		// if our wish speed is too low, we must increase acceleration or we'll never overcome friction
		// Reverse the basic friction calculation to find our required acceleration
		var wishspeedThreshold = 100 * sv_friction / sv_accelerate;
		if ( wishspeed > 0 && wishspeed < wishspeedThreshold )
		{
			float speed = mv.Velocity.Length;
			float flControl = (speed < sv_stopspeed) ? sv_stopspeed : speed;
			acceleration = (flControl * sv_friction) / wishspeed + 1;
		}

		mv.Velocity[2] = 0;
		Accelerate( wishdir, wishspeed, acceleration );
		mv.Velocity[2] = 0;

		// Clamp the players speed in x,y.
		float newSpeed = mv.Velocity.Length;
		if ( newSpeed > mv.MaxSpeed )
		{
			float flScale = Player.MaxSpeed / newSpeed;
			mv.Velocity[0] *= flScale;
			mv.Velocity[1] *= flScale;
		}

		mv.Velocity += Player.BaseVelocity;
		var spd = mv.Velocity.Length;

		if ( spd < 1 )
		{
			mv.Velocity = 0;
			mv.Velocity -= Player.BaseVelocity;
			return;
		}

		// first try just moving to the destination	
		var dest = Vector3.Zero;
		dest[0] = mv.Position[0] + mv.Position[0] * Time.Delta;
		dest[1] = mv.Position[1] + mv.Position[1] * Time.Delta;
		dest[2] = mv.Position[2];

		var trace = TraceBBox( mv.Position, dest );
		// didn't hit anything.
		if ( trace.Fraction == 1 )
		{
			mv.Position = trace.EndPosition;
			mv.Velocity -= Player.BaseVelocity;
			StayOnGround();
			return;
		}

		if ( oldGround == null && Player.WaterLevel == 0 )
		{
			mv.Velocity -= Player.BaseVelocity;
			return;
		}

		// If we are jumping out of water, don't do anything more.
		if ( Player.m_flWaterJumpTime != 0 ) 
		{
			mv.Velocity -= Player.BaseVelocity;
			return;
		}

		StepMove( dest );
		mv.Velocity -= Player.BaseVelocity;

		StayOnGround();
	}

	public virtual void FullWalkMove()
	{
		if ( !InWater() )
		{
			StartGravity();
		}

		// If we are leaping out of the water, just update the counters.
		if ( IsJumpingFromWater )
		{
			// Try to jump out of the water (and check to see if we still are).
			WaterJump();
			TryPlayerMove();

			CheckWater();
			return;
		}

		// If we are swimming in the water, see if we are nudging against a place we can jump up out
		//  of, and, if so, start out jump.  Otherwise, if we are not moving up, then reset jump timer to 0.
		//  Also run the swim code if we're a ghost or have the TF_COND_SWIMMING_NO_EFFECTS condition
		if ( InWater() )
		{
			FullWalkMoveUnderwater();
			return;
		}

		if ( WishJump() ) CheckJumpButton();

		// Make sure velocity is valid.
		CheckVelocity();

		if ( IsGrounded )
		{
			Velocity = Velocity.WithZ( 0 );
			Friction();
			WalkMove();
		}
		else
		{
			AirMove();
		}

		// Set final flags.
		CategorizePosition();

		// Add any remaining gravitational component if we are not in water.
		if ( !InWater() )
		{
			FinishGravity();
		}

		// If we are on ground, no downward velocity.
		if ( IsGrounded )
		{
			Velocity = Velocity.WithZ( 0 );
		}

		// Handling falling.
		CheckFalling();

		// Make sure velocity is valid.
		CheckVelocity();
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
		if ( fraction > = viewFraction )
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

	public bool InWater()
	{
		return Player.WaterLevelType > WaterLevelType.Feet;
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

	void SetupMoveData( Source1Player player )
	{
		Move.Position = player.Position;
		Move.Velocity = player.Velocity;

		Move.EyeRotation = Input.Rotation;

		Move.Forward = Input.Rotation.Forward;
		Move.Right = Input.Rotation.Right;
		Move.Up = Input.Rotation.Up;

		Move.MaxSpeed = Player.MaxSpeed;
		Move.ForwardMove = Input.Forward * Move.MaxSpeed;
		Move.RightMove = -Input.Left * Move.MaxSpeed;
		Move.UpMove = Input.Up * Move.MaxSpeed;
	}

	Source1Player Player { get; set; }

	public virtual void PlayerMove( Source1Player player )
	{
		Player = player;
		if ( !Player.IsValid() )
			return;

		ReduceTimers();
		CheckParameters();

		// remember last level type
		LastWaterLevelType = Player.WaterLevelType;

		// If we are not on ground, store how fast we are moving down
		if ( IsInAir )
		{
			Player.FallVelocity = -Velocity.z;
		}

		SimulateModifiers();
		UpdateViewOffset();
		Player.SimulateFootsteps( Position, Velocity );

		if ( IsAlive ) 
		{
			if ( !LadderMove() && Player.MoveType == MoveType.MOVETYPE_LADDER )
			{
				// Clear ladder stuff unless player is dead or riding a train
				// It will be reset immediately again next frame if necessary
				Player.MoveType = MoveType.MOVETYPE_WALK;
			}
		}

		switch ( Pawn.MoveType )
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
				FullLadderMove();
				break;

			case MoveType.MOVETYPE_OBSERVER:
				FullObserverMove();
				break;
		}
	}

	public virtual void SimulateModifiers()
	{
		SimulateDucking();
	}

	public virtual void UpdateViewOffset()
	{
		// reset x,y
		EyeLocalPosition = GetPlayerViewOffset( false );

		// this updates z offset.
		SetDuckedEyeOffset( Util.SimpleSpline( DuckProgress ) );
	}

	public virtual void SetDuckedEyeOffset( float duckFraction )
	{
		Vector3 vDuckHullMin = GetPlayerMins( true );
		Vector3 vStandHullMin = GetPlayerMins( false );

		float fMore = vDuckHullMin.z - vStandHullMin.z;

		Vector3 vecDuckViewOffset = GetPlayerViewOffset( true );
		Vector3 vecStandViewOffset = GetPlayerViewOffset( false );
		Vector3 temp = EyeLocalPosition;

		temp.z = (vecDuckViewOffset.z - fMore) * duckFraction + vecStandViewOffset.z * (1 - duckFraction);

		EyeLocalPosition = temp;
	}

	public virtual void ReduceTimers()
	{
		if ( JumpTime > 0 )
			JumpTime = Math.Max( JumpTime - Time.Delta, 0 );
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

	public virtual float GetAirSpeedCap() => 30;

	public virtual void CategorizePosition()
	{
		Player.SurfaceFriction = 1.0f;
		CheckWater();

		if ( Player.IsObserver )
			return;

		var point = Position - Vector3.Up * 2;
		var bumpOrigin = Position;

		float zvel = Velocity.z;
		bool bMovingUp = zvel > 0;
		bool bMovingUpRapidly = zvel > sv_maxnonjumpvelocity;
		float flGroundEntityVelZ = 0;

		if ( bMovingUpRapidly )
		{
			if ( IsGrounded )
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
			var trace = TraceBBox( bumpOrigin, point );
			if ( trace.Entity == null || Vector3.GetAngle( Vector3.Up, trace.Normal ) >= sv_maxstandableangle )
			{
				trace = TryTouchGroundInQuadrants( bumpOrigin, point, trace );
				if ( trace.Entity == null || Vector3.GetAngle( Vector3.Up, trace.Normal ) >= sv_maxstandableangle )
				{
					ClearGroundEntity();

					if ( Velocity.z > 0 && Player.MoveType != MoveType.MOVETYPE_NOCLIP )
					{
						Player.SurfaceFriction = 0.25f;
					}
				}
				else
				{
					UpdateGroundEntity( trace );
				}
			}
			else
			{
				UpdateGroundEntity( trace );
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
		if ( GroundEntity == null ) 
			return;

		GroundEntity = null;
		GroundNormal = Vector3.Up;
		Player.SurfaceFriction = 1.0f;
	}

	public Entity TestPlayerPosition( Vector3 pos, ref TraceResult pm )
	{
		pm = TraceBBox( pos, pos );
		return pm.Entity;
	}

	public virtual void CategorizeGroundSurface( TraceResult pm )
	{
		Player.SurfaceData = pm.Surface;
		Player.SurfaceFriction = pm.Surface.Friction;

		Player.SurfaceFriction *= 1.25f;
		if ( Player.SurfaceFriction > 1.0f )
			Player.SurfaceFriction = 1.0f;
	}

	public bool IsOnGround() => Player.GroundEntity != null;
	public bool IsInAir() => !IsOnGround();

	protected virtual void ShowDebugOverlay()
	{
		if ( sv_debug_movement && Player.Client.IsListenServerHost && Host.IsServer ) 
		{
			DebugOverlay.ScreenText( 
				$"[PLAYER]\n" +
				$"LifeState             {Player.LifeState}\n" +
				$"TeamNumber            {Player.TeamNumber}\n" +
				$"LastAttacker          {Player.LastAttacker}\n" +
				$"LastAttackerWeapon    {Player.LastAttackerWeapon}\n" +
				$"GroundEntity          {Player.GroundEntity}\n" +
				$"\n" +

				$"[MOVEMENT]\n" +
				$"Direction             {new Vector3( Input.Forward, -Input.Left, Input.Up )}\n" +
				$"WishVelocity          {WishVelocity}\n" +
				$"SurfaceFriction       {Player.SurfaceFriction}\n" +
				$"MoveType              {Player.MoveType}\n" +
				$"Speed                 {Velocity.Length}\n" +
				$"MaxSpeed              {Player.MaxSpeed}\n" +
				$"Fall Velocity         {Player.FallVelocity}\n" +
				$"\n" +

				$"[DUCKING]\n" +
				$"IsDucked              {Player.IsDucked}\n" +
				$"IsDucking             {IsDucking}\n" +
				$"DuckTime              {DuckTime}\n" +
				$"\n" +

				$"[OBSERVER]\n" +
				$"ObserverMode          {Player.ObserverMode}\n" +
				$"LastObserverMode      {Player.LastObserverMode}\n" +
				$"ForcedObserverMode    {Player.IsForcedObserverMode}\n" +
				$"ObserverTarget        {Player.ObserverTarget}",
				new Vector2( 60, 250 ) );
		}
	}

	[ConVar.Replicated] public static bool sv_debug_movement { get; set; }
}
