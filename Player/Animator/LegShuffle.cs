using Sandbox;

namespace Amper.Source1;

partial class PlayerAnimator
{
	public virtual bool LegShuffleEnabled => true;
	public virtual float LegShuffleMaxYawDiff => 45;
	public virtual float LegShuffleYawSpeed => 10;

	[Net, Predicted] protected Rotation IdealLegShuffleRotation { get; set; }

	public virtual void UpdateLegShuffle()
	{
		var idealRotation = GetIdealRotation();
		var yawDiff = Rotation.Difference( IdealLegShuffleRotation, idealRotation ).Angle();

		// See if we need to start shuffling.
		if ( yawDiff >= LegShuffleMaxYawDiff )
		{
			IdealLegShuffleRotation = idealRotation;
		}

		Player.Rotation = Rotation.Slerp( Player.Rotation, IdealLegShuffleRotation, Time.Delta * LegShuffleYawSpeed );
	}
}
