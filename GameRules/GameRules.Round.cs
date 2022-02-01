using Sandbox;
using System.Linq;
using System.Collections.Generic;

namespace Source1
{
	partial class GameRules
	{
		/// <summary>
		/// Restart the round.
		/// </summary>
		public void RestartRound()
		{
			if ( !IsServer ) return;

			ClearMap();
			RespawnPlayers( true );

			// Reset the winner.
			Winner = 0;
			WinReason = 0;

			StartGameplay();
		}


		[ServerCmd( "mp_restartround" )]
		public static void Command_RestartRound()
		{
			Instance?.RestartRound();
		}

		public virtual void ClearMap()
		{
		}
	}
}
