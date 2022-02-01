using Sandbox;
using System.Linq;
using System.Collections.Generic;

namespace Source1
{
	partial class GameRules
	{
		public void StartGameplay()
		{
			if ( State == GameState.Gameplay ) return;
			State = GameState.Gameplay;
		}
	}
}
