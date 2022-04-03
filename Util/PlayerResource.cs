using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1;

partial class PlayerResource : Entity
{
	public static PlayerResource Instance { get; set; }
	[Net] public IList<Client> Clients { get; set; }

	public PlayerResource()
	{
		Instance = this;
	}

	public override void Spawn()
	{
		base.Spawn();
		Transmit = TransmitType.Always;
	}

	public float NextThinkTime { get; set; }
	public int UpdateCounter { get; set; }

	[Event.Tick.Server]
	public void Think()
	{
		if ( NextThinkTime > Time.Now )
			return;

		UpdateCounter++;
		UpdateAllPlayers();

		NextThinkTime = Time.Now + 0.1f;
	}

	public virtual void UpdateAllPlayers()
	{
		var clients = Client.All;

		// Cleanup data for disconnected players.
		foreach ( var client in Clients.Except( clients ) ) 
			OnClientDisconnect( client );

		foreach ( var client in clients )
		{
			var player = client.Pawn as Source1Player;
			PlayerUpdate( client, player );
		}
	}
}
