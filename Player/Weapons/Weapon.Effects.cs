using Sandbox;

namespace Amper.Source1;

partial class Source1Weapon
{
	public virtual AnimatedEntity GetEffectEntity()
	{
		return IsLocalPawn && IsFirstPersonMode
			? Player?.GetViewModel( ViewModelIndex )
			: this;
	}

	[ClientRpc]
	public virtual void DoMuzzleFlash() { }
	public virtual void DoRecoil() { }

	private static int TracerCount { get; set; }

	public void CreateParticleFromTrace( TraceResult tr )
	{
		// get the tracer particle.
		string particle = GetParticleTracerEffect();
		if ( string.IsNullOrEmpty( particle ) )
			return;

		var traceFreq = GetTracerFrequency();

		// This weapon doesn't have any tracers.
		if ( traceFreq <= 0 )
			return;

		// Throttle tracer particles.
		if ( TracerCount++ % traceFreq != 0 )
			return;

		// Grab the entity we're supposed to draw effects from.
		var attachEnt = GetEffectEntity();
		if ( attachEnt == null )
			return;

		// Find the muzzle attachment on the model.
		var muzzle = attachEnt.GetAttachment( "muzzle" );
		if ( !muzzle.HasValue )
			return;

		var startPos = muzzle.Value.Position;
		var endPos = tr.EndPosition;

		Vector3 toEnd = endPos - startPos;
		Angles angles = Vector3.VectorAngle( toEnd.Normal );
		Vector3 forward = Angles.AngleVector( angles );

		// Create the particle effect
		Particles tracer = Particles.Create( particle );
		tracer.SetPosition( 0, startPos );
		tracer.SetPosition( 1, endPos );
		tracer.SetForward( 0, forward );
	}

	public virtual string GetParticleTracerEffect() => "";
}
