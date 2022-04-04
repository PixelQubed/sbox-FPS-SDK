using Sandbox;

namespace Source1;

partial class GameRules
{
	public void UpdateAllClientsData()
	{
		var clients = Client.All;
		foreach ( var client in clients )
		{
			var pawn = client.Pawn as Source1Player;
			if ( pawn == null )
				continue;

			UpdateClientData( client, pawn );
		}
	}

	public virtual void UpdateClientData( Client client, Source1Player player )
	{
		client.SetValue( "f_health", player.Health );
		client.SetValue( "n_teamnumber", player.TeamNumber );
		client.SetValue( "b_alive", player.IsAlive );
	}
}

public static class ClientExtensions
{
	public static float GetHealth( this Client client ) => client.GetValue<float>( "f_health" );
	public static bool IsAlive( this Client client ) => client.GetValue<bool>( "b_alive" );
	public static int GetTeamNumber( this Client client ) => client.GetValue<int>( "n_teamnumber" );
}
