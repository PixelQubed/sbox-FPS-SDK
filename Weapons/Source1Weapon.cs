using Sandbox;

namespace Amper.Source1;

public partial class Source1Weapon : AnimatedEntity
{
	public virtual bool CanEquip( Source1Player player )
	{
		return true;
	}

	public override void Simulate( Client cl )
	{
		if ( WishSecondaryAttack() && CanSecondaryAttack() )
		{
			SecondaryAttack();
		}

		if ( WishPrimaryAttack() && CanPrimaryAttack() )
		{
			PrimaryAttack();
		}

		if ( WishReload() && CanReload() )
		{

		}
	}

	public virtual void PrimaryAttack()
	{
	}

	public virtual void SecondaryAttack()
	{
	}

	public virtual bool WishPrimaryAttack() => Input.Down( InputButton.PrimaryAttack );
	public virtual bool WishSecondaryAttack() => Input.Down( InputButton.SecondaryAttack );
	public virtual bool WishReload() => Input.Down( InputButton.Reload );


	public virtual bool CanPrimaryAttack() => NextPrimaryAttack <= Time.Now;
	public virtual bool CanSecondaryAttack() => NextSecondaryAttack <= Time.Now;

	public virtual bool CanReload()
	{
		return CanPrimaryAttack() && !IsReloading;
	}

	public int AmmoType { get; set; }
	public int Clip { get; set; }

	public virtual bool HasAmmo()
	{
		// Weapon doesn't use any ammo.
		if ( AmmoType <= 0 )
			return true;

		return Clip > 0;
	}
}
