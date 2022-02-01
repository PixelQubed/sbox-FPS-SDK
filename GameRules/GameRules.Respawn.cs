using Sandbox;
using System.Linq;
using System.Collections.Generic;

namespace Source1
{
	partial class GameRules
	{
		Dictionary<int, SpawnPoint> LastSpawnPoint { get; set; } = new();

		public virtual void MoveToSpawnpoint( Source1Player player )
		{
			// try to find a valid spawn point for this player
			var team = player.TeamNumber;

			var points = All.OfType<SpawnPoint>().ToList();
			var count = points.Count;

			var startIndex = -1;
			if ( LastSpawnPoint.TryGetValue( team, out var spawnPoint ) )
			{
				startIndex = points.IndexOf( spawnPoint );
			}


			int limit = 0;
			int i = startIndex;
			SpawnPoint result = null;

			if ( count > 0 )
			{
				while ( result == null && limit <= count )
				{
					i++;
					if ( i >= points.Count ) i = 0;

					var point = points[i];
					if ( point.CanSpawn( player ) )
					{
						result = point;
						LastSpawnPoint[team] = point;
						break;
					}
				}
			}

			Log.Info( $"Previous spawn point for team {team} was {startIndex} now it's {points.IndexOf( result )}" );

			// If we can't find a spawnpoint, fallback to default s&box spawn points.
			if ( result == null )
			{
				base.MoveToSpawnpoint( player );
				return;
			}

			player.Transform = result.Transform;
			Input.Rotation = result.Rotation;
		}
	}
}
