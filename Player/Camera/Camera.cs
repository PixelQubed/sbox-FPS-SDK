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
			var player = Source1Player.Local;
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

			Position = specPos;
		}

		public virtual float ChaseDistanceMin => 16;
		public virtual float ChaseDistanceMax => 96;

		public virtual float GetDeathCamInterpolationTime( Source1Player player )
		{
			return player.DeathAnimationTime;
		}

		public void CalculateDeathCamView( Source1Player player )
		{
			var killer = player.ObserverTarget;

			// if we dont have a killer use chase cam
			if ( killer == null ) 
			{
				CalculateChaseCamView( player );
				return;
			}

			//
			// Force look at enemy
			//

			float interpolation = player.TimeSinceDeath / GetDeathCamInterpolationTime( player );
			interpolation = Math.Clamp( interpolation, 0, 1.0f );

			var toKiller = killer.EyePosition - Position;
			var rotToKiller = Rotation.LookAt( toKiller );

			Rotation = Rotation.Lerp( Rotation, rotToKiller, interpolation );
		}
	}
}
