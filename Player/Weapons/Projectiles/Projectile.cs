using Sandbox;

namespace Amper.Source1;

public abstract partial class Projectile : ModelEntity, ITeam
{
	[Net] public int TeamNumber { get; set; }
	[Net] public DamageFlags DamageFlags { get; set; } 
	[Net] public TimeSince TimeSinceCreated { get; set; }

	/// <summary>
	/// The entity that used a launcher to fire this projectile.
	/// </summary>
	public Entity Attacker { get; set; }
	/// <summary>
	/// The weapon that launched this projectile.
	/// </summary>
	public Entity Launcher { get; set; }
	/// <summary>
	/// The velocity at which this projectile was fired.
	/// </summary>
	public Vector3 OriginalVelocity { get; set; }
	/// <summary>
	/// The position at which this projectile was originally launched.
	/// </summary>
	public Vector3 OriginalPosition { get; set; }
	/// <summary>
	/// The launcher that launched this projectile originally.
	/// </summary>
	public Entity OriginalLauncher { get; set; }
	public Entity OriginalAttacker { get; set; }
	/// <summary>
	/// How much base damage will this projectile deal.
	/// </summary>
	public float Damage { get; set; }
	/// <summary>
	/// How much this projectile is affected by gravity (only works in Fly movetype.)
	/// </summary>
	public float Gravity { get; set; }
	/// <summary>
	/// The entity that will receive 100% of damage upon explosion.
	/// </summary>
	public Entity Enemy { get; set; }
	/// <summary>
	/// Did we touch world geometry?
	/// </summary>
	public bool Touched { get; set; }

	public float? AutoDestroyTime { get; set; }
	public float? AutoExplodeTime { get; set; }

	public override void Spawn()
	{
		base.Spawn();
		Tags.Add( CollisionTags.Projectile );

		TimeSinceCreated = 0;
		Damage = 0;
		Gravity = 0;
		MoveType = ProjectileMoveType.None;
		Predictable = false;
		DamageFlags = 0;

		EnableDrawing = false;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = false;

		AutoDestroyTime = 30;
	}

	[Event.Tick.Server]
	public virtual void Tick()
	{
		SimulateCollisions();
		SimulateMovement();
		UpdateFaceRotation();

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

	public virtual void OnInitialized( Entity launcher )
	{
		EnableDrawing = true;

		// Remember all this.
		OriginalPosition = Position;
		OriginalVelocity = Velocity;
		OriginalLauncher = Launcher;
		OriginalAttacker = Attacker;

		// Copy base velocity too, some projectile use it
		BaseVelocity = Velocity;

		UpdateFaceRotation();
		CreateTrails();
	}

	public virtual void OnTraceTouch( Entity other, TraceResult result ) { }

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		if ( !Touched )
		{
			var other = eventData.Other.Entity;
			if ( other.IsValid() && other.IsWorld ) 
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

	public virtual void Explode()
	{
		Host.AssertServer();

		Explode( Position );
	}

	public virtual void Explode( Vector3 position )
	{
		Host.AssertServer();

		var origin = position + Vector3.Up * 16;
		var target = position + Vector3.Down * 16;

		var tr = Trace.Ray( origin, target )
			.WorldOnly()
			.Run();

		Explode( tr );
	}

	public virtual void Explode( TraceResult trace )
	{
		Host.AssertServer();

		DoExplosionEffect( Position, trace.Normal );

		if ( Owner.IsValid() ) 
		{
			var dmgInfo = CreateDamageInfo();
			var radius = new RadiusDamageInfo( dmgInfo, Radius, this, AttackerRadius, Enemy );
			GameRules.Current.ApplyRadiusDamage( radius );
		}

		DoScorchTrace( Position, trace.Normal );
		Delete();
	}

	public ExtendedDamageInfo CreateDamageInfo()
	{
		return CreateDamageInfo( Damage );
	}

	public ExtendedDamageInfo CreateDamageInfo( float damage )
	{
		return CreateDamageInfo( damage, DamageFlags );
	}

	public ExtendedDamageInfo CreateDamageInfo( float damage, DamageFlags flags )
	{
		// If this projectile has an owner, report their position
		// otherwise fallback to our own position.
		var reportPos = Owner.IsValid() 
			? Owner.EyePosition 
			: Position;

		return ExtendedDamageInfo.Create( damage )
			.WithReportPosition( reportPos )
			.WithOriginPosition( OriginalPosition )
			.WithHitPosition( Position )
			.WithAttacker( Owner )
			.WithInflictor( this )
			.WithWeapon( Launcher )
			.WithFlag( flags );
	}

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
}

public enum ProjectileMoveType
{
	None,
	Fly,
	Physics
};
