using Sandbox;

namespace Amper.Source1;

partial class Source1Player : ITeam
{
	[Net] public int TeamNumber { get; set; }
	[Net] public int TeamChanges { get; set; }

	public virtual void ChangeTeam( int team, bool autoTeam, bool silent, bool autobalance = false )
	{
		Host.AssertServer();

		// Desired team doesn't exist, don't bother changing to it.
		if ( !TeamManager.TeamExists( team ) )
			return;

		// The player is not allowed to change their team right now.
		if ( !GameRules.Current.CanPlayerChangeTeamTo( this, team ) ) 
			return;

		// see if gamemode wants to override ourt eam with something else.
		team = GameRules.Current.GetTeamAssignmentOverride( this, team, autobalance );

		// cant change team if we're already on this team.
		if ( TeamNumber == team ) 
			return;

		TeamNumber = team;
		TeamChanges++;

		// die if we're alive
		CommitSuicide( explode: false );

		// Enter observer mode if the team we just joined is not playable.
		if ( !TeamManager.IsPlayable( TeamNumber ) ) 
		{
			StartObserverMode( LastObserverMode );
		}

		AttemptRespawn();

		GameRules.Current.PlayerChangeTeam( this, team );
	}
}
