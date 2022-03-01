using Sandbox;
using System;

namespace Source1
{
	partial class Source1Camera : CameraMode
	{
		Vector3 LastPosition { get; set; }
		Rotation LastRotation { get; set; }

		bool LerpEnabled { get; set; }

		float ChaseDistance { get; set; }

		public override void Update()
		{
			var player = Source1Player.LocalPlayer;
			if ( player == null ) return;

			Viewer = player;
			Position = player.EyePosition;
			Rotation = player.EyeRotation;
			FieldOfView = 90f;

			LerpEnabled = true;

			if ( player.IsObserver ) CalculateObserverView( player );
			else CalculatePlayerView( player );

			if ( LerpEnabled )
				CalculateLerp();

			LastPosition = Position;
			LastRotation = Rotation;
		}

		public void CalculateLerp()
		{
			if ( Position.Distance( LastPosition ) < 15 ) 
			{
				Position = LastPosition.LerpTo( Position, 40 * Time.Delta );
			}

			Rotation = Rotation.Lerp( LastRotation, Rotation, 80 * Time.Delta );
		}

		public virtual void CalculatePlayerView( Source1Player player )
		{
			Rotation = Rotation * player.ViewPunch;
		}

		public virtual void CalculateObserverView( Source1Player player)
		{
			switch( player.ObserverMode )
			{
				case ObserverMode.Roaming:
					CalculateRoamingCamView( player );
					break;

				case ObserverMode.InEye:
					CalculateInEyeCamView( player );
					break;

				case ObserverMode.Chase:
					CalculateChaseCamView( player );
					break;

				case ObserverMode.Deathcam:
					CalculateDeathCamView( player );
					break;
			}
		}

		//
		// Observer Camera Modes
		//

		public void CalculateRoamingCamView( Source1Player player )
		{
			LerpEnabled = false;
		}

		public void CalculateInEyeCamView( Source1Player player )
		{
			var target = player.ObserverTarget;

			// dont do anything, we don't have target.
			if ( target == null )
				return;

			if ( target.LifeState != LifeState.Alive )
			{
				CalculateChaseCamView( player );
				return;
			}

			Position = target.EyePosition;
			Rotation = target.EyeRotation;
			Viewer = target;
		}

		public void CalculateChaseCamView( Source1Player player )
		{
			// disable position lerp on chase camera 
			LerpEnabled = false;

			var target = player.ObserverTarget;

			if ( target == null )
				return;

			// TODO:
			// VALVE:
			// If our target isn't visible, we're at a camera point of some kind.
			// Instead of letting the player rotate around an invisible point, treat
			// the point as a fixed camera.

			var specPos = target.EyePosition - Rotation.Forward * 96;

			var tr = Trace.Ray( target.EyePosition, specPos )
				.Ignore( target )
				.HitLayer( CollisionLayer.Solid, true )
				.Run();

			Position = tr.EndPosition;
		}

		public virtual float ChaseDistanceMin => 16;
		public virtual float ChaseDistanceMax => 96;

		Vector3 LastDeathcamPosition { get; set; }

		public void CalculateDeathCamView( Source1Player player )
		{
			LerpEnabled = false;
			var killer = player.LastAttacker;

			// if we dont have a killer use chase cam
			if ( killer == null || player == killer ) 
				return;

			var deathAnimTime = player.DeathAnimationTime;
			if ( player.TimeSinceDeath > deathAnimTime )
			{
				CalculateFreezeCamView( player );
				return;
			}

			//
			// Force look at enemy
			//

			float rotLerp = player.TimeSinceDeath / (deathAnimTime / 2);
			rotLerp = Math.Clamp( rotLerp, 0, 1.0f );

			var toKiller = killer.EyePosition - Position;
			toKiller = toKiller.Normal;

			var rotToKiller = Rotation.LookAt( toKiller );
			Rotation = Rotation.Lerp( Rotation, rotToKiller, rotLerp );

			//
			// Zoom out from our target
			//

			float posLerp = player.TimeSinceDeath / deathAnimTime;
			posLerp = Math.Clamp( posLerp, 0, 1.0f );
			
			var target = Position + -toKiller * posLerp * ChaseDistanceMax * Easing.QuadraticInOut( posLerp );

			var tr = Trace.Ray( Position, target )
				.HitLayer( CollisionLayer.Solid, true )
				.Run();

			target = tr.EndPosition;
			if ( tr.Hit ) target += toKiller * 6;

			Position = target;

			// position is going to be reset next tick, remember it to use in freezecam.
			LastDeathcamPosition = Position;

			WillPlayFreezeCamSound = true;
			WillFreezeGameScene = true;
		}
		bool WillPlayFreezeCamSound { get; set; }
		bool WillFreezeGameScene { get; set; }

		public virtual float FreezeCamDistanceMin => 96;
		public virtual float FreezeCamDistanceMax => 200;

		public void CalculateFreezeCamView( Source1Player player )
		{
			var killer = player.LastAttacker;

			if ( killer == null ) 
				return;

			// get time for death animation
			var deathAnimTime = player.DeathAnimationTime;
			// get time for freeze cam to move to the player
			var travelTime = Source1Player.sv_spectator_freeze_traveltime;

			// time that has passed while we are in freeze cam
			var timeInFreezeCam = player.TimeSinceDeath - deathAnimTime;
			timeInFreezeCam = MathF.Max( 0, timeInFreezeCam );

			// get lerp of the travel
			var travelLerp = Math.Clamp( timeInFreezeCam / travelTime, 0, 1 );
				
			// getting origin position and killer eye position
			var originPos = LastDeathcamPosition;
			var killerPos = killer.EyePosition;

			// direction to target from us.
			var toTarget = killerPos - originPos;
			toTarget = toTarget.Normal;

			// getting distance from that we need to keep from killer's eyes.
			var distFromTarget = FreezeCamDistanceMin;

			// final position, this is where the freezecam will end.
			var targetPos = killerPos - toTarget * distFromTarget;

			//
			// making sure there are no walls in between us
			//

			var tr = Trace.Ray( killerPos, targetPos )
				.HitLayer( CollisionLayer.Solid, true )
				.Ignore( killer )
				.Run();

			targetPos = tr.EndPosition;
			if ( tr.Hit ) targetPos += toTarget * MathF.Min( 5, tr.Distance );


			Position = originPos.LerpTo( targetPos, travelLerp * Easing.EaseIn( travelLerp ) );
			Rotation = Rotation.LookAt( toTarget );

			//
			// Playing freezecam sound .3s before we reach destination.
			//

			var freezeSoundLength = .3f;
			var freezeSoundStartTime = travelTime - freezeSoundLength;

			if ( WillPlayFreezeCamSound && timeInFreezeCam > freezeSoundStartTime )
			{
				WillPlayFreezeCamSound = false;
				PlayFreezeCamSound();
			}

			//
			// Freezing screen when we reach lerp 1.
			//

			if ( WillFreezeGameScene && travelLerp >= 1 )
			{
				WillFreezeGameScene = false;
				FreezeCameraPanel.Freeze( Source1Player.sv_spectator_freeze_time, Position, Rotation, FieldOfView );
			}
		}

		public virtual void PlayFreezeCamSound()
		{
			Sound.FromScreen( "player.freeze_cam" );
		}
	}
}
