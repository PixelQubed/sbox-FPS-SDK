using Sandbox;
using System.Linq;

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

	float NextTickTime { get; set; }

	[Event.Tick]
	public void TickInternal()
	{
		if ( Time.Now < NextTickTime )
			return;

		Tick();

		NextTickTime = Time.Now + 0.1f;
	}

	public virtual void Tick()
	{
		SimulateStates();

		if ( IsServer )
		{
			CheckWaitingForPlayers();
			UpdateAllClientsData();
		}
	}

	public override void ClientDisconnect( Client client, NetworkDisconnectionReason reason )
	{
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

	public virtual float GetPlayerFallDamage( Source1Player player, float velocity )
	{
		var damage = velocity - player.MaxSafeFallSpeed;
		return damage * player.DamageForFallSpeed;
	}

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
	public virtual void PlayerDeath( Source1Player player, DamageInfo info ) { }

	/// <summary>
	/// This player was just hurt.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="info"></param>
	public virtual void PlayerHurt( Source1Player player, DamageInfo info ) { }

	/// <summary>
	/// On player respawned
	/// </summary>
	/// <param name="player"></param>
	public virtual void PlayerRespawn( Source1Player player ) { }

	/// <summary>
	/// On player respawned
	/// </summary>
	public virtual void PlayerChangeTeam( Source1Player player, int team ) { }

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
