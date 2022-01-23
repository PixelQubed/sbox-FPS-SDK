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
	}
}
