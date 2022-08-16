using Sandbox;
using System;
using System.Linq;

namespace Amper.Source1;

partial class Source1Player : IHasParticleContainer
{
	// IHasParticleContainer
	public ParticleContainer ParticleContainer { get; set; }
	public Entity EffectEntity => IsFirstPersonMode ? this : All.OfType<Source1Player>().Where( x => x != this ).FirstOrDefault();

	[Net, Predicted] public Vector3 ViewPunchAngle { get;  set; }
	[Net, Predicted] public Vector3 ViewPunchAngleVelocity { get;  set; }


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
