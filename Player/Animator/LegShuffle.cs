using Sandbox;
using System;

namespace Amper.Source1;

partial class PlayerAnimator
{
	public virtual bool LegShuffleEnabled => false;
	public virtual float LegShuffleMaxYawDiff => 45;
	public virtual float LegShuffleYawSpeed => 10;

	public virtual void UpdateLegShuffle()
	{
		// TODO:
	}
}
