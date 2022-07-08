using Sandbox;
using System;

namespace Amper.Source1;

public static class Source1Extensions
{
	public async static void Reset( this DoorEntity door )
	{
		if ( !Host.IsServer ) return;
		var startsLocked = door.SpawnSettings.HasFlag( DoorEntity.Flags.StartLocked );

		// unlock the door to force change.
		var lastSpeed = door.Speed;
		// Close the door at a very high speed, so it visually closes immediately.
		door.Speed = 10000;
		door.Close();

		// wait some time
		await GameTask.DelaySeconds( 0.1f );

		// reset speed back.
		door.Speed = lastSpeed;
		if ( startsLocked ) door.Lock();
	}

	public static bool IsValid( this GameResource resource ) => resource != null;
	public static void NetInfo( this Logger logger, FormattableString message ) => logger.Info( $"[{(Host.IsServer ? "SV" : "CL")}] {message}" );
	public static void NetInfo( this Logger logger, object message ) => logger.Info( $"[{(Host.IsServer ? "SV" : "CL")}] {message}" );
}
