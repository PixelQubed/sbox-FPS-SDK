using Sandbox;
using System;

namespace Amper.Source1;

partial class Source1Player
{
	//
	// View Angles
	//
	Angles? _forceViewAngles { get; set; }

	public void ForceViewAngles( Angles angles )
	{
		EyeRotation = angles.ToRotation();
		if ( IsServer ) ForceViewAnglesRPC( angles );
		if ( IsClient ) _forceViewAngles = angles;
	}

	[ClientRpc]
	private void ForceViewAnglesRPC( Angles angles )
	{
		ForceViewAngles( angles );
	}

	[Net, Predicted] public float MaxSpeed { get; set; }
	[Net, Predicted] public new NativeMoveType MoveType { get; set; }
	public float SurfaceFriction { get; set; } = 1;
	public Surface SurfaceData { get; set; }

	//
	// Jumps and Air Dash
	//

	public virtual int MaxAirDashes => 0;
	[Net, Predicted] public int AirDashCount { get; set; }


	//
	// Ducking
	//

	public virtual int MaxAirDucks => 1;
	public bool IsDucking => DuckTime > 0;
	public float DuckProgress => Math.Clamp( DuckTime / GameRules.Current.Movement.TimeToDuck, 0, 1 );
	[Net, Predicted] public float DuckTime { get; set; }
	[Net, Predicted] public float DuckSpeed { get; set; }
	[Net, Predicted] public bool IsDucked { get; set; }
	[Net, Predicted] public int AirDuckCount { get; set; }
	[Net, Predicted] public float LastDuckTime { get; set; }

	//
	// Water
	//

	[Net, Predicted] public float NextSwimSoundTime { get; set; }
	[Net, Predicted] public WaterLevelType WaterLevelType { get; set; }
	[Net, Predicted] public Vector3 WaterJumpVelocity { get; set; }
	[Net, Predicted] public float WaterJumpTime { get; set; }
	[Net, Predicted] public float WaterEntryTime { get; set; }
	public bool IsJumpingFromWater => WaterJumpTime != 0;

	//
	// Stuck
	//

	public int LastStuckOffsetIndex { get; set; }
	public float[] LastStuckCheckTime { get; set; } = new float[2];


	public float m_flStepSize => 18;

	[Net, Predicted] public PlayerFlags Flags { get; set; }
	
	public void AddFlags( PlayerFlags flag ) { Flags |= flag; }
	public void RemoveFlag( PlayerFlags flag ) { Flags &= ~flag; }


	[ConVar.Replicated] public static bool mp_freeze_on_round_start { get; set; } = true;

	public virtual bool CanMove()
	{
		if ( GameRules.Current.IsWaitingForPlayers )
			return true;

		if ( mp_freeze_on_round_start )
		{
			if ( GameRules.Current.IsRoundStarting )
				return false;
		}

		return true;
	}

	public virtual bool CanJump()
	{
		if ( !IsAlive )
			return false;

		if ( ActiveWeapon.IsValid() && !ActiveWeapon.CanOwnerJump() )
			return false;

		if ( IsInAir )
			return false;

		return true;
	}

	public virtual bool CanAirDash()
	{
		if ( !IsAlive )
			return false;

		if ( ActiveWeapon.IsValid() && !ActiveWeapon.CanOwnerAirDash() )
			return false;

		// Air dash can only be executed in air.
		if ( IsGrounded )
			return false;

		if ( AirDashCount >= MaxAirDashes )
			return false;

		return true;
	}

	public virtual bool CanDuck()
	{
		if ( !IsAlive )
			return false;

		if ( ActiveWeapon.IsValid() && !ActiveWeapon.CanOwnerDuck() )
			return false;

		if ( IsInAir )
		{
			if ( AirDuckCount >= MaxAirDucks )
				return false;
		}

		return true;
	}

	public virtual bool CanUnduck()
	{
		if ( !IsAlive )
			return false;

		return true;
	}
}

[Flags]
public enum PlayerFlags
{
	FL_FROZEN = 1 << 0,
	FL_ONTRAIN = 1 << 1,
	FL_WATERJUMP = 1 << 2
}

public enum NativeMoveType
{
	None,
	Isometric,
	Walk,
	Step,
	Fly,
	FlyGravity,
	Physics,
	Push,
	NoClip,
	Ladder,
	Observer
}
