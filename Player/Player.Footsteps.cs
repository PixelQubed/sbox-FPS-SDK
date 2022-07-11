using Sandbox;

namespace Amper.Source1;

partial class Source1Player
{
	public float StepSoundTime { get; set; }
	bool NextFootRight { get; set; }

	TimeSince TimeSinceFootstep { get; set; }

	public virtual bool CanPlayFootsteps()
	{
		// dont play footstep sounds if we're in noclip or in observer mode.
		if ( MoveType == MoveType.MOVETYPE_NOCLIP || MoveType == MoveType.MOVETYPE_OBSERVER )
			return false;

		// always cool down footsteps for at least .3 seconds.
		if ( TimeSinceFootstep < .3f )
			return false;

		// footsteps disabled serverwide.
		if ( !sv_footsteps )
			return false;

		// No footsteps while airborne.
		if ( !IsGrounded )
			return false;

		return true;
	}

	public float GetStepFrequencyForVelocity( float velocity )
	{
		return velocity.Remap( 30, 250, 1, .1f );
	}

	public float MinStepSpeed => 30;


	// 30  - 1
	// 250 - .3

	public virtual void SimulateFootsteps( Vector3 position, Vector3 velocity )
	{
		// can't play footsteps.
		if ( !CanPlayFootsteps() )
			return;

		float groundSpeed = velocity.WithZ( 0 ).Length;

		// we're moving too slow. don't play any sounds.
		if ( groundSpeed < MinStepSpeed )
			return;

		// calculate the frequency.
		var stepTime = GetStepFrequencyForVelocity( groundSpeed );
		if ( TimeSinceFootstep < stepTime )
			return;

		// To hear step sounds you must be either on a ladder or moving along the ground AND
		// You must be moving fast enough

		var volume = 1f;

		// 50% volume if ducking
		if ( IsDucked )
			volume *= 0.5f;

		DoFootstep( position, SurfaceData, volume );
		TimeSinceFootstep = 0;
	}

	public virtual void PlayStepSound( Vector3 origin, string sound, float volume = 1f )
	{
		if ( IsClient && !Prediction.FirstTime )
			return;

		if ( string.IsNullOrWhiteSpace( sound ) )
			return;

		Sound.FromEntity( sound, this ).SetVolume( volume );
	}

	[ConVar.Server] public static bool sv_debug_footstep_surfaces { get; set; }

	public virtual void DoFootstep( Vector3 origin, Surface surface, float volume = 1f )
	{
		if ( surface == null )
			return;

		if ( sv_debug_footstep_surfaces )
		{
			DebugOverlay.Sphere( origin, 3, Color.Yellow, 5, true );
			DebugOverlay.Text( surface.ResourceName, origin, 5 );
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
