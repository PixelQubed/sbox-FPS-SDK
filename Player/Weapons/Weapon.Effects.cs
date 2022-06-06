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

		var startPos = Vector3.Zero;
		if ( attachEnt is ViewModel vm )
		{
			if ( !vm.MuzzleOrigin.HasValue )
				return;

			startPos = vm.MuzzleOrigin.Value;
		}
		else
		{
			// Find the muzzle attachment on the model.
			var muzzle = attachEnt.GetAttachment( "muzzle" );
			if ( !muzzle.HasValue )
				return;

			startPos = muzzle.Value.Position;
		}

		var endPos = tr.EndPosition;

		Vector3 toEnd = endPos - startPos;
		Angles angles = Vector3.VectorAngle( toEnd.Normal );
		Vector3 forward = Angles.AngleVector( angles );

		// Create the particle effect
		Particles tracer = Particles.Create( particle );
		tracer.SetPosition( 0, startPos );
		tracer.SetPosition( 1, endPos );
		tracer.SetForward( 0, forward );

	//	DebugOverlay.Line( startPos, endPos, Color.Cyan, 1, false );
	}

	public virtual string GetParticleTracerEffect() => "";
}
