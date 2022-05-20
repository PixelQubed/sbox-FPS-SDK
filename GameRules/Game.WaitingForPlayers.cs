using Sandbox;
using System;

namespace Source1;

partial class GameRules
{
	[Net] public bool IsWaitingForPlayers { get; set; }
	public TimeSince TimeSinceWaitingForPlayersStart { get; set; }
	public float TimeUntilWaitingForPlayersEnds => MathF.Max( 0, mp_waiting_for_players_time - TimeSinceWaitingForPlayersStart );
	public float WaitingForPlayersTime { get; set; }

	public virtual void CheckWaitingForPlayers()
	{
		if ( mp_waiting_for_players_cancel )
		{
			mp_waiting_for_players_cancel = false;
		}

		if ( IsWaitingForPlayers )
		{
			if ( TimeSinceWaitingForPlayersStart > WaitingForPlayersTime )
			{
				if ( IsEnoughPlayersToStartRound() )
				{
					StopWaitingForPlayers();

					// Restart round immediately.
					RestartRound();
				}
			}
		}
	}

	public void StartWaitingForPlayers()
	{
		if ( !IsServer ) 
			return;

		if ( IsWaitingForPlayers ) 
			return;

		IsWaitingForPlayers = true;
		TimeSinceWaitingForPlayersStart = 0;
		WaitingForPlayersTime = mp_waiting_for_players_time;

		OnWaitingForPlayersStarted();
	}

	public void StopWaitingForPlayers()
	{
		if ( !IsServer )
			return;

		if ( !IsWaitingForPlayers )
			return;

		IsWaitingForPlayers = false;
		OnWaitingForPlayersEnded();
	}

	public virtual void OnWaitingForPlayersStarted() { }
	public virtual void OnWaitingForPlayersEnded() { }

	[ConVar.Replicated] public static bool mp_waiting_for_players_cancel { get; set; }
	[ConVar.Replicated] public static bool mp_waiting_for_players_restart { get; set; }
	[ConVar.Replicated] public static float mp_waiting_for_players_time { get; set; } = 30;
}
