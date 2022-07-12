using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.Source1;

partial class Source1Weapon
{
	public virtual bool WishPrimaryAttack() => Input.Down( InputButton.PrimaryAttack );

	/// <summary>
	/// This simulates weapon's primary attack.
	/// </summary>
	public virtual void SimulatePrimaryAttack()
	{
		if ( !WishPrimaryAttack() )
			return;

		if ( !CanPrimaryAttack() )
			return;

		PrimaryAttack();
	}

	public virtual bool CanPrimaryAttack()
	{
		if ( !CanAttack() )
			return false;

		return NextAttackTime <= Time.Now;
	}

	public virtual void PrimaryAttack()
	{
		// Handle dry fire, if we don't have any ammo.
		if ( !HasEnoughAmmoToAttack() )
		{
			OnDryFire();
			return;
		}

		LastAttackTime = Time.Now;
		PlayAttackSound();
		SendAnimParametersOnAttack();
		ConsumeAmmoOnAttack();
		StopReload();

		Fire();
	}

	public virtual bool HasEnoughAmmoToAttack()
	{
		if ( !NeedsAmmo() )
			return true;

		var ammoPerAttack = GetAmmoPerShot();
		if ( Clip < ammoPerAttack )
			return false;

		return true;
	}

	public virtual void ConsumeAmmoOnAttack()
	{
		if ( !NeedsAmmo() )
			return;

		if ( sv_infinite_ammo )
			return;

		// Drain ammo.
		TakeAmmo( GetAmmoPerShot() );
	}

	public virtual void Fire()
	{
		for ( var i = 0; i < GetBulletsPerShot(); i++ )
		{
			FireBullet( GetDamage(), i );
		}

		CreateMuzzleFlash();
		DoRecoil();

		CalculateNextAttackTime( GetAttackTime() );
	}

	[ConVar.Replicated] public static bool sv_infinite_ammo { get; set; }

	public virtual void PlayAttackSound() { }
}
