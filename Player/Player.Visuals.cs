using Sandbox;
using System;

namespace Amper.Source1;

partial class Source1Player
{
	[Net, Predicted] public Vector3 ViewPunchAngle { get;  set; }
	[Net, Predicted] public Vector3 ViewPunchAngleVelocity { get;  set; }

	public virtual void SimulateVisuals()
	{
	}

	public virtual void DecayViewModificators() { }

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
