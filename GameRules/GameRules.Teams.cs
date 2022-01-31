using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1
{
	public partial class GameRules : Game
	{
		public Dictionary<int, Team> Teams { get; set; }

		public virtual Team DeclareTeam( int number, string name, Color32 Color, bool joinable = true, bool playable = true )
		{
			// check if we've already created a team 
			if ( Teams.TryGetValue( number, out Team value ) )
			{
				Teams.Remove( number );
				value.Delete();
			}

			var team = new Team();
			team.Initialize( name, number, Color, joinable, playable );
			Teams[number] = team;

			return team;
		}
	}
}
