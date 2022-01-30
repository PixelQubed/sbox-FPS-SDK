using Sandbox;
using System;

namespace Source1
{
	partial class Source1Player
	{
		public Surface SurfaceData { get; set; }
		public float StepSoundTime { get; set; }
		bool NextRightFoot { get; set; }

		public virtual void SimulateFootsteps()
		{
			if ( StepSoundTime > 0 )
			{
				StepSoundTime -= Time.Delta;
				if ( StepSoundTime < 0 ) StepSoundTime = 0;
			}

			if ( StepSoundTime > 0 ) return;

			// if ( GetFlags() & (FL_FROZEN|FL_ATCONTROLS))
			// return;

			if ( MoveType == MoveType.MOVETYPE_NOCLIP || MoveType == MoveType.MOVETYPE_OBSERVER )
				return;

			if ( !sv_footsteps )
				return;

			// LandFootstep( Position, SurfaceData );
			StepSoundTime += 1;
		}

		public void DoFootstep( Vector3 vecOrigin, Surface surface, float fvol = 1f )
		{
			if ( IsClient && !Prediction.FirstTime ) return;
			if ( surface == null ) return;

			var isRight = NextRightFoot;
			var stepSoundName = isRight ? surface.Sounds.FootRight : surface.Sounds.FootLeft;
			if ( string.IsNullOrWhiteSpace( stepSoundName ) )
				return;

			NextRightFoot = !NextRightFoot;

			float flVol = 1;

			Sound.FromWorld( "player.footstep.concrete", vecOrigin ).SetVolume( flVol );
			OnFootstep( isRight, vecOrigin, stepSoundName, flVol );
		}

		public void DoLandSound( Vector3 vecOrigin, Surface surface, float fvol = 1f )
		{
			if ( IsClient && !Prediction.FirstTime ) return;
			if ( surface == null ) return;

			var sound = surface.Sounds.FootLand;
			if ( string.IsNullOrWhiteSpace( sound ) )
				return;

			NextRightFoot = !NextRightFoot;

			float flVol = 1;

			Sound.FromWorld( "player.footstep.concrete", vecOrigin ).SetVolume( flVol );
			OnLandStep( vecOrigin, sound, flVol );
		}

		public void DoJumpSound( Vector3 vecOrigin, Surface surface, float fvol = 1f )
		{
			if ( IsClient && !Prediction.FirstTime ) return;
			if ( surface == null ) return;

			var sound = surface.Sounds.FootLaunch;
			if ( string.IsNullOrWhiteSpace( sound ) )
				return;

			NextRightFoot = !NextRightFoot;

			float flVol = 1;

			Sound.FromWorld( "player.footstep.concrete", vecOrigin ).SetVolume( flVol );
			OnLandStep( vecOrigin, sound, flVol );
		}

		[ConVar.Replicated] public static bool sv_footsteps { get; set; } = true;
	}
}
