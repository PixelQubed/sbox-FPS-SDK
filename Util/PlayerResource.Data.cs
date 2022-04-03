using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1;

partial class PlayerResource
{
	[Net] IDictionary<Source1Player, bool> Alive { get; set; }
	[Net] new IDictionary<Source1Player, float> Health { get; set; }

	public virtual void OnPlayerDisconnect( Source1Player player )
	{
		Players.Remove( player );

		Alive.Remove( player );
		Health.Remove( player );
	}

	public virtual void PlayerUpdate( Source1Player player )
	{
		if ( !Players.Contains( player ) )
			Players.Add( player );

		Alive[player] = player.IsAlive;
		Health[player] = player.Health;
	}

	public float GetHealth( Source1Player player )
	{
		float health = 0;
		Health.TryGetValue( player, out health );
		return health;
	}

	public bool IsAlive( Source1Player player )
	{
		var value = false;
		Alive.TryGetValue( player, out value );
		return value;
	}
}
