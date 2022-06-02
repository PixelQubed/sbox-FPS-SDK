using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amper.Source1;

partial class GameRules
{
	protected Dictionary<int, SpawnPoint> LastSpawnPoint { get; set; } = new();

	/// <summary>
	/// Try to place the player on the spawn point.	This functions returns true if nothing occupies the player's
	/// space and they can safely spawn without getting stuck. `transform` will contain the transform data of the position where the 
	/// player would've spawned.
	/// </summary>
	public virtual bool TryFitOnSpawnpoint( Source1Player player, Entity spawnPoint, out Transform transform )
	{
		transform = spawnPoint.Transform;

		// trying to land the player on the ground
		var origin = spawnPoint.Position;

		var extents = player.GetPlayerExtentsScaled( false );
		var offset = Vector3.Up * (extents.z / 2);
		var center = origin + offset;

		var up = center + Vector3.Up * 64;
		var down = center + Vector3.Down * 64;

		// DebugOverlay.Sphere( center, 10, Color.Red, false, 5 );

		// 
		// Land the player on the ground
		//

		// Trace down so maybe we can find a spot to land on.
		var tr = Trace.Ray( up, down )
			.Size( extents )
			.HitLayer( CollisionLayer.Solid, true )
			.WithoutTags( "player", "projectile" )
			.Run();

		// DebugOverlay.Sphere( tr.EndPosition, 10, Color.Yellow, false, 5 );

		// we landed on something, update our transform position.
		if ( tr.Hit )
		{
			transform.Position = tr.EndPosition - offset;
			center = tr.EndPosition;
		}

		// 
		// Check if nothing occupies our spawn space.
		//

		// TODO:
		// use gamemovement trace builder?

		tr = Trace.Ray( center, center )
			.Size( extents )
			.HitLayer( CollisionLayer.Solid, true )
			.WithoutTags( "projectile" )
			.Run();

		return !tr.Hit;
	}

	public virtual void MoveToSpawnpoint( Source1Player player )
	{
		// try to find a valid spawn point for this player
		var team = player.TeamNumber;

		// get all available spawn points in the list.
		var points = All.OfType<SpawnPoint>().ToList();
		var count = points.Count;

		//
		// TEAM SPAWN POINTS
		//

		// go through all source1base spawn points and see which one can spawn us.
		if ( count > 0 ) 
		{
			// we'll use this if we can't find any point that could place us
			// without getting stuck
			SpawnPoint firstEligiblePoint = null;

			// figuring out at which point we should start.
			var index = -1;
			if ( LastSpawnPoint.TryGetValue( team, out var lastSpawnPoint ) )
				index = points.IndexOf( lastSpawnPoint );

			// looping through all points in the list.
			for ( int i = 0; i < count; i++ )
			{
				index++;
				if ( index >= count ) index = 0;

				var point = points[index];
				if ( point.CanSpawn( player ) )
				{
					// this point can spawn us!

					// remember it for the future, if it's the first one we found.
					if ( firstEligiblePoint == null )
						firstEligiblePoint = point;

					if ( TryFitOnSpawnpoint( player, point, out var transform ) )
					{
						player.Transform = transform;
						LastSpawnPoint[team] = point;
						return;
					}
				}
			}

			// we couldn't find a spawn point that wont get us stuck. But we did find a spawn point that can spawn us.
			// Place us there even if we get stuck.
			if ( firstEligiblePoint != null )
			{
				TryFitOnSpawnpoint( player, firstEligiblePoint, out var transform );
				player.Transform = transform;
				LastSpawnPoint[team] = firstEligiblePoint;
				return;
			}
		}


		//
		// SBOX DEFAULT SPAWN POINTS
		//

		// We weren't able to find any valid spawn point.
		// try to seek some default sbox ones.
		var sboxpoints = All
			.OfType<Sandbox.SpawnPoint>()
			.OrderBy( x => Guid.NewGuid() )
			.ToList();

		// there are default sbox points on this map!
		if ( sboxpoints.Count > 0 ) 
		{
			// find the one we can spawn at
			foreach ( var point in sboxpoints )
			{
				if ( TryFitOnSpawnpoint( player, point, out var transform ) )
				{
					player.Transform = transform;
					return;
				}
			}

			// nothing could fit us, place us at a random point,
			// even if we get stuck
			var rndpoint = Rand.FromList( sboxpoints );
			TryFitOnSpawnpoint( player, rndpoint, out var transform2 );
			player.Transform = transform2;
			return;
		}

		//
		// THIS MAP HAS NO SPAWN POINTS
		//

		// Spawn at 0,0,0. There's nothing we can do really.
		player.Transform = new( 0, Rotation.Identity );
	}
}
