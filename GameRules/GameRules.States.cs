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
		[Net] public GameState State { get; set; }
		[Net] public TimeSince TimeSinceStateChange { get; set; }
		GameState LastState { get; set; }

		//
		// Initialized
		//

		public virtual void StartedInitialized() { }
		public virtual void SimulateInitialized() { }
		public virtual void EndedInitialized() { }

		//
		// PreGame
		//

		public virtual void StartedPreGame() { }
		public virtual void SimulatePreGame() { }
		public virtual void EndedPreGame() { }

		//
		// StartGame
		//

		public virtual void StartedStartGame() { }
		public virtual void SimulateStartGame() { }
		public virtual void EndedStartGame() { }

		//
		// PreRound
		//

		public virtual void StartedPreRound() { }
		public virtual void SimulatePreRound() { }
		public virtual void EndedPreRound() { }

		//
		// Gameplay
		//

		public virtual void StartedGameplay() { }
		public virtual void SimulateGameplay() { }
		public virtual void EndedGameplay() { }

		//
		// TeamWin
		//

		public virtual void StartedTeamWin() { }
		public virtual void SimulateTeamWin() { }
		public virtual void EndedTeamWin() { }

		//
		// Restart
		//

		public virtual void StartedRestart() { }
		public virtual void SimulateRestart() { }
		public virtual void EndedRestart() { }

		//
		// Stalemate
		//

		public virtual void StartedStalemate() { }
		public virtual void SimulateStalemate() { }
		public virtual void EndedStalemate() { }

		//
		// GameOver
		//

		public virtual void StartedGameOver() { }
		public virtual void SimulateGameOver() { }
		public virtual void EndedGameOver() { }

		//
		// Bonus
		//

		public virtual void StartedBonus() { }
		public virtual void SimulateBonus() { }
		public virtual void EndedBonus() { }

		//
		// BetweenRounds
		//

		public virtual void StartedBetweenRounds() { }
		public virtual void SimulateBetweenRounds() { }
		public virtual void EndedBetweenRounds() { }


		public virtual void TickStates()
		{
			if ( LastState != State )
			{
				OnStateChanged( LastState, State );
				LastState = State;
			}

			switch ( State )
			{
				case GameState.Initialized: SimulateInitialized(); break;
				case GameState.PreGame: SimulatePreGame(); break;
				case GameState.StartGame: SimulateStartGame(); break;
				case GameState.PreRound: SimulatePreRound(); break;
				case GameState.Gameplay: SimulateGameplay(); break;
				case GameState.TeamWin: SimulateTeamWin(); break;
				case GameState.Restart: SimulateRestart(); break;
				case GameState.Stalemate: SimulateStalemate(); break;
				case GameState.GameOver: SimulateGameOver(); break;
				case GameState.Bonus: SimulateBonus(); break;
				case GameState.BetweenRounds: SimulateBetweenRounds(); break;
			}
		}

		/// <summary>
		/// Gamemode state has been updated.
		/// </summary>
		/// <param name="previous"></param>
		/// <param name="current"></param>
		public virtual void OnStateChanged( GameState previous, GameState current )
		{
			OnStateEnded( previous );
			OnStateStarted( previous );

			TimeSinceStateChange = 0;
		}

		/// <summary>
		/// Gamemode state has started.
		/// </summary>
		/// <param name="state"></param>
		public virtual void OnStateStarted( GameState state )
		{
			switch ( state )
			{
				case GameState.Initialized: StartedInitialized(); break;
				case GameState.PreGame: StartedPreGame(); break;
				case GameState.StartGame: StartedStartGame(); break;
				case GameState.PreRound: StartedPreRound(); break;
				case GameState.Gameplay: StartedGameplay(); break;
				case GameState.TeamWin: StartedTeamWin(); break;
				case GameState.Restart: StartedRestart(); break;
				case GameState.Stalemate: StartedStalemate(); break;
				case GameState.GameOver: StartedGameOver(); break;
				case GameState.Bonus: StartedBonus(); break;
				case GameState.BetweenRounds: StartedBetweenRounds(); break;
			}
		}

		/// <summary>
		/// Gamemode state has ended.
		/// </summary>
		/// <param name="state"></param>
		public virtual void OnStateEnded( GameState state )
		{
			switch ( state )
			{
				case GameState.Initialized: EndedInitialized(); break;
				case GameState.PreGame: EndedPreGame(); break;
				case GameState.StartGame: EndedStartGame(); break;
				case GameState.PreRound: EndedPreRound(); break;
				case GameState.Gameplay: EndedGameplay(); break;
				case GameState.TeamWin: EndedTeamWin(); break;
				case GameState.Restart: EndedRestart(); break;
				case GameState.Stalemate: EndedStalemate(); break;
				case GameState.GameOver: EndedGameOver(); break;
				case GameState.Bonus: EndedBonus(); break;
				case GameState.BetweenRounds: EndedBetweenRounds(); break;
			}
		}
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
