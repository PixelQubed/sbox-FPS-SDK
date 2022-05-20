namespace Source1;

partial class GameRules
{
	public virtual bool ShouldShowTeamGoal()
	{
		return State == GameState.PreRound;
	}
}
