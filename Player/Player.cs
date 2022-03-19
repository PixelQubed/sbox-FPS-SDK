using Sandbox;
using System.Linq;

namespace Source1;

public partial class Source1Player : Player
{
	public static Source1Player LocalPlayer => Local.Pawn as Source1Player;

	[Net] public float FallVelocity { get; set; }
	[Net] public float SurfaceFriction { get; set; } = 1;

	[Net] public float MaxSpeed { get; set; }
	[Net] public float MaxHealth { get; set; }

	// These are from Entity, but they're not networked by default.
	// Client needs to be aware about these things.
	[Net] public new Entity LastAttacker { get; set; }
	[Net] public new Entity LastAttackerWeapon { get; set; }

	public override void Spawn()
	{
		base.Spawn();
		EnableLagCompensation = true;

		Controller = new Source1GameMovement();
		Animator = new StandardPlayerAnimator();
		CameraMode = new Source1Camera();

		TeamNumber = 0;
		LastObserverMode = ObserverMode.Chase;
	}

	[AdminCmd( "respawn_me" )]
	public static void Command_Respawn()
	{
		((Source1Player)ConsoleSystem.Caller.Pawn).Respawn();
	}

	public override void Respawn()
	{
		//
		// Tags
		//
		RemoveAllTags();
		Tags.Add( "player" );
		Tags.Add( TeamManager.GetTag( TeamNumber ) );

		//
		// Life State
		//
		LifeState = LifeState.Alive;
		Health = GetMaxHealth();
		MaxHealth = Health;
		TimeSinceRespawned = 0;

		LastAttacker = null;
		LastAttackerWeapon = null;
		LastDamageInfo = default;

		//
		// Rendering
		//
		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = false;
		UseAnimGraph = true;

		//
		// Teamplay
		// 
		if ( TeamManager.IsPlayable( TeamNumber ) ) StopObserverMode();
		else StartObserverMode( LastObserverMode );

		//
		// Movement
		//
		Velocity = Vector3.Zero;
		MoveType = MoveType.MOVETYPE_WALK;
		FallVelocity = 0;
		BaseVelocity = 0;
		MaxSpeed = GetMaxSpeed();

		CollisionGroup = CollisionGroup.Player;
		SetInteractsAs( CollisionLayer.Player );

		EnableHitboxes = true;
		SetCollisionBounds( GetPlayerMins( false ), GetPlayerMaxs( false ) );

		//
		// Weapons
		//
		PreviousWeapon = null;

		//
		// Misc
		//
		TimeSinceSprayed = sv_spray_cooldown + 1;

		// move the player to the spawn point
		GameRules.Current.MoveToSpawnpoint( this );
		ResetInterpolation();

		// let gamerules know that we have respawned.
		GameRules.Current.PlayerRespawn( this );
	}

	public override void OnNewModel( Model model )
	{
		base.OnNewModel( model );
		SetCollisionBounds( GetPlayerMins( false ), GetPlayerMaxs( false ) );
	}

	public void SetCollisionBounds( Vector3 mins, Vector3 maxs )
	{
		var lastEnableHitboxes = EnableHitboxes;
		var lastMoveType = MoveType;

		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, mins, maxs );

		MoveType = lastMoveType;
		EnableHitboxes = lastEnableHitboxes;

