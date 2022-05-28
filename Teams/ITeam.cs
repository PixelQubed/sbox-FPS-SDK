using Sandbox;

namespace Amper.Source1;

/// <summary>
/// This class can have a team.
/// </summary>
public interface ITeam
{
	public int TeamNumber { get; }

	/// <summary>
	/// Returns true only if both entities are team entities, and both are in the same team. 
	/// </summary>
	/// <param name="one"></param>
	/// <param name="two"></param>
	/// <returns></returns>
	static public bool IsSame( Entity one, Entity two )
	{
		if ( one is not ITeam teamOne ) return false;
		if ( two is not ITeam teamTwo ) return false;

		return teamOne.TeamNumber == teamTwo.TeamNumber;
	}
}
