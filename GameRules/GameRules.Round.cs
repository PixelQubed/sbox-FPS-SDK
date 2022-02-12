using Sandbox;
using System.Linq;
using System.Collections.Generic;

namespace Source1
{
	partial class GameRules
	{
		public bool IsRoundActive => State == GameState.Gameplay;

		/// <summary>
		/// Restart the round.
		/// </summary>
		public void RestartRound()
		{
			if ( !IsServer ) return;

			ResetObjectives();
			ClearMap();
			CalculateObjectives();
			RespawnPlayers( true );

			// Reset the winner.
			Winner = 0;
			WinReason = 0;

			State = GameState.PreRound;
		}

		public virtual void CalculateObjectives() { }
		public virtual void ResetObjectives() { }


		[ServerCmd( "mp_restart_round" )]
		public static void Command_RestartRound()
		{
			Current?.RestartRound();
		}

		public virtual void ClearMap()
		{
		}
	}
}
