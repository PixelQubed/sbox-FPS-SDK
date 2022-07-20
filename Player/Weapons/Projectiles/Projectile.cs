using Sandbox;

namespace Amper.Source1;

public abstract partial class Projectile : ModelEntity, ITeam
{
	[Net] public int TeamNumber { get; set; }
	[Net] public TimeSince TimeSinceCreated { get; set; }

	public Vector3 InitialVelocity { get; set; }
	public Vector3 StartPosition { get; set; }
	public Entity OriginalLauncher { get; set; }
	public Entity Launcher { get; set; }
	public float Damage { get; set; }
	public float Gravity { get; set; }
	public Entity Enemy { get; set; }
	public bool Touched { get; set; }

	public float? AutoDestroyTime { get; set; }
	public float? AutoExplodeTime { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		Tags.Add( CollisionTags.Solid );
		Tags.Add( CollisionTags.Projectile );

		TimeSinceCreated = 0;
		Damage = 0;
		Gravity = 0;
		MoveType = ProjectileMoveType.None;

		EnableDrawing = false;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = false;

		AutoDestroyTime = 30;
	}

	[Event.Tick.Server]
	public virtual void Tick()
	{
		SimulateMoveType();
		UpdateFaceRotation();
		SimulateCollisions();

		if ( AutoExplodeTime.HasValue )
		{
			if ( TimeSinceCreated > AutoExplodeTime.Value )
				Explode();
		}

		if ( AutoDestroyTime.HasValue )
		{
			if ( TimeSinceCreated > AutoDestroyTime.Value )
				Delete();
		}
	}

	public virtual void OnInitialized( Source1Weapon launcher )
	{
		EnableDrawing = true;
		UpdateFaceRotation();
	}

	public virtual void OnTraceTouch( Entity other, TraceResult result ) { }

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		base.OnPhysicsCollision( eventData );

		if ( !Touched )
		{
			var other = eventData.This.Entity;
			if ( other != null && other.IsWorld )
			{
				Touched = true;
				Damage *= TouchedDamageMultiplier;
			}
		}
	}

	public virtual float TouchedDamageMultiplier => 1;

	public void UpdateFaceRotation()
	{
		if ( !FaceVelocity )
			return;

		if ( Velocity.Length <= 0 )
			return;
		
		Rotation = Rotation.LookAt( Velocity.Normal );
	}

	public virtual void Initialize( Entity launcher, Vector3 start, Vector3 velocity, Entity owner, float damage )
	{
		Launcher = launcher;
		OriginalLauncher = launcher;
		Owner = owner;

		Velocity = velocity;
		InitialVelocity = velocity;
		Position = start;
		StartPosition = start;
		Damage = damage;
		EnableDrawing = true;

		// get the team of the owner entity.
		if ( Owner is ITeam iteam )
			TeamNumber = iteam.TeamNumber;

		UpdateFaceRotation();
	}

	protected Particles Trail { get; set; }
	protected Particles CriticalTrail { get; set; }

	/// <summary>
	/// Display clientside particle effects on detonation.
	/// </summary>
	[ClientRpc]
	public virtual void DoExplosionEffect( Vector3 position, Vector3 normal )
	{
		Host.AssertClient();

		var boom = Particles.Create( ExplosionParticleName, position );
		boom.SetForward( 0, normal );
		Sound.FromWorld( "weapon.explosion", Position );
	}

	[ClientRpc]
	public void DoScorchTrace( Vector3 position, Vector3 normal )
	{
		var tr = Trace.Ray( position + normal * 10, position - normal * 10 )
			.Ignore( this )
			.WorldOnly()
			.Run();

		if ( tr.Hit )
		{
			DecalSystem.PlaceOnWorld(
				Material.Load( "materials/decals/scorch.vmat" ),
				tr.EndPosition,
				Rotation.LookAt( tr.Normal ),
				new Vector3( 128, 128, 3 )
			);
		}
	}

	public virtual void Explode()
	{
		Explode( Position );
	}

	public virtual void Explode( Vector3 position )
	{
		var origin = position + Vector3.Up * 16;
		var target = position + Vector3.Down * 16;

		var tr = Trace.Ray( origin, target )
			.WorldOnly()
			.Run();

		Explode( tr );
	}

	public virtual void Explode( TraceResult trace )
	{
		DoExplosionEffect( Position, trace.Normal );

		if ( Owner.IsValid() ) 
		{
			var damage = SetupDamageInfo();
			ApplyDamageModifyRules( ref damage );

			var radius = new RadiusDamageInfo( damage, Radius, this, AttackerRadius, Enemy );
			GameRules.Current.ApplyRadiusDamage( radius );
		}

		DoScorchTrace( Position, trace.Normal );
		Delete();
	}

	public DamageInfo SetupDamageInfo()
	{
		var info = DamageInfo.Generic( Damage )
			.WithAttacker( Owner )
			.WithWeapon( Launcher )
			.WithPosition( Position )
			.WithFlag( GetDamageFlags() );

		return info;
	}

	public virtual void ApplyDamageModifyRules( ref DamageInfo info ) { }

	/// <summary>
	/// Default damage flag of this projectile, 
	/// </summary>
	public virtual DamageFlags DefaultDamageFlags => DamageFlags.Blast;

	public virtual DamageFlags GetDamageFlags() => DefaultDamageFlags;

	public virtual void Explode( Entity other, TraceResult trace )
	{
		// Save this entity as enemy, they will take 100% damage.
		Enemy = other;

		// Invisible.
		EnableDrawing = false;

		// Pull out a bit.
		if ( trace.Fraction != 1 ) 
			Position = trace.EndPosition + trace.Normal;

		Explode( trace );
	}

	public virtual float Radius => 146;
	public virtual float AttackerRadius => 121;

	public virtual bool IsDestroyable => false;
	public virtual bool FaceVelocity => true;

	public virtual string TrailParticleName => "";
	public virtual string ExplosionParticleName => "";
}

public enum ProjectileMoveType
{
	None,
	Fly,
	Physics
};
