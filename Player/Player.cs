using Sandbox;
using System;

namespace Source1
{
	public partial class S1Player : Player
	{
		/// <summary>
		/// Whether the player is alive.
		/// </summary>
		public bool IsAlive => LifeState == LifeState.Alive;
		/// <summary>
		/// Time since last damage taken, used by medigun.
		/// </summary>
		public TimeSince TimeSinceTakeDamage { get; set; }

		public override void Spawn()
		{
			base.Spawn();
			CollisionGroup = CollisionGroup.Player;
			EnableLagCompensation = true;
		}

		public virtual float GetMaxSpeed()
		{
			return S1GameMovement.sv_maxspeed;
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

		public virtual bool AllowAutoMovement { get; set; } = true;
	}

	public static class PlayerFlags
	{
		public const string Ducked = "ducked";
	}
}
