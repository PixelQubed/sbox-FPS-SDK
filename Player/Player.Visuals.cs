using Sandbox;
using System;

namespace Amper.Source1;

partial class Source1Player
{
	[Net, Predicted] public Vector3 ViewPunchAngle { get;  set; }
	[Net, Predicted] public Vector3 ViewPunchAngleVelocity { get;  set; }

	public virtual void SimulateVisuals()
	{
		DecayViewModificators();
	}

	public virtual void DecayViewModificators()
	{
		var angles = ViewPunchAngle;
		DecayAngles( ref angles, sv_view_punch_decay, 0, Time.Delta );
		ViewPunchAngle = angles;
	}

	[ConVar.Replicated] public static float sv_view_punch_decay { get; set; } = 18f;

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
}
