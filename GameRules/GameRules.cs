using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1
{
	public partial class GameRules : Game
	{
		public static GameRules Instance { get; set; }

		public GameRules()
		{
			Instance = this;
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
			TickStates();
		}

		/// <summary>
		/// Called when a new client joins.
		/// </summary>
		public override void ClientJoined( Client client )
		{
		}

		public override void PostLevelLoaded()
		{
			CreateStandardEntities();
		}


		public virtual float GetGravityMultiplier() { return 1; }
		public virtual float GetDamageMultiplier() { return 1; }
		public virtual bool AllowThirdPersonCamera() { return false; }
		public virtual void RadiusDamage( DamageInfo info, Vector3 src, float radius, Entity ignore ) { }

		/// <summary>
		/// Can this player respawn right now?
		/// </summary>
		/// <param name="player"></param>
		public virtual void CanPlayerRespawn( Source1Player player ) { }

		/// <summary>
		/// Amount of seconds until this player is able to respawn.
		/// </summary>
		/// <param name="player"></param>
		public virtual float GetPlayerRespawnTime( Source1Player player ) { return 0; }

		/// <summary>
		/// This player was just killed.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="info"></param>
		public virtual void PlayerDeath( Source1Player player, DamageInfo info )
		{
			if ( !IsServer ) return;
			Event_OnPlayerDeath( player, info.Attacker, null, null, info.Weapon, info.Flags );
		}

		/// <summary>
		/// This player was just hurt.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="info"></param>
		public virtual void PlayerHurt( Source1Player player, DamageInfo info )
		{
			if ( !IsServer ) return;
			Event_OnPlayerHurt( player, info.Attacker, null, null, info.Weapon, info.Flags, info.Position, info.Damage );
		}

		/// <summary>
		/// On player respawned
		/// </summary>
		/// <param name="player"></param>
		public virtual void PlayerRespawn( Source1Player player )
		{
			if ( !IsServer ) return;
			Event_OnPlayerSpawn( player );
		}

		/// <summary>
		/// Is this a valid spawn point for this player?
		/// </summary>
		/// <param name="point"></param>
		/// <param name="player"></param>
		public virtual void IsSpawnPointValid( Entity point, Source1Player player )
		{

		}

		/// <summary>
		/// Create standard game entities.
		/// </summary>
		public virtual void CreateStandardEntities()
		{

		}

		// States
		public virtual bool IsPlayerTeammate( Source1Player player, Source1Player other ) { return false; }

		// Winning
		public virtual void DeclareWinner( int team, int winreason, bool forceMapReset = true, bool switchTeams = false, bool dontAddScore = false, bool final = false ) { }
		public virtual void SetStalemate( int reason, bool forceMapReset = true, bool switchTeams = false ) { }

		// Switch Teams
		[Net] public bool ShouldSwitchTeams { get; set; }
		public virtual void SwitchTeams() { return; }

		// Waiting for players
		[Net] public bool IsWaitingForPlayers { get; set; }


		/// <summary>
		/// Respawn all players.
		/// </summary>
		/// <param name="forceRespawn"></param>
		/// <param name="team"></param>
		public void RespawnPlayers( bool forceRespawn, int team = 0 )
		{
			var players = All.OfType<Source1Player>().ToList();
			for ( int i = players.Count - 1; i >= 0; i-- )
			{
				var player = players[i];
				if ( !player.IsValid() ) continue;

				// Check for a certain team, if we are not unassigned.
				if ( team != 0 && player.TeamNumber != team ) continue;

				// TODO: Check for ready to play
				if ( !forceRespawn && player.IsAlive ) continue;

				player.Respawn();
			}
		}
	}
}
