using Sandbox;
using System;

namespace Amper.Source1;

public struct MoveData
{
	public float MaxSpeed;

	public Vector3 Position;
	public Vector3 Velocity;
	public Rotation EyeRotation;

	public float ForwardMove;
	public float RightMove;
	public float UpMove;

	public Vector3 Forward;
	public Vector3 Right;
	public Vector3 Up;
}

public partial class GameMovement 
{

	WaterLevelType m_nOldWaterLevel { get; set; }
	float m_flWaterEntryTime { get; set; }
	int m_nOnLadder { get; set; }
	MoveData mv { get; set; }

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

	public const float CATEGORIZE_GROUND_SURFACE_INTERVAL = 0.3f;
	public const float CATEGORIZE_GROUND_SURFACE_TICK_INTERVAL = 0.3f;

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
				if ( player->m_StuckLast != 0 )
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

	public void ProcessMovement( Source1Player player )
	{
		if ( !player.IsValid() )
			return;

		ProcessingMovement = true;
		InStuckTest = false;

		SetupMoveData( player );
		PlayerMove( player );

		ProcessingMovement = false;
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

	public virtual void CheckParameters()
	{
		if ( Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
			Player.MoveType != MoveType.MOVETYPE_NOCLIP &&
			Player.MoveType != MoveType.MOVETYPE_OBSERVER )
		{
			var speed = ForwardMove * ForwardMove + RightMove * RightMove + UpMove * UpMove;
			var maxSpeed = Player.MaxSpeed;

			if ( speed != 0 && speed > maxSpeed * maxSpeed )
			{
				var ratio = maxSpeed / MathF.Sqrt( speed );

				ForwardMove *= ratio;
				RightMove *= ratio;
				UpMove *= ratio;
			}
		}
	}

	public virtual void StepMove( Vector3 dest )
	{
		var mover = new MoveHelper( Position, Velocity );
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
		if ( IsJumpingFromWater )
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
		var speed = Velocity.Dot( wishdir );

		var addspeed = wishspeed - speed;
		if ( addspeed <= 0 )
			return;

		// Determine amount of acceleration.
		var accelspeed = acceleration * Time.Delta * wishspeed * Player.SurfaceFriction;

		// Cap at addspeed
		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Velocity += accelspeed * wishdir;
	}

	/// <summary>
	/// Add our wish direction and speed onto our velocity
	/// </summary>
	public virtual void AirAccelerate( Vector3 wishdir, float wishSpeed, float acceleration )
	{
		if ( !CanAccelerate() )
			return;

		var speedCap = GetAirSpeedCap();

		var wishSpeedCapped = wishSpeed;
		if ( wishSpeedCapped > speedCap )
			wishSpeedCapped = speedCap;

		// See if we are changing direction a bit
		var currentspeed = Velocity.Dot( wishdir );

		// Reduce wishspeed by the amount of veer.
		var addspeed = wishSpeedCapped - currentspeed;

		// If not going to add any speed, done.
		if ( addspeed <= 0 )
			return;

		// Determine amount of acceleration.
		var accelspeed = acceleration * wishSpeed * Time.Delta * Player.SurfaceFriction;

		// Cap at addspeed
		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Velocity += accelspeed * wishdir;
	}

	public virtual float GetAirSpeedCap() => 30;

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
		if ( speed < 0.1f ) 
			return;

		float control, drop = 0;
		if ( IsGrounded )
		{
			var friction = sv_friction * Player.SurfaceFriction;

			control = (speed < sv_stopspeed) ? sv_stopspeed : speed;

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
			Velocity *= newspeed;
		}
	}

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

	/// <summary>
	/// Try to keep a walking player on the ground when running down slopes etc
	/// </summary>
	public virtual void StayOnGround()
	{
		var start = Position + Vector3.Up * 2;
		var end = Position + Vector3.Down * sv_stepsize;

		// See how far up we can go without getting stuck
		var trace = TraceBBox( Position, start );
		start = trace.EndPosition;

		// Now trace down from a known safe position
		trace = TraceBBox( start, end );

		if ( trace.Fraction <= 0 ) return;
		if ( trace.Fraction >= 1 ) return;
		if ( trace.StartedSolid ) return;
		if ( Vector3.GetAngle( Vector3.Up, trace.Normal ) >= sv_maxstandableangle ) return;

		Position = trace.EndPosition;
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

	public bool IsAlive => Pawn.LifeState == LifeState.Alive;
	public bool IsDead => !IsAlive;
	public bool IsGrounded => GroundEntity != null;
	public bool IsInAir => !IsGrounded;

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
