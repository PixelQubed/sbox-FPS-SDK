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
	/// Override this if need to change the overall logic of how attacks are calculated.
	/// </summary>
	public virtual void SimulatePrimaryAttack()
	{
		if ( !WishPrimaryAttack() )
			return;

		if ( !CanPrimaryAttack() )
			return;

		PrimaryAttack();
	}

	/// <summary>
	/// Can we do a primary attack right now?
	/// </summary>
	public virtual bool CanPrimaryAttack()
	{
		if ( !CanAttack() )
			return false;

		return NextAttackTime <= Time.Now;
	}

	/// <summary>
	/// This is what happens when succefully initiate a primary attack.
	/// This play the required animations, sounds, consumes ammo and calculates next attack time.
	/// If you wish to change what the attack actually does (i.e. Fires a Rocket instead of a Bullet), override Attack().
	/// </summary>
	public virtual void PrimaryAttack()
	{
		// Handle dry fire, if we don't have any ammo.
		if ( !HasEnoughAmmoToAttack() )
		{
			// Play some dry fire effects.
			OnDryFire();
			return;
		}

		// Note when we last fired.
		LastAttackTime = Time.Now;

		// GAMEPLAY
		// Calculate when we need to attack next.
		CalculateNextAttackTime();
		// Consume ammo for this attack.
		ConsumeAmmoOnAttack();
		// Stop reloading if we are firing already.
		StopReload();
		// Adds recoil after shot.
		DoRecoil();

		// VISUALS
		// Play the appropriate attack animations.
		SendAnimParametersOnAttack();
		// Creates a muzzle particle effect.
		CreateMuzzleFlash();
		// Play the appropriate sound.
		PlayAttackSound();

		// DO THE SHOOTY THING!
		Attack();
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

	/// <summary>
	/// Consume ammo for this attack.
	/// </summary>
	public virtual void ConsumeAmmoOnAttack()
	{
		if ( !NeedsAmmo() )
			return;

		if ( sv_infinite_ammo )
			return;

		// Drain ammo.
		TakeAmmo( GetAmmoPerShot() );
	}

	/// <summary>
	/// This summons all the "attack" projectiles that this weapon executes.
	/// </summary>
	public virtual void Attack()
	{
		for ( var i = 0; i < GetBulletsPerShot(); i++ )
		{
			FireBullet( GetDamage(), i );
		}
	}

	public virtual void PlayAttackSound() { }

	[ConVar.Replicated] public static bool sv_infinite_ammo { get; set; }
}
