using Sandbox;

namespace TFS2
{
	partial class Source1Camera : CameraMode
	{
		public override void Update()
		{
			var pawn = Local.Pawn;
			var eyeRot = pawn.EyeRotation;
			var fov = 90f;

			//
			// View Punch
			//

			if ( cl_enable_view_punch )
			{
				var punchRot = (pawn as TFPlayer).ViewPunch;
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
