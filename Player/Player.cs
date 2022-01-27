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
		[Net] public float MaxSpeed { get; set; }
		public virtual bool AllowAutoMovement { get; set; } = true;

		public override void Spawn()
		{
			base.Spawn();
			CollisionGroup = CollisionGroup.Player;
			EnableLagCompensation = true;

			MaxSpeed = S1GameMovement.sv_maxspeed;
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
		public virtual Vector3 GetPlayerMins( bool ducked )
		{
			return (ducked
				? GameRules.Instance.ViewVectors.DuckHullMin
				: GameRules.Instance.ViewVectors.HullMin
				);
		}

		public virtual Vector3 GetPlayerMaxs( bool ducked )
		{
			return (ducked
				? GameRules.Instance.ViewVectors.DuckHullMax
				: GameRules.Instance.ViewVectors.HullMax
				);
		}

		public virtual Vector3 GetPlayerExtents( bool ducked )
		{
			var mins = GetPlayerMins( ducked );
			var maxs = GetPlayerMaxs( ducked );

			return mins.Abs() + maxs.Abs();
		}

		public virtual Vector3 GetPlayerViewOffset( bool ducked )
		{
			return (ducked
				? GameRules.Instance.ViewVectors.DuckViewOffset
				: GameRules.Instance.ViewVectors.ViewOffset
				) * Scale;
		}
	}

	public static class PlayerTags
	{
		public const string Ducked = "ducked";
		public const string Jumped = "jumped";
	}
}
