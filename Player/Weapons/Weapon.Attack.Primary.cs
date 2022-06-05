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
		if ( !HasAmmo() )
		{
			OnDryFire();
			return;
		}

		FinishReload();
		Fire();
	}

	public virtual void Fire()
	{
		PlayShootSound();

		DoPlayerModelAnimation();
		DoViewModelAnimation();

		// Drain ammo.

		if ( NeedsAmmo() )
		{
			if( !sv_infinite_ammo )
				Clip -= AmmoPerShot;
		}

		for ( var i = 0; i < BulletsPerShot; i++ )
		{
			FireBullet( GetDamage(), i );
		}

		DoMuzzleFlash();
		DoRecoil();

		CalculateNextAttackTime( GetAttackTime() );
	}

	[ConVar.Replicated] public static bool sv_infinite_ammo { get; set; }

	public virtual void PlayShootSound() { }
}
