using Sandbox;

namespace Source1
{
	partial class GameRules
	{
		/// <summary>
		/// This team has won the round! This will be Unassigned any other time.
		/// </summary>
		[Net] public int Winner { get; set; }
		[Net] public int WinReason { get; set; }

		/// <summary>
		/// This declares the winner and enters the round in humiliation mode?
		/// </summary>
		public void DeclareWinner( int winner, int reason )
		{
			// If we're already in humiliation, don't do anything.
			if ( State == GameState.TeamWin ) return;
			State = GameState.TeamWin;

			Winner = winner;
			WinReason = reason;

			if ( !Score.ContainsKey( winner ) ) Score[winner] = 0;
			Score[winner]++;

			// TFGame.PlaySoundToTeam( winner, "announcer.your_team.won" );
			// TFGame.PlaySoundToTeam( winner.GetOpponent(), "announcer.your_team.lost" );

			// TODO: For spectators the sound depends on who the user last spectated.
		}

		[ConVar.Replicated] public static float mp_chattime { get; set; } = 15f;
	}
}
