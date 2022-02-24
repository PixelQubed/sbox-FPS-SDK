using Sandbox;

namespace Source1
{
	partial class Source1Camera : CameraMode
	{
		public override void Update()
		{
			var pawn = Source1Player.Local;
			var eyeRot = pawn.EyeRotation;
			var fov = 90f;

			//
			// View Punch
			//

			if ( cl_enable_view_punch )
			{
				var punchRot = pawn.ViewPunch;
				eyeRot = Rotation.From( eyeRot.Pitch() + punchRot.Pitch(), eyeRot.Yaw() + punchRot.Yaw(), eyeRot.Roll() + punchRot.Roll() );
			}

			FieldOfView = fov;
			Rotation = eyeRot;
			Position = pawn.EyePosition;
			Viewer = pawn;
		}

		[ClientVar] public static bool cl_enable_view_punch { get; set; } = true;
	}
}
