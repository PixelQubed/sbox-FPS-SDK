using Sandbox;

namespace Amper.Source1;

partial class GameRules
{
	// This is being called by Sandbox's native "kill" command.
	public override void DoPlayerSuicide( Client cl )
	{
		var player = cl.Pawn as Source1Player;
		if ( player == null ) return;

		player.CommitSuicide( explode: false );
	}

	[ConCmd.Server( "explode", Help = "Spontaneous combustion!" )]
	public static void Command_Explode()
	{
		var client = ConsoleSystem.Caller;
		if ( client == null )
			return;

		var player = client.Pawn as Source1Player;
		if ( player == null )
			return;

		player.CommitSuicide( explode: true );
	}

	[ConCmd.Admin( "respawn" )]
	public static void Command_Respawn()
	{
		var client = ConsoleSystem.Caller;
		if ( client == null )
			return;

		var player = client.Pawn as Source1Player;
		if ( player == null )
			return;

		player.Respawn();
	}

	[ConVar.Server] public static float sv_damageforce_scale { get; set; } = 1;
}
