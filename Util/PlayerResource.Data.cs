using Sandbox;
using System.Collections.Generic;

namespace Source1;

partial class PlayerResource
{
	[Net] protected IDictionary<Client, int> TeamNumber { get; set; }
	[Net] protected IDictionary<Client, bool> Alive { get; set; }
	[Net] protected new IDictionary<Client, float> Health { get; set; }

	public virtual void OnClientDisconnect( Client client )
	{
		Clients.Remove( client );

		Alive.Remove( client );
		Health.Remove( client );
		TeamNumber.Remove( client );
	}

	public virtual void PlayerUpdate( Client client, Source1Player player )
	{
		if ( !Clients.Contains( client ) )
			Clients.Add( client );

		Alive[client] = player.IsAlive;
		Health[client] = player.Health;
		TeamNumber[client] = player.TeamNumber;
	}

	public static float GetHealth( Client client )
	{
		float health = 0;
		Instance.Health.TryGetValue( client, out health );
		return health;
	}

	public static bool IsAlive( Client client )
	{
		var value = false;
		Instance.Alive.TryGetValue( client, out value );
		return value;
	}
	public static int GetTeamNumber( Client client )
	{
		var value = -1;
		Instance.TeamNumber.TryGetValue( client, out value );
		return value;
	}
}
