using System;
using System.Collections.Generic;
using Sandbox;

namespace Amper.Source1;

partial class Source1Weapon : IHasEntityParticleManager
{
	public EntityParticleManager ParticleManager { get; set; }
	Entity IHasEntityParticleManager.EffectEntity => GetEffectEntity();


	[Event.Frame]
	public void InternalFrameUpdate()
	{
		FrameUpdate();
	}

	public virtual void FrameUpdate() { }
}
