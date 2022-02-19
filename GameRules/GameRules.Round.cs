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

			IsWaitingForPlayers = false;

			ResetObjectives();
			ClearMap();
			RespawnPlayers( true );

			// Reset the winner.
			Winner = 0;
			WinReason = 0;

			if ( !IsEnoughPlayersToStartRound() )
			{
				StartWaitingForPlayers();
			}

			TransitionToState( GameState.PreRound );
			OnRoundRestart();
		}

		public virtual void OnRoundRestart()
		{
			
		}

		public virtual void CalculateObjectives() { }
		public virtual void ResetObjectives() { }

		public virtual bool IsEnoughPlayersToStartRound()
		{
			foreach ( var pair in TeamManager.Teams )
			{
				// not enough members in one team
				if ( !IsEnoughPlayersInTeamToStartRound( pair.Key ) )
					return false;
			}
			return true;
		}

		public virtual bool IsEnoughPlayersInTeamToStartRound( int team ) { return true; }

		[AdminCmd( "mp_restart_round" )]
		public static void Command_RestartRound()
		{
			Current?.RestartRound();
		}

		public virtual void ClearMap()
		{
			// reset all the doors to their initial state.
			var doors = All.OfType<DoorEntity>();
			foreach ( var door in doors ) door.Reset();
		}
	}
}
