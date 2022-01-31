using Sandbox;
using System;

namespace Source1
{
	partial class Source1Player : ITeam
	{
		[Net] public int TeamNumber { get; set; }
		public Team TeamProperties => Source1.Team.FindByNumber( TeamNumber );

		public void SetTeam( int team )
		{
			Host.AssertServer();

			var lastTeam = TeamNumber;
			if ( lastTeam == team ) return;

			TeamNumber = team;
			Kill();

			// Run the event.
			// TFGame.Event_OnPlayerChangeTeam( this, team );
		}

		public void SetAutoTeam()
		{
			SetTeam( 0 );
		}
	}
}