		if ( IsServer ) SetCollisionBoundsClient( mins, maxs );
	}

	[ClientRpc]
	void SetCollisionBoundsClient( Vector3 mins, Vector3 maxs )
	{
		SetCollisionBounds( mins, maxs );
	}

	public virtual WaterLevelType WaterLevelType { get; set; }

	[Net] public TimeSince TimeSinceRespawned { get; set; }
	[Net] public TimeSince TimeSinceDeath { get; set; }
	[Net] public TimeSince TimeSinceTakeDamage { get; set; }

	public DamageInfo LastDamageInfo { get; set; }

	public override void Simulate( Client cl )
	{
		SimulateVisuals();

		if ( IsObserver ) 
			SimulateObserver();

		if ( cl.IsBot ) 
			SimulateBot( cl );

		GetActiveController()?.Simulate( cl, this, GetActiveAnimator() );

		SimulateActiveChild( cl, ActiveChild );
		SimulatePassiveChildren( cl );

		if ( !IsAlive )
			return;

		SimulateHover();
		SimulateActiveWeapon();
	}

	public virtual void SimulateActiveWeapon()
	{
		//
		// Input requested a weapon switch
		//

		if ( Input.ActiveChild != null )
		{
			var newWeapon = Input.ActiveChild as Source1Weapon;
			if ( newWeapon != null )
			{
				ActiveChild = Input.ActiveChild;
			}
		}
	}

	public void RemoveAllTags()
	{
		var list = Tags.List.ToList();
		foreach ( var tag in list )
		{
			Tags.Remove( tag );
		}
	}

	public virtual void SimulatePassiveChildren( Client client )
	{
		foreach ( var child in Children.OfType<IPassiveChild>() )
		{
			child.PassiveSimulate( client );
		}
	}

	public virtual float GetMaxSpeed() { return Source1GameMovement.sv_maxspeed; }
	public virtual float GetSensitivityMultiplier() { return 1f; }

	public void Kill()
	{
		if ( !IsAlive ) return;

		var dmg = DamageInfo.Generic( Health * 2 )
			.WithAttacker( this )
			.WithPosition( Position );

		TakeDamage( dmg );
	}

	public override void BuildInput( InputBuilder builder )
	{
		builder.AnalogLook *= GetSensitivityMultiplier();

		if ( ForcedWeapon != null )
		{
			builder.ActiveChild = ForcedWeapon;
			ForcedWeapon = null;
		}
	}

	public override void OnKilled()
	{
		DeleteChildren();
		UseAnimGraph = false;

		BecomeRagdollOnClient( Velocity, LastDamageInfo.Flags, LastDamageInfo.Position, LastDamageInfo.Force * 30, GetHitboxBone( LastDamageInfo.HitboxIndex ) );

		EnableAllCollisions = false;
		EnableDrawing = false;

		TimeSinceDeath = 0;
		LifeState = LifeState.Respawning;
		StopUsing();

		StartObserverMode( ObserverMode.Deathcam );

		GameRules.Current.PlayerDeath( this, LastDamageInfo );
	}

	public override void TakeDamage( DamageInfo info )
	{
		TimeSinceTakeDamage = 0;

		LastDamageInfo = info;

		//
		// We need to punch our view a little bit.
		//

		var maxPunch = 5;
		var maxDamage = 100;
		var punchAngle = info.Damage.Remap( 0, maxDamage, 0, maxPunch );
		PunchView( -punchAngle, 0, 0 );

		// TODO: Punch player's view in position of damage?

		// flinch the model.
		SetAnimParameter( "b_flinch", true );

		// moved this up from entity class to not call procedural hit react from base
		LastAttacker = info.Attacker;
		LastAttackerWeapon = info.Weapon;

		if ( IsServer && Health > 0f && LifeState == LifeState.Alive )
		{
			Health -= info.Damage;
			if ( Health <= 0f )
			{
				Health = 0f;
				OnKilled();
			}
		}

		// Let gamerules know about this.
		GameRules.Current.PlayerHurt( this, info );
	}

	public virtual bool IsReadyToPlay()
	{
		return TeamManager.IsPlayable( TeamNumber );
	}

	[ConVar.Replicated] public static bool mp_player_freeze_on_round_start { get; set; } = true;

	public virtual bool CanMove()
	{
		var inPreRound = GameRules.Current.State == GameState.PreRound;
		var preRoundFreeze = mp_player_freeze_on_round_start;

		// no need to freeze if we're waiting for players.
		if ( GameRules.Current.IsWaitingForPlayers ) return true;

		var noMovement = inPreRound && preRoundFreeze;
		return !noMovement;
	}

	public virtual float GetMaxHealth()
	{
		return 100;
	}

	public virtual void CommitSuicide( bool explode = false, bool force = false )
	{
		if ( !IsAlive )
			return;

		Health = 1;
		var damage = DamageFlags.Generic;

		if ( explode ) damage |= DamageFlags.Blast | DamageFlags.AlwaysGib;
		else damage |= DamageFlags.DoNotGib;

		var info = DamageInfo.Generic( 1 )
			.WithAttacker( this )
			.WithFlag( damage );

		TakeDamage( info );
	}
}

public static class PlayerTags
{
	public const string Ducked = "ducked";
	public const string WaterJump = "waterjump";
	public const string Noclipped = "noclipped";
}

public static class Source1Team
{
	public const int Unassigned = 0;
	public const int Spectator = 1;
}
