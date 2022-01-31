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
			Winner = TFTeam.Unassigned;
			WinReason = WinReason.None;
			CalculateObjectives();

			StartGameplay();
		}


		[ServerCmd( "mp_restartround" )]
		public static void Command_RestartRound()
		{
			Instance?.RestartRound();
		}

		public virtual void ClearMap()
		{
			var list = new List<Entity>();

			// Cleanup dropped weapons.
			list.AddRange( All.OfType<TFWeaponBase>().Where( x => x.Owner == null ) );

			for ( int i = list.Count - 1; i >= 0; i-- )
			{
				var ent = list[i];
				if ( ent != null && ent.IsValid ) ent.Delete();
			}

			// Place flags on their home positions.
			foreach ( var flag in All.OfType<Flag>() ) 
			{
				flag.Return();
			}
		}
	}
}
