using Sandbox;

namespace Amper.Source1;

partial class Source1Player
{
	public override void TakeDamage( DamageInfo info )
	{
		if ( !IsServer )
			return;

		// Check if player can receive damage from attacker.
		var attacker = info.Attacker;
		if ( !CanTakeDamage( attacker, info ) ) 
			return;


		// Apply all global damage modifications.
		GameRules.Current.ApplyOnDamageModifyRules( ref info, this );

		// Apply damage modifications that are exclusive to the Player.
		ApplyOnPlayerDamageModifyRules( ref info );

		// Do all the reactions to this damage.
		OnTakeDamageReaction( info );


		// Remember this damage as the one we taken last.
		// This is NOT networked!
		LastDamageInfo = info;

		LastAttacker = info.Attacker;
		LastAttackerWeapon = info.Weapon;

		TimeSinceTakeDamage = 0;

		// Actually deal damage!
		Health -= info.Damage;
		if ( Health <= 0f )
		{
			OnKilled();
		}

		// Make an rpc to do stuff clientside.
		OnTakeDamageEffects( info.Attacker, info.Weapon, info.Damage, info.Flags, info.Position, info.HitboxIndex, info.Force );

		// Let gamerules know about this.
		GameRules.Current.PlayerHurt( this, info );
	}

	public bool PreventDeath( DamageInfo info )
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
	public virtual bool CanTakeDamage( Entity attacker, DamageInfo info )
	{
		// Gods take no damage!
		if ( IsInGodMode )
			return false;

		return GameRules.Current.CanEntityTakeDamage( this, attacker, info );
	}

	/// <summary>
	/// If mod requires us to be pushed by the damage, apply the impulse here.
	/// </summary>
	public virtual void ApplyPushFromDamage( DamageInfo info ) { }

	/// <summary>
	/// Modify how player accepts damage.
	/// </summary>
	public virtual void ApplyOnPlayerDamageModifyRules( ref DamageInfo info ) { }

	public virtual void ApplyDamageViewPunch( DamageInfo info )
	{
		// We need to punch our view a little bit.
		var maxPunch = 5;
		var maxDamage = 100;
		var punchAngle = info.Damage.Remap( 0, maxDamage, 0, maxPunch );
		// PunchViewAngles( -punchAngle, 0, 0 );
	}

	/// <summary>
	/// How will the player react to taking damage? By default this applies abs velocity to the player,
	/// kicks the view of the player and makes it flinch.
	/// </summary>
	public virtual void OnTakeDamageReaction( DamageInfo info )
	{
		// Apply velocity to the player from the damage.
		ApplyPushFromDamage( info );

		// Apply view kick.
		ApplyDamageViewPunch( info );

		// flinch the model.
		SetAnimParameter( "b_flinch", true );
	}

	[ClientRpc] public virtual void OnTakeDamageEffects( Entity attacker, Entity weapon, float damage, DamageFlags flags, Vector3 position, int bone, Vector3 force ) { }
}
