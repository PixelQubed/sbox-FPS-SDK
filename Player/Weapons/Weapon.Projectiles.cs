using Sandbox;

namespace Amper.Source1;

public partial class Source1Weapon
{
	public T FireProjectile<T>( Vector3 origin, Vector3 velocity, float damage, DamageFlags flags = 0 ) where T : Projectile, new()
	{
		var ent = new T();
		SetupProjectile( ent, origin, velocity, damage, flags );
		ent.OnInitialized( this );
		return ent; // Launch!
	}

	public virtual void SetupProjectile( Projectile ent, Vector3 origin, Vector3 velocity, float damage, DamageFlags flags )
	{
		ent.TeamNumber = TeamNumber;
		ent.Attacker = Owner;
		ent.Owner = Owner;
		ent.Launcher = this;

		ent.Velocity = velocity;
		ent.Position = origin;

		ent.Damage = damage;
		ent.DamageFlags |= flags;
	}

	public virtual void GetProjectileFireSetup( Vector3 offset, out Vector3 origin, out Vector3 direction, bool hitTeammates = false, float maxRange = 2000 )
	{
		var attackRotation = GetAttackRotation();
		var attackOrigin = GetAttackOrigin();

		var forward = attackRotation.Forward;
		var right = attackRotation.Right;
		var up = attackRotation.Up;

		// Trace the point at which the attacker is currently looking.
		var spread = GetSpread();
		var trDirection = GetAttackDirectionWithSpread( spread );
		var trTarget = attackOrigin + trDirection * maxRange;

		// Trace forward and find what's in front of us, and aim at that
		var tr = SetupFireBulletTrace( attackOrigin, trTarget ).Run();

		// Calculate the initial projectile setup
		origin = attackOrigin + forward * offset.x + right * offset.y + up * offset.z;
		direction = tr.EndPosition - attackOrigin;

		// Find angles that will get us to our desired end point
		// Only use the trace end if it wasn't too close, which results
		// in visually bizarre forward angles
		if ( tr.Fraction <= 0.1f )
			direction = trTarget - attackOrigin;

		direction = direction.Normal;
	}
}
