using Sandbox;
using System.Linq;
using System.Collections.Generic;

namespace Source1
{
	partial class GameRules
	{
		public virtual bool ShouldShowTeamGoal()
		{
			return State == GameState.PreRound;
		}
	}
}
