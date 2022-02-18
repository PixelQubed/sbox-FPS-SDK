using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1
{
	partial class GameRules
	{
		[Net] public bool IsWaitingForPlayers { get; set; }
		[Net] public TimeSince TimeSinceWaitingForPlayersStart { get; set; }
		public float TimeUntilWaitingForPlayersEnds => MathF.Max( 0, mp_waiting_for_players_time - TimeSinceWaitingForPlayersStart );

		public virtual void CheckWaitingForPlayers()
		{
			if ( mp_waiting_for_players_cancel )
			{
				mp_waiting_for_players_cancel = false;
			}

			if ( IsWaitingForPlayers )
			{
				if ( TimeSinceWaitingForPlayersStart >= mp_waiting_for_players_time )
				{
					if ( IsEnoughPlayersToStartRound() )
					{
						// Restart round immediately.
						RestartRound();
					}
				}
			}
		}

		public void StartWaitingForPlayers()
		{
			if ( IsWaitingForPlayers ) return;
			Log.Info( "Started waiting for players." );

			IsWaitingForPlayers = true;
			TimeSinceWaitingForPlayersStart = 0;
		}

		public void StopWaitingForPlayers()
		{
			if ( !IsWaitingForPlayers ) return;
			Log.Info( "Ended waiting for players." );
			IsWaitingForPlayers = false;
		}

		[ConVar.Replicated] public static bool mp_waiting_for_players_cancel { get; set; }
		[ConVar.Replicated] public static bool mp_waiting_for_players_restart { get; set; }
		[ConVar.Replicated] public static float mp_waiting_for_players_time { get; set; } = 30;
	}
}
