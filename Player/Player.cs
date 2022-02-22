using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Source1
{
	public partial class Source1Player : Player
	{
		[Net] public float FallVelocity { get; set; }
		[Net] public float MaxSpeed { get; set; }
		[Net] public float MaxHealth { get; set; }

		public override void Spawn()
		{
			base.Spawn();
			Log.Info( $"Source1Player.Spawn()" );

			EnableLagCompensation = true;

			Controller = new Source1GameMovement();
			Animator = new StandardPlayerAnimator();
			Camera = new FirstPersonCamera();

			TeamNumber = 0;
			LastObserverMode = ObserverMode.Roaming;
		}

		[ServerCmd("respawn_me")]
		public static void Command_Respawn()
		{
			((Source1Player)ConsoleSystem.Caller.Pawn).Respawn();
		}

		public override void Respawn()
		{
			base.Respawn();
			Log.Info( $"Source1Player.Respawn()" );

			LifeState = LifeState.Alive;
			Velocity = Vector3.Zero;
			WaterLevel.Clear();
			MoveType = MoveType.MOVETYPE_WALK;

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = false;
			UseAnimGraph = true;

			CollisionGroup = CollisionGroup.Player;
			SetInteractsAs( CollisionLayer.Player );

			// Health
			Health = 100;
			MaxHealth = Health;

			// Movement
			FallVelocity = 0;
			MaxSpeed = GetMaxSpeed();
			AllowAutoMovement = true;
			SetCollisionBounds( GetPlayerMins(), GetPlayerMaxs() );

			TimeSinceSprayed = sv_spray_cooldown + 1;
			TimeSinceRespawned = 0;

			// Tags
			RemoveAllTags();
			Tags.Add( "player" );
			Tags.Add( TeamManager.GetTag( TeamNumber ) );

			PreviousWeapon = null;

			GameRules.Current.MoveToSpawnpoint( this );

			if ( TeamManager.IsPlayable( TeamNumber ) )
				StopObserverMode();
			else
				StartObserverMode( LastObserverMode );

			GameRules.Current.PlayerRespawn( this );
			ResetInterpolation();
		}

		public void SetCollisionBounds( Vector3 mins, Vector3 maxs )
		{
			var lastMoveType = MoveType;
			SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, mins, maxs );
			MoveType = lastMoveType;
		}

		public virtual WaterLevelType WaterLevelType { get; set; }
		[Net] public bool AllowAutoMovement { get; set; } = true;

		[Net] public TimeSince TimeSinceRespawned { get; set; }
		[Net] public TimeSince TimeSinceDeath { get; set; }
		[Net] public TimeSince TimeSinceTakeDamage { get; set; }

		public DamageInfo LastDamageInfo { get; set; }

		public override void Simulate( Client cl )
		{
			SimulateVisuals();

			if ( cl.IsBot ) SimulateBot( cl );
			GetActiveController()?.Simulate( cl, this, GetActiveAnimator() );

			SimulateActiveChild( cl, ActiveChild );
			SimulatePassiveChildren( cl );
		}

		public virtual void UpdateModel()
		{
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

		[ClientRpc]
		public virtual void RespawnEffects()
		{
			CreateHull();
		}

		public virtual float GetMaxSpeed()
		{
			return Source1GameMovement.sv_maxspeed;
		}

		public virtual float GetSensitivityMultiplier()
		{
			return 1f;
		}

		public void Kill()
		{
			if ( !IsAlive ) return;

			var dmg = DamageInfo.Generic( Health * 2 ).WithAttacker( this ).WithPosition( Position );
			TakeDamage( dmg );
		}


		[Event.BuildInput]
		public virtual void ProcessInput( InputBuilder input )
		{
			input.AnalogLook *= GetSensitivityMultiplier();

			if ( ForcedWeapon != null )
			{
				input.ActiveChild = ForcedWeapon;
				ForcedWeapon = null;
			}
		}

		public override void OnKilled()
		{
			Log.Info( "Source1Player.OnKilled()" );

			DeleteChildren();
			UseAnimGraph = false;

			BecomeRagdollOnClient( Velocity, LastDamageInfo.Flags, LastDamageInfo.Position, LastDamageInfo.Force * 30, GetHitboxBone( LastDamageInfo.HitboxIndex ) );

			EnableAllCollisions = false;
			EnableDrawing = false;

			TimeSinceDeath = 0;
			LifeState = LifeState.Respawning;
			StopUsing();

			GameRules.Current.PlayerDeath( this, LastDamageInfo );

			SetObserverMode( ObserverMode.Roaming );
		}

		public override void TakeDamage( DamageInfo info )
		{
			TimeSinceTakeDamage = 0;

			LastDamageInfo = info;
			GameRules.Event_OnPlayerHurt( this, info.Attacker, null, null, info.Weapon, info.Flags, info.Position, info.Damage );

			// flinch the model.
			Animator?.SetParam( "b_flinch", true );
			base.TakeDamage( info );
		}

		public virtual bool IsReadyToPlay()
		{
			return TeamManager.IsPlayable( TeamNumber );
		}

		[ConVar.Replicated] public static bool mp_player_freeze_on_round_start { get; set; } = true;
		public virtual bool CanPlayerMove()
		{
			var inPreRound = GameRules.Current.State == GameState.PreRound;
			var preRoundFreeze = mp_player_freeze_on_round_start;

			// no need to freeze if we're waiting for players.
			if ( GameRules.Current.IsWaitingForPlayers ) return true;

			var noMovement = inPreRound && preRoundFreeze;
			return !noMovement;
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
}
