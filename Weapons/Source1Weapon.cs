using Sandbox;

namespace Amper.Source1;

public partial class Source1Weapon : AnimatedEntity
{
	public virtual bool CanEquip( Source1Player player )
	{
		return true;
	}

	/// <summary>
	/// This weapon performs a primary attack.
	/// </summary>
	public override void PrimaryAttack()
	{
		TimeSincePrimaryAttack = 0;
		StopReload();

		CalculateIsAttackCritical();
		PlayShootSound();

		DoPlayerModelAnimation();
		DoViewModelAnimation();

		// We want to attack, this will make us wait attackdelay time before we perform an attack.
		WantsToAttack = true;
	}
}
