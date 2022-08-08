using Sandbox;
using System;

namespace Amper.Source1;

partial class Source1Camera
{
	public void CalculateChaseCamView( Source1Player player )
	{
		var target = player.ObserverTarget;

		if ( target == null )
			return;

		// TODO:
		// VALVE:
		// If our target isn't visible, we're at a camera point of some kind.
		// Instead of letting the player rotate around an invisible point, treat
		// the point as a fixed camera.

		var specPos = target.EyePosition - Rotation.Forward * 96;

		var tr = Trace.Ray( target.EyePosition, specPos )
			.Ignore( target )
			.WithAnyTags( CollisionTags.Solid )
			.Run();

		Position = tr.EndPosition;
	}

	public virtual float ChaseDistanceMin => 16;
	public virtual float ChaseDistanceMax => 96;
}
