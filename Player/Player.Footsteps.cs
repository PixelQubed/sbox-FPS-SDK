using Sandbox;
using System;

namespace Source1;

partial class Source1Player
{
	public Surface SurfaceData { get; set; }
	public float StepSoundTime { get; set; }
	bool NextFootRight { get; set; }

	TimeSince TimeSinceFootstep { get; set; }



	public virtual void GetStepSoundVelocities( out float velwalk, out float velrun )
	{
		if ( IsDucked || MoveType == MoveType.MOVETYPE_LADDER )
		{
			velwalk = 60;
			velrun = 80;
		} 
		else
		{
			velwalk = 90;
			velrun = 220;
		}
	}

	public virtual void SimulateFootsteps( Vector3 position, Vector3 velocity )
	{
		if ( TimeSinceFootstep < .3f )
			return;

		if ( MoveType == MoveType.MOVETYPE_NOCLIP || MoveType == MoveType.MOVETYPE_OBSERVER ) 
			return;

		if ( !sv_footsteps )
			return;

		var speed = velocity.Length;
		float groundspeed = velocity.WithZ( 0 ).Length;

		GetStepSoundVelocities( out var velwalk, out var velrun );

		bool movingalongground = groundspeed > 0.0001f;
		bool moving_fast_enough = speed >= velwalk;

		// To hear step sounds you must be either on a ladder or moving along the ground AND
		// You must be moving fast enough

		if ( !moving_fast_enough || !IsGrounded || !movingalongground ) 
			return;

		var walking = speed < velrun;

		var knee = position;
		var feet = position;

		var height = GetPlayerMaxs( IsDucked ).z - GetPlayerMins( IsDucked ).z;
		knee = knee.WithZ( position.z + .2f * height );

		var volume = 1f;

		// play the sound
		// 65% volume if ducking
		if ( IsDucked )
		{
			volume *= 0.65f;
		}

		DoFootstep( feet, SurfaceData, volume );
		TimeSinceFootstep = 0;
	}

	public virtual void PlayStepSound( Vector3 origin, string sound, float volume = 1f )
	{
		if ( IsClient && !Prediction.FirstTime )
			return;

		if ( string.IsNullOrWhiteSpace( sound ) )
			return;

		Sound.FromWorld( sound, origin ).SetVolume( volume );
	}

	[ServerVar] public static bool sv_debug_footstep_surfaces { get; set; }

	public virtual void DoFootstep( Vector3 origin, Surface surface, float volume = 1f )
	{
		if ( surface == null )
			return;

		if ( sv_debug_footstep_surfaces )
		{
			DebugOverlay.Sphere( origin, 3, Color.Yellow, true, 5 );
			DebugOverlay.Text( origin, surface.Name, 5 );
		}

		if ( !FootstepData.GetSoundsForSurface( surface, out var sounds ) )
			return;

		var right = NextFootRight;
		var soundname = right ? sounds.FootRight : sounds.FootLeft;
		NextFootRight = !NextFootRight;

		if ( string.IsNullOrWhiteSpace( soundname ) )
			return;

		PlayStepSound( origin, soundname, volume );
		OnFootstep( right, origin, soundname, volume );
	}

	public void DoLandSound( Vector3 origin, Surface surface, float volume = 1f )
	{
		if ( surface == null )
			return;

		if ( !FootstepData.GetSoundsForSurface( surface, out var sounds ) )
			return;

		if ( string.IsNullOrWhiteSpace( sounds.FootLand ) )
			return;

		PlayStepSound( origin, sounds.FootLand, volume );
		OnLandStep( origin, sounds.FootLand, volume );
	}

	public void DoJumpSound( Vector3 origin, Surface surface, float volume = 1f )
	{
		if ( surface == null )
			return;

		if ( !FootstepData.GetSoundsForSurface( surface, out var sounds ) )
			return;

		if ( string.IsNullOrWhiteSpace( sounds.FootLand ) )
			return;

		PlayStepSound( origin, sounds.FootLaunch, volume );
		OnLandStep( origin, sounds.FootLaunch, volume );
	}

	[ConVar.Replicated] public static bool sv_footsteps { get; set; } = true;
}
