using Sandbox;

namespace Source1
{
	partial class Source1Camera : CameraMode
	{
		Vector3 LastPosition { get; set; }

		public override void Update()
		{
			var player = Source1Player.Local;
			if ( player == null ) return;

			Viewer = player;
			var eyepos = player.EyePosition;
			var eyerot = player.EyeRotation;
			var fov = 90f;

			if ( player.IsObserver )
			{
				CalculateObserverView( player, ref eyepos, ref eyerot, ref fov );
			}
			else
			{
				CalculatePlayerView( player, ref eyepos, ref eyerot, ref fov );
			}

			if ( eyepos.Distance( LastPosition ) < 60 )
			{
				eyepos = LastPosition.LerpTo( eyepos, 40 * Time.Delta );
			}

			FieldOfView = fov;
			Rotation = eyerot;
			Position = eyepos;

			LastPosition = eyepos;
		}

		public void CalculatePlayerView( Source1Player player, ref Vector3 eyepos, ref Rotation eyerot, ref float fov )
		{
		}

		public void CalculateObserverView( Source1Player player, ref Vector3 eyepos, ref Rotation eyerot, ref float fov )
		{
			switch( player.ObserverMode )
			{
				case ObserverMode.Deathcam:
				case ObserverMode.Freezecam:
				case ObserverMode.Roaming:
				case ObserverMode.Fixed:
					CalculateUnimplementedCamView( player, ref eyepos, ref eyerot, ref fov );
					break;

				case ObserverMode.InEye:
					CalculateInEyeCamView( player, ref eyepos, ref eyerot, ref fov );
					break;

				case ObserverMode.Chase:
					CalculateChaseCamView( player, ref eyepos, ref eyerot, ref fov );
					break;
			}
		}

		public void CalculateUnimplementedCamView( Source1Player player, ref Vector3 eyepos, ref Rotation eyerot, ref float fov )
		{
			FieldOfView = fov;
			Rotation = eyerot;
			Position = eyepos;
			Viewer = Local.Pawn;
		}

		public void CalculateInEyeCamView( Source1Player player, ref Vector3 eyepos, ref Rotation eyerot, ref float fov )
		{
			var target = player.ObserverTarget;

			if ( target == null )
				return;

			if ( target.LifeState != LifeState.Alive )
			{
				CalculateChaseCamView( player, ref eyepos, ref eyerot, ref fov );
				return;
			}

			eyepos = target.EyePosition;
			eyerot = target.EyeRotation;
			Viewer = target;
		}

		public void CalculateChaseCamView( Source1Player player, ref Vector3 eyepos, ref Rotation eyerot, ref float fov )
		{
			var target = player.ObserverTarget;

			if ( target == null )
				return;

			// TODO:
			// VALVE:
			// If our target isn't visible, we're at a camera point of some kind.
			// Instead of letting the player rotate around an invisible point, treat
			// the point as a fixed camera.

			var specPos = target.EyePosition - eyerot.Forward * 96;

			var tr = Trace.Ray( target.EyePosition, specPos )
				.Ignore( target )
				.HitLayer( CollisionLayer.Solid, true )
				.Run();

			eyepos = specPos;
		}


		[ClientVar] public static bool cl_enable_view_punch { get; set; } = true;
	}
}
