using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1
{
	public partial class TeamplayRules : GameRules
	{
		[Net] public GameState State { get; set; }

		// States
		 
		public virtual float GetCaptureValueForPlayer( Source1Player player ) { return 1; }
		public virtual bool TeamMayCapturePoint( int team, int pointindex ) { return true; }
		public virtual bool PlayerMayCapturePoint( Source1Player player, int pointindex ) { return true; }
		public virtual bool PlayerMayBlockPoint( Source1Player player, int pointindex ) { return false; }

		public virtual bool PointsMayBeCaptured() { return true; }
		public virtual bool IsPlayerTeammate( Source1Player player, Source1Player other ) { return false; }

		// Winning
		public virtual void DeclareWinner( int team, int winreason, bool forceMapReset = true, bool switchTeams = false, bool dontAddScore = false, bool final = false ) { return; }
		public virtual void SetStalemate( int reason, bool forceMapReset = true, bool switchTeams = false ) { return; }

		// Switch Teams
		[Net] public bool ShouldSwitchTeams { get; set; }
		public virtual void SwitchTeams() { return; }

		// Waiting for players
		[Net] public bool IsWaitingForPlayers { get; set; }
	}

	public enum GameState
	{
		/// <summary>
		/// The game was just initialized.
		/// </summary>
		Initialized,
		/// <summary>
		/// Before players have joined the game. Periodically checks to see if enough players are ready
		/// to start a game. Also reverts to this when there are no active players.
		/// </summary>
		PreGame,
		/// <summary>
		/// The game is about to start, wait a bit and spawn everyone.
		/// </summary>
		StartGame,
		/// <summary>
		/// All players are respawned, frozen in place.
		/// </summary>
		PreRound,
		/// <summary>
		/// Round is on, playing normally.
		/// </summary>
		Gameplay,
		/// <summary>
		/// Someone has won the round.
		/// </summary>
		TeamWin,
		/// <summary>
		/// Noone has won, manually restart the game, reset scores.
		/// </summary>
		Restart,
		/// <summary>
		/// Game is over, showing the scoreboard etc
		/// </summary>
		Stalemate,
		GameOver,
		Bonus,
		BetweenRounds
	}
}
