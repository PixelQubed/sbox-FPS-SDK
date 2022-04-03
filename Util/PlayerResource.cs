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
	protected List<Source1Player> Players { get; set; } = new();

	public PlayerResource()
	{
		Instance = this;
	}

	public override void Spawn()
	{
		base.Spawn();
		Transmit = TransmitType.Always;
	}

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

	public float NextThinkTime { get; set; }
	public int UpdateCounter { get; set; }

	[Event.Tick.Server]
	public void Think()
	{
		Debug();

		if ( NextThinkTime > Time.Now )
			return;

		UpdateCounter++;
		UpdateAllPlayers();

		NextThinkTime = Time.Now + 0.1f;
	}

	public virtual void Debug()
	{

	}

	public virtual void UpdateAllPlayers()
	{
		var players = All.OfType<Source1Player>();

		// Cleanup data for disconnected players.
		foreach ( var player in Players.Except( players ) ) 
			OnPlayerDisconnect( player );

		foreach ( var player in players )
		{
			PlayerUpdate( player );
		}
	}
}
