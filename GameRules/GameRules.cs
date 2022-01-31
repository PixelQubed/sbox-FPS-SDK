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
			DeclareTeam( 0, "Unassigned", Color.White, false, false );
			DeclareTeam( 1, "Spectator", Color.White, false, false );
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
		public virtual void RadiusDamage( DamageInfo info, Vector3 src, float radius, Entity ignore ) {  }

		/// <summary>
		/// On player respawned
		/// </summary>
		/// <param name="player"></param>
		public virtual void PlayerRespawn( Source1Player player ) { }

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
		public virtual void PlayerKilled( Source1Player player, DamageInfo info )
		{

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

		public virtual ViewVectors ViewVectors => new()
		{
			ViewOffset = new( 0, 0, 64 ),

			HullMin = new( -16, -16, 0 ),
			HullMax = new( 16, 16, 72 ),

			DuckHullMin = new( -16, -16, 0 ),
			DuckHullMax = new( 16, 16, 36 ),
			DuckViewOffset = new( 0, 0, 28 ),

			ObserverHullMin = new( -10, -10, -10 ),
			ObserverHullMax = new( 10, 10, 10 ),

			ObserverDeadViewPosition = new( 0, 0, 14 )
		};
	}

}
