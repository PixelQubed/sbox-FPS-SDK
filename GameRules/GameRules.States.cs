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

		public virtual void SimulateInitialized()
		{
			// We have started the game, we are in pre game now.
			State = GameState.PreGame;
		}

		public virtual void StartedInitialized() { }
		public virtual void EndedInitialized() { }

		//
		// PreGame
		//

		public virtual void SimulatePreGame()
		{
			// If we have players that are ready to play, 
			// start waiting for players timer.
			if ( HasPlayers() )
			{
				StartWaitingForPlayers();
			}
		}

		public virtual void StartedPreGame() { }
		public virtual void EndedPreGame() { }

		//
		// ReadyUp
		//

		public virtual void SimulateReadyUp() { }
		public virtual void StartedReadyUp() { }
		public virtual void EndedReadyUp() { }

		//
		// PreRound
		//

		public virtual void SimulatePreRound() 
		{
			if ( TimeSinceStateChange > 5 )
			{
				State = GameState.Gameplay;
			}
		}
		public virtual void StartedPreRound() { }
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
				case GameState.ReadyUp: SimulateReadyUp(); break;
				case GameState.PreRound: SimulatePreRound(); break;
				case GameState.Gameplay: SimulateGameplay(); break;
				case GameState.RoundEnd: SimulateTeamWin(); break;
				case GameState.GameOver: SimulateGameOver(); break;
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
			Log.Info( $"Game State Changed: {previous} → {current}" );

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
				case GameState.ReadyUp: StartedReadyUp(); break;
				case GameState.PreRound: StartedPreRound(); break;
				case GameState.Gameplay: StartedGameplay(); break;
				case GameState.RoundEnd: StartedTeamWin(); break;
				case GameState.GameOver: StartedGameOver(); break;
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
				case GameState.ReadyUp: EndedReadyUp(); break;
				case GameState.PreRound: EndedPreRound(); break;
				case GameState.Gameplay: EndedGameplay(); break;
				case GameState.RoundEnd: EndedTeamWin(); break;
				case GameState.GameOver: EndedGameOver(); break;
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
		/// No players have yet joined the game. We are waiting for someone to join.
		/// </summary>
		PreGame,
		/// <summary>
		/// The game is about to start, we are waiting for players to get ready
		/// </summary>
		ReadyUp,
		/// <summary>
		/// The round is about to start, we make a short freezetime before the game is on.
		/// </summary>
		PreRound,
		/// <summary>
		/// Round is on, playing normally.
		/// </summary>
		Gameplay,
		/// <summary>
		/// The round has ended
		/// </summary>
		RoundEnd,
		/// <summary>
		/// Game is over, showing the scoreboard etc
		/// </summary>
		GameOver
	}
}
