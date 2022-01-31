using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1
{
	public partial class Team : Entity
	{
		[Net] public string Number { get; protected set; }

		[Net] public string Title { get; set; }
		[Net] public int RoundsWon { get; set; }
		[Net] public Color32 Color { get; set; }
		[Net] public bool Joinable { get; set; }
		[Net] public bool Playable { get; set; }

		public int Deaths { get; set; }
		public int LastSpawn { get; set; }

		public string Tag => $"team_{Number}";

		List<TeamSpawnPoint> SpawnPoints { get; set; }
		public List<ITeam> Members { get; protected set; }
		public IEnumerable<Source1Player> Players => Members.OfType<Source1Player>();

		public virtual void Initialize( string name, int number, Color32 color, bool joinable = true, bool playable = true )
		{
			InitializeSpawnpoints();
			InitializePlayers();

			Title = name;
			Number = Number;
			Color = color;

			Joinable = joinable;
			Playable = playable;
		}

		public virtual void InitializeSpawnpoints()
		{
			LastSpawn = 0;
		}

		public virtual void InitializePlayers()
		{

		}

		public virtual void AddSpawnpoint( TeamSpawnPoint point )
		{
			SpawnPoints.Add( point );
		}

		public virtual void RemoveSpawnpoint( TeamSpawnPoint pSpawnpoint )
		{
			SpawnPoints.Remove( pSpawnpoint );
		}

		public virtual Entity GetValidSpawnPoint( Source1Player player )
		{
			if ( SpawnPoints.Count == 0 ) return null;

			// Randomize the start spot
			int iSpawn = LastSpawn + Rand.Int( 1, 3 );
			if ( iSpawn >= SpawnPoints.Count )
				iSpawn -= SpawnPoints.Count;
			int iStartingSpawn = iSpawn;

			// Now loop through the spawnpoints and pick one
			int loopCount = 0;
			do
			{
				if ( iSpawn >= SpawnPoints.Count )
				{
					++loopCount;
					iSpawn = 0;
				}

				var point = SpawnPoints[iSpawn];

				// check if pSpot is valid, and that the player is on the right team
				if ( loopCount > 3 || point.IsValid ) 
				{
					LastSpawn = iSpawn;
					return point;
				}

				iSpawn++;
			} while ( iSpawn != iStartingSpawn ); // loop if we're not back to the start

			return null;
		}

		public virtual void AddMember( ITeam member )
		{
			Members.Add( member );
		}

		public virtual void RemoveMember( ITeam member )
		{
			Members.Remove( member );
		}

		public static Team FindByNumber( int number )
		{
			if ( GameRules.Instance.Teams.TryGetValue( number, out Team team ) )
			{
				return team;
			}

			return null;
		}

		public static bool IsPlayable( int index )
		{
			var team = FindByNumber( index );
			if ( team != null ) return team.Playable;
			return false;
		}

		public static bool IsJoinable( int index )
		{
			var team = FindByNumber( index );
			if ( team != null ) return team.Joinable;
			return false;
		}

		public static bool Exists( int index )
		{
			return FindByNumber( index ) != null;
		}
	}
}
