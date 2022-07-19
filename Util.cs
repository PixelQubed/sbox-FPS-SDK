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
	public static DamageInfo WithoutFlag( this ref DamageInfo info, DamageFlags flag )
	{
		info.Flags &= ~flag;
		return info;
	}
}

public static class CollisionTags
{
	/// <summary>
	/// Never collides with anything.
	/// </summary>
	public const string NotSolid = "notsolid";
	/// <summary>
	/// Everything that is solid.
	/// </summary>
	public const string Solid = "solid";
	/// <summary>
	/// Trigger that isn't collideable but can still send touch events.
	/// </summary>
	public const string Trigger = "trigger";
	/// <summary>
	/// A ladder.
	/// </summary>
	public const string Ladder = "ladder";
	/// <summary>
	/// Water pool.
	/// </summary>
	public const string Water = "water";
	/// <summary>
	/// Never collides with anything except solid and other debris.
	/// </summary>
	public const string Debris = "debris";
	/// <summary>
	/// Just like debris, but also sends touch events to players.
	/// </summary>
	public const string Interactable = "interactable";
	/// <summary>
	/// This is a player.
	/// </summary>
	public const string Player = "player";
	/// <summary>
	/// A fired projectile.
	/// </summary>
	public const string Projectile = "projectile";
	/// <summary>
	/// This is a weapon players can interact with.
	/// </summary>
	public const string Weapon = "weapon";
	/// <summary>
	/// Driveable vehicle.
	/// </summary>
	public const string Vehicle = "vehicle";
	/// <summary>
	/// Physics prop, collideable by player movement by default.
	/// </summary>
	public const string Prop = "prop";
	/// <summary>
	/// A non playable entity.
	/// </summary>
	public const string NPC = "npc";

	public const string Clip = "clip";
	public const string PlayerClip = "playerclip";
	public const string BulletClip = "bulletclip";
	public const string ProjectileClip = "projectileclip";
	public const string NPCClip = "npcclip";
}

/// <summary>
/// This class allows the Source 1 codebase use the original movetype entries names 
/// by aliasing the names of the managed movetypes that garry renamed to unused for whatever reason.
/// We're referencing managed movetype entries by casting an int to that enum value
/// because only gman himself knows if any of these are gonna be renamed ever again.
/// </summary>
public static class NativeMoveType
{
	public const MoveType None = 0;                             // None
	public const MoveType Isometric = (MoveType)1;              // MOVETYPE_UNUSED1
	public const MoveType Walk = (MoveType)2;                   // MOVETYPE_WALK
	public const MoveType Step = (MoveType)3;                   // MOVETYPE_STEP
	public const MoveType Fly = (MoveType)4;                    // MOVETYPE_UNUSED2
	public const MoveType FlyGravity = (MoveType)5;             // MOVETYPE_UNUSED3
	public const MoveType Physics = (MoveType)6;                // Physics
	public const MoveType Push = (MoveType)7;                   // Push
	public const MoveType NoClip = (MoveType)8;                 // MOVETYPE_UNUSED4
	public const MoveType Ladder = (MoveType)9;                 // MOVETYPE_UNUSED5
	public const MoveType Observer = (MoveType)10;              // MOVETYPE_UNUSED6
}
