using Sandbox;
using System;
using System.Linq;

namespace Amper.FPS;

partial class SDKPlayer : IHasEffectEntity
{
	[Net, Predicted] public Vector3 ViewPunchAngle { get;  set; }
	[Net, Predicted] public Vector3 ViewPunchAngleVelocity { get;  set; }

	public virtual void ApplyViewPunchImpulse( float pitch, float yaw = 0, float roll = 0 )
	{
		var angle = ViewPunchAngle;
		angle.x += pitch;
		angle.y = yaw;
		angle.z = roll;
		ViewPunchAngle = angle;
	}

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

	public ModelEntity GetEffectEntity() => this;
}
