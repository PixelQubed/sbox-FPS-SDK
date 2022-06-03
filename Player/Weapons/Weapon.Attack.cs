using Sandbox;

namespace Amper.Source1;

partial class Source1Weapon
{
	[Net, Predicted]
	public float NextAttackTime { get; set; }

	public virtual void SimulatePrimaryAttack()
	{
		if ( !CanPrimaryAttack() )
			return;

		PrimaryAttack();
	}

	public virtual void SimulateSecondaryAttack()
	{
		if ( !CanSecondaryAttack() )
			return;

		SecondaryAttack();
	}

	public virtual bool CanAttack()
	{
		if ( !GameRules.Current.CanWeaponsAttack() )
			return false;

		if ( !Player.CanAttack() )
			return false;

		return true;
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
			Clip -= AmmoPerShot;

		for ( var i = 0; i < BulletsPerShot; i++ )
		{
			FireBullet( GetDamage(), i );
		}

		DoMuzzleFlash();
		DoRecoil();

		CalculateNextAttackTime( GetAttackTime() );
	}

	public virtual void PlayShootSound() { }

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
	public virtual bool WishPrimaryAttack() => Input.Down( InputButton.PrimaryAttack );
	public virtual bool WishSecondaryAttack() => Input.Down( InputButton.SecondaryAttack );

	public virtual bool CanPrimaryAttack()
	{
		if ( !CanAttack() )
			return false;

		return NextAttackTime <= Time.Now;
	}

	public virtual bool CanSecondaryAttack()
	{
		return CanPrimaryAttack();
	}

	public virtual void DoPlayerModelAnimation()
	{
		SendPlayerAnimParameter( "b_fire" );
	}

	public virtual void DoViewModelAnimation()
	{
		SendViewModelAnimParameter( "b_fire" );
	}

	/// <summary>
	/// Fire a bullet from this weapon.
	/// </summary>
	public virtual void FireBullet( float damage, int seedOffset = 0 )
	{
		FireBulletClient( damage, seedOffset );
		if ( IsServer ) FireBulletServer( damage, seedOffset );
	}

	[ClientRpc]
	void FireBulletClient( float damage, int seedOffset = 0 )
	{
		FireBulletEffects( damage, seedOffset );
	}

	/// <summary>
	/// Do server-side calculations, related to firing a bullet.
	/// </summary>
	protected virtual TraceResult FireBulletServer( float damage, int seedOffset = 0 )
	{
		Host.AssertServer();
		var tr = TraceFireBullet( seedOffset );

		if ( tr.Hit )
		{
			var entity = tr.Entity;
			if ( entity != null )
			{
				if ( GameRules.Current.CanTeamEntityDamageOther( Owner, entity ) ) 
				{
					var info = SetupDamageInfo( tr, damage );
					ApplyDamageModifications( entity, ref info, tr );
					entity.TakeDamage( info );
				}
			}
		}

		return tr;
	}

	public virtual void ApplyDamageModifications( Entity victim, ref DamageInfo info, TraceResult trace ) { }

	public virtual DamageFlags DefaultDamageFlags => DamageFlags.Bullet;
	public virtual DamageInfo SetupDamageInfo( TraceResult tr, float damage )
	{
		return DamageInfo.Generic( damage )
			.UsingTraceResult( tr )
			.WithFlag( DefaultDamageFlags )
			.WithForce( tr.Direction )
			.WithAttacker( Owner )
			.WithPosition( Owner.EyePosition )
			.WithWeapon( this );
	}

	private static int TracerCount { get; set; }

	/// <summary>
	/// Do client-side effects, related to firing a bullet.
	/// </summary>
	protected virtual TraceResult FireBulletEffects( float damage, int seedOffset = 0 )
	{
		Host.AssertClient();

		// Log.Info( "::FireBulletEffects()" );
		var tr = TraceFireBullet( seedOffset );
		if ( TracerFrequency > 0 && TracerCount++ % TracerFrequency == 0 )
		{
			var attachEnt = GetEffectEntity();
			if ( attachEnt != null )
			{
				var muzzle = attachEnt.GetAttachment( "muzzle" );
				if ( muzzle.HasValue )
				{
					var muzzlePos = muzzle.Value.Position;
					DoParticleTracerEffect( muzzlePos, tr.EndPosition );
				}
			}
		}

		if ( tr.Hit && GameRules.Current.CanTeamEntityDamageOther( Owner, tr.Entity ) )
			tr.Surface.DoBulletImpact( tr );

		return tr;
	}

	public virtual TraceResult TraceFireBullet( int seedOffset = 0 )
	{
		using var _ = LagCompensation();

		Rand.SetSeed( Time.Tick + seedOffset );

		Vector3 origin = GetAttackOrigin();
		Vector3 direction = GetAttackSpreadDirection();

		var target = origin + direction * Range;
		var tr = SetupFireBulletTrace( origin, target ).Run();

		if ( sv_debug_hitscan_hits )
		{
			DebugOverlay.Line( tr.StartPosition, tr.EndPosition, IsServer ? Color.Yellow : Color.Green, 5f, true );
			DebugOverlay.Sphere( tr.StartPosition, 2f, Color.Cyan, 5f, true );
			DebugOverlay.Sphere( tr.EndPosition, 2f, Color.Red, 5f, true );
			DebugOverlay.Text(
				$"Distance: {tr.Distance}\n" +
				$"HitBox: {tr.HitboxIndex}",
				tr.EndPosition,
				5f );
		}

		return tr;
	}

	[ConVar.Replicated] public static bool sv_debug_hitscan_hits { get; set; }

	protected virtual Trace SetupFireBulletTrace( Vector3 Origin, Vector3 Target )
	{
		var tr = Trace.Ray( Origin, Target )
			.Size( 2 )
			.Ignore( this )
			.Ignore( Owner )
			.HitLayer( CollisionLayer.Solid )
			.HitLayer( CollisionLayer.WINDOW )
			.HitLayer( CollisionLayer.Water, false )
			.UseHitboxes();

		return tr;
	}

	/// <summary>
	/// Return the position in the worldspace, from which the attack is made.
	/// </summary>
	/// <returns></returns>
	public virtual Vector3 GetAttackOrigin()
	{
		if ( Owner == null ) return Vector3.Zero;
		return Owner.EyePosition;
	}

	/// <summary>
	/// Return the diretion of the attack of this weapon.
	/// </summary>
	/// <returns></returns>
	public virtual Vector3 GetAttackDirection()
	{
		if ( Owner == null ) return Vector3.Zero;
		return Owner.EyeRotation.Forward;
	}

	/// <summary>
	/// Return the diretion of the attack of this weapon.
	/// </summary>
	/// <returns></returns>
	public virtual Vector3 GetAttackSpreadDirection()
	{
		var dir = GetAttackDirection();

		var spread = GetBulletSpread();
		dir += Vector3.Random * spread * .15f;

		return dir;
	}
}
