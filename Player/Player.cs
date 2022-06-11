using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Amper.Source1;

[Title( "Player" ), Icon( "emoji_people" )]
public partial class Source1Player : AnimatedEntity
{
	public static Source1Player LocalPlayer => Local.Pawn as Source1Player;

	[Net] public PawnController Controller { get; set; }
	[Net] public PawnAnimator Animator { get; set; }
	public CameraMode CameraMode
	{
		get => Components.Get<CameraMode>();
		set => Components.Add( value );
	}

	public float SurfaceFriction { get; set; } = 1;

	public float MaxSpeed { get; set; }
	public float MaxHealth { get; set; }

	// These are from Entity, but they're not networked by default.
	// Client needs to be aware about these things.
	[Net] public new Entity LastAttacker { get; set; }
	[Net] public new Entity LastAttackerWeapon { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		Controller = new Source1GameMovement();
		Animator = new StandardPlayerAnimator();
		CameraMode = new Source1Camera();

		TeamNumber = 0;
		LastObserverMode = ObserverMode.Chase;
	}

	public virtual float GetMaxHealth() => 100;

	public virtual void Respawn()
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
		EnableLagCompensation = true;

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
		UpdateMaxSpeed();

		CollisionGroup = CollisionGroup.Player;
		AddCollisionLayer( CollisionLayer.Player );
		SetInteractsAs( CollisionLayer.Player );

		EnableHitboxes = true;
		SetCollisionBounds( GetPlayerMins( false ), GetPlayerMaxs( false ) );

		//
		// Weapons
		//
		PreviousWeapon = null;
		ActiveWeapon = null;

		//
		// Misc
		//
		TimeSinceSprayed = sv_spray_cooldown + 1;

		// move the player to the spawn point
		GameRules.Current.MoveToSpawnpoint( this );
		ResetInterpolation();

		if ( !IsObserver )
		{
			// let gamerules know that we have respawned.
			GameRules.Current.PlayerRespawn( this );
		}
	}

	public float GetFOV()
	{
		var camFov = ((Source1Camera)CameraMode).FieldOfView;
		if ( camFov > 0 )
			return camFov;

		// Fallback to 90, this is most likely just bots.
		return 90;
	}

	public override void OnKilled()
	{
		DeleteAllWeapons();

		UseAnimGraph = false;
		EnableAllCollisions = false;
		EnableDrawing = false;
		TimeSinceDeath = 0;
		LifeState = LifeState.Dead;

		StopUsing();
		StartObserverMode( ObserverMode.Deathcam );

		OnKilledRPC();

		GameRules.Current.PlayerDeath( this, LastDamageInfo );
	}

	[ClientRpc]
	void OnKilledRPC()
	{
		OnKilledEffects();
	}

	public virtual void OnKilledEffects() { }

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

		UpdateMaxSpeed();
		Controller?.Simulate( cl, this, Animator );

		SimulateWeaponSwitch();
		SimulateActiveWeapon( cl, ActiveWeapon );
		SimulatePassiveChildren( cl );

		if ( !IsAlive )
			return;

		SimulateHover();
	}


	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );
		Controller?.FrameSimulate( cl, this, Animator );
	}

	public void UpdateMaxSpeed()
	{
		MaxSpeed = CalculateMaxSpeed();

		if ( MaxSpeed <= 0 ) 
			Velocity = 0;
	}

	/// <summary>
	/// Called before movement is calculated, we update our max speed values based on current effects.
	/// I.e. if we're sprinting.
	/// </summary>
	public virtual float CalculateMaxSpeed() => Source1GameMovement.sv_maxspeed;

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
		var children = Children.OfType<IPassiveChild>().ToList();

		foreach ( var child in children )
		{
			child.PassiveSimulate( client );
		}
	}


	public virtual bool IsReadyToPlay() => TeamManager.IsPlayable( TeamNumber );

	[ConVar.Replicated] public static bool mp_freeze_on_round_start { get; set; } = true;

	public virtual bool CanMove()
	{
		if ( GameRules.Current.IsWaitingForPlayers )
			return true;

		if ( mp_freeze_on_round_start )
		{
			if ( GameRules.Current.IsRoundStarting )
				return false;
		}

		return true;
	}

	public virtual void CommitSuicide( bool explode = false )
	{
		if ( !IsAlive )
			return;

		Health = 1;
		var flags = DamageFlags.Generic;

		if ( explode )
		{
			// If we set to explode ourselves, gib!
			flags |= DamageFlags.Blast;
		}

		var info = DamageInfo.Generic( 1000 )
			.WithAttacker( this )
			.WithPosition( Position )
			.WithFlag( flags );

		TakeDamage( info );
	}

	public virtual float DuckingSpeedMultiplier => 0.33f;

	/// <summary>
	/// Called after the camera setup logic has run. Allow the player to
	/// do stuff to the camera, or using the camera. Such as positioning entities
	/// relative to it, like viewmodels etc.
	/// </summary>
	public override void PostCameraSetup( ref CameraSetup setup )
	{
		Host.AssertClient();

		if ( ActiveWeapon != null )
		{
			ActiveWeapon.PostCameraSetup( ref setup );
		}
	}

	/// <summary>
	/// Called from the gamemode, clientside only.
	/// </summary>
	public override void BuildInput( InputBuilder input )
	{
		if ( input.StopProcessing )
			return;

		ActiveWeapon?.BuildInput( input );
		Controller?.BuildInput( input );

		if ( input.StopProcessing )
			return;

		Animator?.BuildInput( input );

		// We were forced to switch to this weapon.
		if ( ForcedWeapon != null )
		{
			input.ActiveChild = ForcedWeapon;
			ForcedWeapon = null;
		}
	}

	public virtual void AttemptRespawn()
	{
		// See if we're allowed to respawn right now.
		if ( !GameRules.Current.AreRespawnsAllowed() )
			return;

		// team is not allowed to respawn right now.
		if ( !GameRules.Current.CanTeamRespawn( TeamNumber ) )
			return;

		// can the player respawn right now.
		if ( !GameRules.Current.CanPlayerRespawn( this ) )
			return;

		Respawn();
	}

	protected override void OnAnimGraphTag( string tag, AnimGraphTagEvent fireMode )
	{
		ActiveWeapon?.OnPlayerAnimGraphTag( tag, fireMode );
	}

	public override void OnAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData )
	{
		ActiveWeapon?.OnPlayerAnimEventGeneric( name, intData, floatData, vectorData, stringData );
	}

	public virtual void RenderHud( Vector2 screenSize )
	{
		ActiveWeapon?.RenderHud( screenSize );
	}
}

public static class PlayerTags
{
	/// <summary>
	/// Is currently ducking.
	/// </summary>
	public const string Ducked = "ducked";
	/// <summary>
	/// Is currently performing a water jump.
	/// </summary>
	public const string WaterJump = "waterjump";
	/// <summary>
	/// Is currently in cheat activated noclip mode.
	/// </summary>
	public const string Noclipped = "noclipped";
	/// <summary>
	/// Does not accept any damage.
	/// </summary>
	public const string GodMode = "god";
	/// <summary>
	/// Take all the damage, but don't die.
	/// </summary>
	public const string Buddha = "buddha";
}

public static class Source1Team
{
	public const int Unassigned = 0;
	public const int Spectator = 1;
}
