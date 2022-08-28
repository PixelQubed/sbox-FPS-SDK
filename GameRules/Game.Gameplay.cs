namespace Amper.FPS;

partial class GameRules
{
	public virtual bool ShouldShowTeamGoal()
	{
		return State == GameState.PreRound;
	}
}
