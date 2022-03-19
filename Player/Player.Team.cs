using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Source1;

partial class Source1Player : ITeam
{
	[Net] public int TeamNumber { get; set; }

	public virtual void ChangeTeam( int team, bool autoTeam, bool silent, bool autobalace = false )
	{
		Host.AssertServer();

		if ( !TeamManager.TeamExists( team ) )
			return;

		if ( TeamNumber == team ) return;
		TeamNumber = team;

		GameRules.Current.PlayerChangeTeam( this, team );
	}
}
