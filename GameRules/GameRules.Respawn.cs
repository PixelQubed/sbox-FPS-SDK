using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Source1
{
	partial class GameRules
	{
		protected Dictionary<int, SpawnPoint> LastSpawnPoint { get; set; } = new();

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
					limit++;
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

			Transform transform = new( 0, Rotation.Identity );

			// If we can't find a spawnpoint, fallback to default s&box spawn points.
			if ( result != null )
			{
				transform = result.Transform;
			}
			else 
			{
				Log.Info( $"Failed to find a team spawn point for {player}. Fallback to default sbox points." );

				var sboxpoint = All
					.OfType<Sandbox.SpawnPoint>()
					.OrderBy( x => Guid.NewGuid() )
					.FirstOrDefault();

				Log.Info( $"{sboxpoint}" );

				if ( sboxpoint != null ) transform = sboxpoint.Transform;
				else Log.Info( $"- This map lacks any spawn points, trying to land the player on [0,0,0]" );
			}

			// trying to land the player on the ground
			var origin = transform.Position;
			var up = origin + Vector3.Up * 64;
			var down = origin + Vector3.Down * 64;

			// Trace down so maybe we can find a spot to land on.
			var tr = Trace.Ray( up, down )
				.Size( player.CollisionBounds )
				.WorldOnly()
				.Run();

			if ( tr.Hit ) transform.Position = tr.EndPos;
			player.Transform = transform;
		}
	}
}
