using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Source1
{
	public static class TeamManager
	{
		public struct TeamProperties
		{
			/// <summary>
			/// Internal name of the team.
			/// </summary>
			public string Name { get; set; }
			/// <summary>
			/// Display title of the team.
			/// </summary>
			public string Title { get; set; }
			/// <summary>
			/// Color of the team.
			/// </summary>
			public Color32 Color { get; set; }
			/// <summary>
			/// Is this team playable? Do they participate in action?
			/// </summary>
			public bool IsPlayable { get; set; }
			/// <summary>
			/// Can the player manually join this team?
			/// </summary>
			public bool IsJoinable { get; set; }
		}

		public static Dictionary<int, TeamProperties> Teams { get; set; } = new();

		public static void DeclareTeam( int number, string name, string title, Color32 color, bool playable = true, bool joinable = true )
		{
			DeleteTeam( number );

			Teams[number] = new TeamProperties()
			{
				Name = name,
				Title = title,
				Color = color,
				IsPlayable = playable,
				IsJoinable = joinable
			};
		}

		public static void DeleteTeam( int number )
		{
			if ( Teams.ContainsKey( number ) ) Teams.Remove( number );
		}

		public static TeamProperties GetProperties( int number ) => Teams[number];
		public static string GetTag( int team ) => $"Team_{team}";
		public static IEnumerable<Source1Player> GetPlayers( int team ) => Entity.All.OfType<Source1Player>().Where( x => x.TeamNumber == team );
		public static string GetName( int team ) => GetProperties( team ).Name;
		public static string GetTitle( int team ) => GetProperties( team ).Name;
		public static bool IsJoinable( int team ) => GetProperties( team ).IsJoinable;
		public static bool IsPlayable( int team ) => GetProperties( team ).IsPlayable;
		public static Color32 GetColor( int team ) => GetProperties( team ).Color;
	}
}
