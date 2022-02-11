using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Source1
{
	public partial class Source1Player : Player
	{
		public virtual WaterLevelType WaterLevelType { get; set; }
		[Net] public float MaxSpeed { get; set; }
		[Net] public bool AllowAutoMovement { get; set; } = true;

		[Net] public TimeSince TimeSinceRespawned { get; set; }
		[Net] public TimeSince TimeSinceTakeDamage { get; set; }
		[Net] public TimeSince TimeSinceDeath { get; set; }

		public DamageInfo LastDamageInfo { get; set; }

		public override void Simulate( Client cl )
		{
			SimulateVisuals();

			if ( cl.IsBot ) SimulateBot( cl );

			SimulateActiveChild( cl, ActiveChild );
			SimulatePassiveChildren( cl );
		}

		public override void Spawn()
		{
			base.Spawn();
			TeamNumber = 0;

			LifeState = LifeState.Respawnable;
			CollisionGroup = CollisionGroup.Player;
			EnableLagCompensation = true;

			MaxSpeed = GetMaxSpeed();
			AllowAutoMovement = true;
		}

		/// <summary>
		/// Respawn this player.
		/// </summary>
		public override void Respawn()
		{
			LifeState = LifeState.Alive;
			Velocity = Vector3.Zero;
			WaterLevel.Clear();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = false;

			UseAnimGraph = true;
			Controller = new Source1GameMovement();
			Animator = new StandardPlayerAnimator();
			Camera = new FirstPersonCamera();

			// Tags
			RemoveAllTags();
			Tags.Add( "player" );
			Tags.Add( TeamManager.GetTag( TeamNumber ) );

			CreateHull();
			PreviousWeapon = null;
			ResetInterpolation();

			RespawnEffects();
			GameRules.Current.MoveToSpawnpoint( this );
			TimeSinceRespawned = 0;

			GameRules.Current.PlayerRespawn( this );
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
			OnKilled();
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
			DeleteChildren();
			UseAnimGraph = false;

			// BecomeRagdollOnClient( Velocity, LastDamageInfo.Flags, LastDamageInfo.Position, LastDamageInfo.Force * 30, GetHitboxBone( LastDamageInfo.HitboxIndex ) );

			Controller = null;
			Animator = null;
			Camera = new SpectateRagdollCamera();

			EnableAllCollisions = false;
			EnableDrawing = false;

			GameRules.Current.PlayerDeath( this, LastDamageInfo );
			TimeSinceDeath = 0;

			LifeState = LifeState.Respawning;
			StopUsing();
		}

		public override void TakeDamage( DamageInfo info )
		{
			TimeSinceTakeDamage = 0;

			LastDamageInfo = info;
			GameRules.Event_OnPlayerHurt( this, info.Attacker, null, null, info.Weapon, info.Flags, info.Position, info.Damage );

			// flinch the model.
			Animator.SetParam( "b_flinch", true );
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

			// Competitve checks?
			// Special conditions when pre round doesnt need to freeze us?

			var noMovement = inPreRound && preRoundFreeze;
			return !noMovement;
		}
	}

	public static class PlayerTags
	{
		public const string Ducked = "ducked";
		public const string WaterJump = "waterjump";
	}
}
