using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Source1
{
	public partial class Source1Player : Player
	{
		public virtual WaterLevelType WaterLevelType { get; set; }
		[Net] public TimeSince TimeSinceTakeDamage { get; set; }
		[Net] public float MaxSpeed { get; set; }
		[Net] public bool AllowAutoMovement { get; set; } = true;

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
			GameRules.Instance.MoveToSpawnpoint( this );
		}

		public void RemoveAllTags()
		{
			var list = Tags.List.ToList();
			foreach ( var tag in list )
			{
				Tags.Remove( tag );
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
	}

	public static class PlayerTags
	{
		public const string Ducked = "ducked";
		public const string WaterJump = "waterjump";
	}
}
