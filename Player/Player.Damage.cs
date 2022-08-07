using Sandbox;

namespace Amper.Source1;

partial class Source1Player
{
	/// <summary>
	/// This is called before all the calculations are made. Even if the damage doesn't go through!
	/// </summary>
	public virtual void OnAttackedBy( Entity attacker, ExtendedDamageInfo info ) { }

	public virtual void TakeDamage( ExtendedDamageInfo info )
	{
		if ( !IsServer )
			return;

		// We have been attacked by someone.
		OnAttackedBy( info.Attacker, info );

		// Check if player can receive damage from attacker.
		var attacker = info.Attacker;
		if ( !CanTakeDamage( attacker, info ) ) 
			return;

		// Apply damage modifications that are exclusive to the Player.
		ApplyOnPlayerDamageModifyRules( ref info );

		// Apply all global damage modifications.
		GameRules.Current.ApplyOnDamageModifyRules( ref info, this );

		// Do all the reactions to this damage.
		OnTakeDamageReaction( info );

		// Remember this damage as the one we taken last.
		// This is NOT networked!
		LastDamageInfo = info;
		LastAttacker = info.Attacker;
		LastAttackerWeapon = info.Weapon;
		TimeSinceTakeDamage = 0;

		//
		// Actually deal damage!
		//

		Health -= info.Damage;

		// We might want to avoid dying, do so.
		if ( ShouldPreventDeath( info ) ) 
			PreventDeath( info );

		if ( Health <= 0f )
			OnKilled();

		// Make an rpc to do stuff clientside.
		TakeDamageRPC( info.Attacker, info.Weapon, info.Damage, info.Flags, info.Position, info.HitboxIndex, info.Force );

		// Let gamerules know about this.
		GameRules.Current.PlayerHurt( this, info );
	}

	[ClientRpc]
	void TakeDamageRPC( Entity attacker, Entity weapon, float damage, DamageFlags flags, Vector3 position, int hitbox, Vector3 force )
	{
		OnTakeDamageEffects( attacker, weapon, damage, flags, position, hitbox, force );
	}

	public virtual void OnTakeDamageEffects( Entity attacker, Entity weapon, float damage, DamageFlags flags, Vector3 position, int hitbox, Vector3 force ) { }

	public bool PreventDeath( ExtendedDamageInfo info )
	{
		// We take damage, but we dont allow ourselves to die.
		if ( (Health - info.Damage) <= 0 )
		{
			Health = 1;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Check if this player is allowed to take damage from a given attacker.
	/// </summary>
	public virtual bool CanTakeDamage( Entity attacker, ExtendedDamageInfo info )
	{
		// Gods take no damage!
		if ( IsInGodMode )
			return false;

		return GameRules.Current.CanEntityTakeDamage( this, attacker, info );
	}

	/// <summary>
	/// If mod requires us to be pushed by the damage, apply the impulse here.
	/// </summary>
	public virtual void ApplyPushFromDamage( ExtendedDamageInfo info ) { }

	/// <summary>
	/// Modify how player accepts damage.
	/// </summary>
	public virtual void ApplyOnPlayerDamageModifyRules( ref ExtendedDamageInfo info ) { }

	public virtual void ApplyDamageViewPunch( ExtendedDamageInfo info ) { }

	/// <summary>
	/// How will the player react to taking damage? By default this applies abs velocity to the player,
	/// kicks the view of the player and makes it flinch.
	/// </summary>
	public virtual void OnTakeDamageReaction( ExtendedDamageInfo info )
	{
		// Apply velocity to the player from the damage.
		ApplyPushFromDamage( info );

		// Apply view kick.
		ApplyDamageViewPunch( info );

		PlayFlunchFromDamage( info );
	}

	public virtual void PlayFlunchFromDamage( ExtendedDamageInfo info )
	{
		// flinch the model.
		SetAnimParameter( "b_flinch", true );
	}

	public virtual bool ShouldPreventDeath( ExtendedDamageInfo info )
	{
		return IsInBuddhaMode;
	}

}
