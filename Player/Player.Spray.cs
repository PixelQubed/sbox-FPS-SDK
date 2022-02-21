using Sandbox;

namespace Source1
{
	partial class Source1Player
	{
		public TimeSince TimeSinceSprayed { get; set; }

		[ServerCmd( "spray" )]
		public static void Command_Spray()
		{
			if ( ConsoleSystem.Caller.Pawn is Source1Player player ) 
			{
				if ( player.TimeSinceSprayed < sv_spray_cooldown ) return;

				var tr = Trace.Ray( player.EyePosition, player.EyePosition + player.EyeRotation.Forward * sv_spray_max_distance )
					.WorldOnly()
					.Run();

				if ( tr.Hit )
				{
					Sound.FromWorld( "player.sprayer", tr.EndPos );
					var decal = DecalDefinition.ByPath["data/decal/spray.default.decal"];
					decal.PlaceUsingTrace( tr );

					player.TimeSinceSprayed = 0;
				}
			}
		}

		[ConVar.Replicated] public static float sv_spray_max_distance { get; set; } = 100;
		[ConVar.Replicated] public static float sv_spray_cooldown { get; set; } = 60;

	}
}
