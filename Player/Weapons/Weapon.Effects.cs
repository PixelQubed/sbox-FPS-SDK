using Sandbox;

namespace Amper.Source1;

partial class Source1Weapon
{
	[ClientRpc]
	public virtual void DoMuzzleFlash() { }
	public virtual void DoRecoil() { }

	/// <summary>
	/// Creates clientside particle tracers.
	/// </summary>
	[ClientRpc]
	public void DoParticleTracerEffect( Vector3 startPos, Vector3 endPos )
	{
		// Grab the data
		Vector3 toEnd = endPos - startPos;
		Angles angles = Vector3.VectorAngle( toEnd.Normal );
		Vector3 forward = Angles.AngleVector( angles );

		string particle = GetParticleTracerEffect();
		if ( string.IsNullOrEmpty( particle ) )
			return;

		// Create the particle effect
		Particles tracer = Particles.Create( particle );
		tracer.SetPosition( 0, startPos );
		tracer.SetPosition( 1, endPos );
		tracer.SetForward( 0, forward );
	}

	public virtual string GetParticleTracerEffect() => "";
}
