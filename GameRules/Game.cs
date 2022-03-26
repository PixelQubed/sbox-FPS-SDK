using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1;

public partial class GameRules : Game
{
	public new static GameRules Current { get; set; }

	public GameRules()
	{
		Current = this;
	}

	public override void Spawn()
	{
		base.Spawn();
		DeclareGameTeams();
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();
		DeclareGameTeams();
	}

	public virtual void DeclareGameTeams()
	{
		// By default all games have these two teams.

		TeamManager.DeclareTeam( 0, "unassigned", "UNASSIGNED", Color.White, false, false );
		TeamManager.DeclareTeam( 1, "spectator", "Spectator", Color.White, false, true );
	}

	[Event.Tick]
	public virtual void Tick()
	{
		SimulateStates();

		if ( IsServer )
		{
			CheckWaitingForPlayers();
		}
	}

	/// <summary>
	/// Called when a new client joins.
	/// </summary>
	public override void ClientJoined( Client client )
	{
		Event.Run( "Client_Connect", new Source1Event.Client.ConnectArgs( client ) );
	}

	public override void ClientDisconnect( Client client, NetworkDisconnectionReason reason )
	{
		Event.Run( "Client_Disconnect", new Source1Event.Client.DisconnectArgs( client, reason ) );

		if ( client.Pawn.IsValid() )
		{
			client.Pawn.Delete();
			client.Pawn = null;
		}
	}

	public override void PostLevelLoaded()
	{
		CalculateObjectives();
		CreateStandardEntities();
	}

	public virtual float GetPlayerFallDamage( Source1Player player, float velocity ) => 0;
	public virtual float GetGravityMultiplier() => 1;
	public virtual float GetDamageMultiplier() => 1;
	public virtual bool AllowThirdPersonCamera() => false;
	public virtual void RadiusDamage( DamageInfo info, Vector3 src, float radius, Entity ignore ) { }

	/// <summary>
	/// Can this player respawn right now?
	/// </summary>
	/// <param name="player"></param>
	public virtual bool CanPlayerRespawn( Source1Player player ) => true;

	/// <summary>
	/// Amount of seconds until this player is able to respawn.
	/// </summary>
	/// <param name="player"></param>
	public virtual float GetPlayerRespawnTime( Source1Player player ) => 0;

	/// <summary>
	/// This player was just killed.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="info"></param>
	public virtual void PlayerDeath( Source1Player player, DamageInfo info )
	{
		if ( !IsServer )
			return;

		Event_OnPlayerDeath( player, info.Attacker, null, null, info.Weapon, info.Flags );
	}

	/// <summary>
	/// This player was just hurt.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="info"></param>
	public virtual void PlayerHurt( Source1Player player, DamageInfo info )
	{
		if ( !IsServer )
			return;

		Event_OnPlayerHurt( player, info.Attacker, null, null, info.Weapon, info.Flags, info.Position, info.Damage );
	}

	/// <summary>
	/// On player respawned
	/// </summary>
	/// <param name="player"></param>
	public virtual void PlayerRespawn( Source1Player player )
	{
		if ( !IsServer )
			return;

		Event_OnPlayerSpawn( player );
	}

	/// <summary>
	/// On player respawned
	/// </summary>
	public virtual void PlayerChangeTeam( Source1Player player, int team )
	{
		if ( !IsServer ) 
			return;

		Event_OnPlayerChangeTeam( player, team );
	}

	/// <summary>
	/// Create standard game entities.
	/// </summary>
	public virtual void CreateStandardEntities() { }

	/// <summary>
	/// Respawn all players.
	/// </summary>
	public virtual void RespawnPlayers( bool forceRespawn, bool teamonly = false, int team = 0 )
	{
		var players = All.OfType<Source1Player>().ToList();

		foreach ( var player in players )
		{
			// if we only respawn 
			if ( teamonly && player.TeamNumber != team )
				continue;

			if ( !player.IsReadyToPlay() )
				continue;

			if ( !forceRespawn )
			{
				if ( player.IsAlive )
					continue;

				if ( !AreRespawnConditionsMet( player ) )
					continue;
			}

			player.Respawn();
		}
	}

	/// <summary>
	/// Player can technically respawn, but we must wait for certain condition to happen in order to 
	/// be respawned. (i.e. respawn waves)
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public virtual bool AreRespawnConditionsMet( Source1Player player ) => true;
	public bool HasPlayers() => All.OfType<Source1Player>().Any( x => x.IsReadyToPlay() );

	public override void DoPlayerNoclip( Client client )
	{
		if ( !client.HasPermission( "noclip" ) )
			return;

		var player = client.Pawn as Source1Player;
		if ( player == null ) return;

		// If player is not in noclip, enable it.
		if ( player.MoveType != MoveType.MOVETYPE_NOCLIP )
		{
			player.SetParent( null );
			player.MoveType = MoveType.MOVETYPE_NOCLIP;
			player.Tags.Add( PlayerTags.Noclipped );
			Log.Info( $"noclip ON for {client}" );
			return;
		}

		player.Tags.Remove( PlayerTags.Noclipped );
		player.MoveType = MoveType.MOVETYPE_WALK;
		Log.Info( $"noclip OFF for {client}" );
	}

	public override void DoPlayerSuicide( Client cl )
	{
		var player = cl.Pawn as Source1Player;
		if ( player == null ) return;

		player.CommitSuicide( false, false );
	}
}
