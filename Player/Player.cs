using Sandbox;
using System;

namespace Source1
{
	public partial class Source1Player : Player
	{
		/// <summary>
		/// Whether the player is alive.
		/// </summary>
		public bool IsAlive => LifeState == LifeState.Alive;
		/// <summary>
		/// Time since last damage taken, used by medigun.
		/// </summary>
		public virtual WaterLevelType WaterLevelType { get; set; }
		[Net] public TimeSince TimeSinceTakeDamage { get; set; }
		[Net] public float MaxSpeed { get; set; }
		[Net] public bool AllowAutoMovement { get; set; } = true;

		public override void Spawn()
		{
			base.Spawn();
			CollisionGroup = CollisionGroup.Player;
			EnableLagCompensation = true;

			MaxSpeed = GetMaxSpeed();
			AllowAutoMovement = true;
		}

		public bool InWater()
		{
			return WaterLevelType >= WaterLevelType.Feet;
		}

		public bool InUnderwater()
		{
			return WaterLevelType >= WaterLevelType.Eyes;
		}

		public virtual float GetMaxSpeed()
		{
			return Source1GameMovement.sv_maxspeed;
		}

		public virtual float GetSensitivityMultiplier()
		{
			return 1f;
		}

		[Event.BuildInput]
		protected new virtual void BuildInput( InputBuilder builder )
		{
			builder.AnalogLook *= GetSensitivityMultiplier();
		}
	}

	public static class PlayerTags
	{
		public const string Ducked = "ducked";
		public const string Jumped = "jumped";
		public const string WaterJump = "waterjump";
	}
}
