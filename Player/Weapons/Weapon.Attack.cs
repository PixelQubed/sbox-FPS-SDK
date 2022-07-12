using Sandbox;

namespace Amper.Source1;

partial class Source1Weapon
{
	[Net, Predicted] public float NextAttackTime { get; set; }
	[Net, Predicted] public float LastAttackTime { get; set; }


	/// <summary>
	/// This simulates weapon's attack abilities.
	/// </summary>
	public virtual void SimulateAttack()
	{
		SimulatePrimaryAttack();
		SimulateSecondaryAttack();
	}

	/// <summary>
	/// This simulates weapon's secondary attack.
	/// </summary>
	public virtual void SimulateSecondaryAttack()
	{
		if ( !WishSecondaryAttack() )
			return;

		if ( !CanSecondaryAttack() )
			return;

		SecondaryAttack();
	}

	public virtual bool CanAttack()
	{
		if ( !Owner.IsValid() )
			return false;

		if ( !GameRules.Current.CanWeaponsAttack() )
			return false;

		if ( !Player.CanAttack() )
			return false;

		return true;
	}

	public float CalculateNextAttackTime() => CalculateNextAttackTime( GetAttackTime() );

	public virtual float CalculateNextAttackTime( float attackTime )
	{
		// Fixes:
		// https://www.youtube.com/watch?v=7puuYqq_rgw

		var curAttack = NextAttackTime;
		var deltaAttack = Time.Now - curAttack;

		if ( deltaAttack < 0 || deltaAttack > Global.TickInterval )
		{
			curAttack = Time.Now;
		}

		NextAttackTime = curAttack + attackTime;
		return curAttack;
	}

	/// <summary>
	/// When weapon fired while having no ammo.
	/// </summary>
	public virtual void OnDryFire()
	{
		if ( !PlayEmptySound() )
			return;

		NextAttackTime = Time.Now + 0.2f;
	}

	/// <summary>
	/// Procedure to play empty fire sound, if game needs it.
	/// If your weapon needs dry fire sounds, play it in this function and return true.
	/// Otherwise return false.
	/// </summary>
	public virtual bool PlayEmptySound() => false;

	public virtual void SecondaryAttack() { }
	public virtual bool WishSecondaryAttack() => Input.Down( InputButton.SecondaryAttack );

	public virtual bool CanSecondaryAttack()
	{
		return CanPrimaryAttack();
	}

	public virtual void SendAnimParametersOnAttack()
	{
		SendPlayerAnimParameter( "b_fire" );
		SendViewModelAnimParameter( "b_fire" );
	}


	/// <summary>
	/// Return the position in the worldspace, from which the attack is made.
	/// </summary>
	public virtual Vector3 GetAttackOrigin()
	{
		if ( Player == null ) 
			return Vector3.Zero;

		return Player.GetAttackPosition();
	}

	/// <summary>
	/// Return the diretion of the attack of this weapon.
	/// </summary>
	public virtual Rotation GetAttackRotation()
	{
		if ( Player == null )
			return Rotation.Identity;

		return Player.GetAttackRotation();
	}

	public virtual Vector3 GetAttackDirection() => GetAttackRotation().Forward;

	/// <summary>
	/// Return the diretion of the attack of this weapon.
	/// </summary>
	/// <returns></returns>
	public virtual Vector3 GetAttackDirectionWithSpread( Vector2 spread )
	{
		var rotation = GetAttackRotation();

		var forward = rotation.Forward;
		var right = rotation.Right;
		var up = rotation.Up;

		var dir = forward + spread.x * right + spread.y * up;
		dir = dir.Normal;
		return dir;
	}
}
