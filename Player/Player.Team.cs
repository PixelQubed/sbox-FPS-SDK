using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Source1
{
	partial class Source1Player : ITeam
	{
		[Net] public int TeamNumber { get; set; }

		public void SetTeam( int team )
		{
			Host.AssertServer();

			var lastTeam = TeamNumber;
			if ( lastTeam == team ) return;

			TeamNumber = team;
			Kill();

			// Run the event.
			GameRules.Current.PlayerChangeTeam( this, team );
		}

		public void SetAutoTeam()
		{
			// see which team has less players.
			var index = TeamManager.Teams.Keys.Where( x =>
			 {
				 var team = TeamManager.Teams[x];
				 if ( !team.IsJoinable ) return false;
				 if ( !team.IsPlayable ) return false;

				 return true;
			 } ).OrderBy( x => TeamManager.GetPlayers( x ).Count() ).FirstOrDefault();

			SetTeam( index );
		}
	}
}
