using Sandbox;
using System;

namespace Amper.Source1;

partial class PlayerAnimator
{
	public virtual bool LegShuffleEnabled => false;
	public virtual float LegShuffleMaxYawDiff => 45;
	public virtual float LegShuffleYawSpeed => 10;

	[Net, Predicted] public float GoalLegShuffleYaw { get; set; }

	public virtual void UpdateLegShuffle()
	{
		// TODO:
	}
}
