using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1;

partial class PlayerResource
{
	[Net] IDictionary<Client, bool> Alive { get; set; }
	[Net] new IDictionary<Client, float> Health { get; set; }

	public virtual void OnClientDisconnect( Client client )
	{
		Clients.Remove( client );

		Alive.Remove( client );
		Health.Remove( client );
	}

	public virtual void PlayerUpdate( Client client, Source1Player player )
	{
		if ( !Clients.Contains( client ) )
			Clients.Add( client );

		Alive[client] = player.IsAlive;
		Health[client] = player.Health;
	}

	public float GetHealth( Client client )
	{
		float health = 0;
		Health.TryGetValue( client, out health );
		return health;
	}

	public bool IsAlive( Client client )
	{
		var value = false;
		Alive.TryGetValue( client, out value );
		return value;
	}
}
