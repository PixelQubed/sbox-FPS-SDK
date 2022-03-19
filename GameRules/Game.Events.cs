using Sandbox;

namespace Source1;

partial class GameRules
{
	#region Player_Death
	/// <summary>
	/// Player_Death
	/// </summary>
	/// <param name="victim"></param>
	/// <param name="attacker"></param>
	/// <param name="inflictor"></param>
	/// <param name="assister"></param>
	/// <param name="weapon"></param>
	/// <param name="flags"></param>
	public static void Event_OnPlayerDeath( Source1Player victim, Entity attacker, Entity inflictor, Entity assister, Entity weapon, DamageFlags flags )
	{
		if ( sv_log_events )
			Log.Info( $"[{(Host.IsServer ? "SV" : "CL")}] " +
			   $"Player_Death " +
			   $"(player \"{victim}\") " +
			   $"(attacker \"{attacker}\") " +
			   $"(inflictor \"{inflictor}\") " +
			   $"(assister \"{assister}\") " +
			   $"(weapon \"{weapon}\") " +
			   $"(flags \"{flags}\") " );

		Event.Run( "Player_Death", new Source1Event.Player.DeathArgs( victim, attacker, inflictor, assister, weapon, flags ) );
		if ( Host.IsServer ) Event_OnPlayerDeathClient( victim, attacker, inflictor, assister, weapon, flags );
	}

	[ClientRpc]
	public static void Event_OnPlayerDeathClient( Source1Player victim, Entity attacker, Entity inflictor, Entity assister, Entity weapon, DamageFlags flags )
	{
		Event_OnPlayerDeath( victim, attacker, inflictor, assister, weapon, flags );
	}

	#endregion

	#region Player_Spawn
	/// <summary>
	/// Player_Death
	/// </summary>
	/// <param name="player"></param>
	public static void Event_OnPlayerSpawn( Source1Player player )
	{
		if ( sv_log_events )
			Log.Info( $"[{(Host.IsServer ? "SV" : "CL")}] " +
			   $"Player_Spawn " +
			   $"(player \"{player}\") " );

		Event.Run( "Player_Spawn", new Source1Event.Player.SpawnArgs( player ) );
		if ( Host.IsServer ) Event_OnPlayerSpawnClient( player );
	}

	[ClientRpc]
	public static void Event_OnPlayerSpawnClient( Source1Player player )
	{
		Event_OnPlayerSpawn( player );
	}

	#endregion

	#region Player_Hurt
	/// <summary>
	/// Player_Hurt
	/// </summary>
	/// <param name="victim"></param>
	/// <param name="attacker"></param>
	/// <param name="inflictor"></param>
	/// <param name="assister"></param>
	/// <param name="weapon"></param>
	/// <param name="flags"></param>
	/// <param name="position"></param>
	/// <param name="damage"></param>
	public static void Event_OnPlayerHurt( Source1Player victim, Entity attacker, Entity inflictor, Entity assister, Entity weapon, DamageFlags flags, Vector3 position, float damage )
	{
		if ( sv_log_events )
			Log.Info( $"[{(Host.IsServer ? "SV" : "CL")}] " +
			   $"Player_Hurt " +
			   $"(player \"{victim}\") " +
			   $"(attacker \"{attacker}\") " +
			   $"(inflictor \"{inflictor}\") " +
			   $"(assister \"{assister}\") " +
			   $"(weapon \"{weapon}\") " +
			   $"(flags \"{flags}\") " +
			   $"(position \"{position}\") " +
			   $"(damage \"{damage}\")" );

		Event.Run( "Player_Hurt", new Source1Event.Player.HurtArgs( victim, attacker, inflictor, assister, weapon, flags, position, damage ) );
		if ( Host.IsServer ) Event_OnPlayerHurtClient( victim, attacker, inflictor, assister, weapon, flags, position, damage );
	}

	[ClientRpc]
	public static void Event_OnPlayerHurtClient( Source1Player victim, Entity attacker, Entity inflictor, Entity assister, Entity weapon, DamageFlags flags, Vector3 position, float damage )
	{
		Event_OnPlayerHurt( victim, attacker, inflictor, assister, weapon, flags, position, damage );
	}
	#endregion

	#region Player_ChangeTeam
	/// <summary>
	/// Player_ChangeTeam
	/// </summary>
	/// <param name="player"></param>
	/// <param name="team"></param>
	public static void Event_OnPlayerChangeTeam( Source1Player player, int team )
	{
		if ( sv_log_events )
			Log.Info( $"[{(Host.IsServer ? "SV" : "CL")}] " +
				$"Player_ChangeTeam " +
				$"(player \"{player}\") " +
				$"(team \"{team}\")" );

		Event.Run( "Player_ChangeTeam", new Source1Event.Player.ChangeTeamArgs( player, team ) );
		if ( Host.IsServer ) Event_OnPlayerChangeTeamClient( player, team );
	}

	[ClientRpc]
	public static void Event_OnPlayerChangeTeamClient( Source1Player player, int team )
	{
		Event_OnPlayerChangeTeam( player, team );
	}
	#endregion

	[ConVar.Replicated] public static bool sv_log_events { get; set; }


}
