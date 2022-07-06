using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

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

public partial class GameMovement
{
	protected Source1Player Player;
	protected MoveData Move;

	public virtual void ProcessMovement( Source1Player player, ref MoveData move )
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
		ReduceTimers();

		if ( CanStuck() )
		{
			if ( CheckStuck() )
				return;
		}

		UpdateViewOffset();
		SimulateModifiers();
		Player.SimulateFootsteps( Move.Position, Move.Velocity );

		switch ( Player.MoveType )
		{
			case MoveType.MOVETYPE_ISOMETRIC:
			case MoveType.MOVETYPE_WALK:
				FullWalkMove();
				break;

			case MoveType.MOVETYPE_NOCLIP:
				FullNoClipMove(sv_noclip_speed, sv_noclip_accelerate);
				break;
		}
	}

	public virtual void UpdateViewOffset()
	{
		// reset x,y
		Player.EyeLocalPosition = GetPlayerViewOffset( false );

		if ( Player.m_flDuckTime == 0 )
			return;

		var duckProgress = Math.Clamp( Player.m_flDuckTime / TimeToDuck, 0, 1 );

		// this updates z offset.
		SetDuckedEyeOffset( Util.SimpleSpline( duckProgress ) );
	}

	public virtual void SimulateModifiers()
	{
		SimulateDucking();
	}

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

	public virtual void ReduceTimers()
	{
		float frame_msec = 1000.0f * Time.Delta;
		int nFrameMsec = (int)frame_msec;

		if ( Player.m_nJumpTimeMsecs > 0 )
		{
			Player.m_nJumpTimeMsecs -= nFrameMsec;
			if ( Player.m_nJumpTimeMsecs < 0 )
				Player.m_nJumpTimeMsecs = 0;
		}

		if ( Player.m_flSwimSoundTime > 0 )
		{
			Player.m_flSwimSoundTime -= frame_msec;
			if ( Player.m_flSwimSoundTime < 0 )
				Player.m_flSwimSoundTime = 0;
		}
	}

	protected void ReduceTimer( ref int timer, int delta )
	{
		if ( timer > 0 )
		{
			timer -= delta;
			if ( timer < 0 )
				timer = 0;
		}
	}

	/// <summary>
	/// Add our wish direction and speed onto our velocity
	/// </summary>
	public virtual void AirAccelerate( Vector3 wishdir, float wishspeed, float accel )
	{
		if ( !CanAccelerate() )
			return;

		var wishspd = wishspeed;

		if ( wishspd > AirSpeedCap )
			wishspd = AirSpeedCap;

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

	public virtual bool CanAccelerate()
	{
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
	public virtual void CategorizePosition()
	{
		Player.m_surfaceFriction = 1.0f;
		// CheckWater();

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
			SetGroundEntity( null );
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
					SetGroundEntity( null );

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
		if ( pm.Entity != null && pm.Normal.z < 0.7f )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the +x, +y quadrant
		maxs = maxsSrc;
		mins = new( MathF.Max( 0, minsSrc.x ), MathF.Max( 0, minsSrc.y ), minsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && pm.Normal.z < 0.7f )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the -x, +y quadrant
		mins = new( minsSrc.x, MathF.Max( 0, minsSrc.y ), minsSrc.z );
		maxs = new( MathF.Min( 0, maxsSrc.x ), maxsSrc.y, maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && pm.Normal.z < 0.7f )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the +x, -y quadrant
		mins = new( MathF.Max( 0, minsSrc.x ), minsSrc.y, minsSrc.z );
		maxs = new( maxsSrc.x, MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && pm.Normal.z < 0.7f )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		pm.Fraction = fraction;
		pm.EndPosition = endpos;
		return pm;
	}

	public virtual void CheckParameters()
	{
		if ( Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				Player.MoveType != MoveType.MOVETYPE_NOCLIP &&
				Player.MoveType != MoveType.MOVETYPE_OBSERVER )
		{
			float spd = (Move.ForwardMove * Move.ForwardMove) +
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

		DecayViewPunchAngle();

		if ( !Player.IsAlive ) 
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

		if ( Move.ViewAngles.Yaw > 180 )
		{
			Move.ViewAngles.Yaw -= 360;
		}
	}
}
